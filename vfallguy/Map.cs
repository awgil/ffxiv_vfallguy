using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace vfallguy;

public class Map : IDisposable
{
    public enum AOEShape
    {
        None, // uninitialized
        Circle, // r1 = radius, r2 = n/a
        Square, // r1 = half-side, r2 = n/a
    }

    public class RepeatingAOE
    {
        public AOEShape Type;
        public float R1;
        public float R2;
        public Vector3 Origin;
        public float Repeat; // seconds between activations
        public float SeqDelay; // delay until next aoe in sequence
        public DateTime NextActivation;

        public float TimeUntilNextActivation(DateTime now)
        {
            if (NextActivation == default)
                return float.MaxValue; // no data yet
            var imminentIn = (float)(NextActivation - now).TotalSeconds;
            return imminentIn >= 0 ? imminentIn : imminentIn % Repeat + Repeat; // TODO: consider returning 0 if we're slightly delaying?..
        }

        // returns negative value if does not activate, otherwise returns time between min and activation
        public float ActivatesBetween(DateTime now, float min, float max)
        {
            if (max < 0)
                return -1;
            min = Math.Max(0, min);
            var activateAfterMin = TimeUntilNextActivation(now.AddSeconds(min));
            return activateAfterMin < max - min ? activateAfterMin : -1;
        }

        public (float enter, float exit) Intersect(Vector3 start, Vector3 dir) => Type switch
        {
            AOEShape.Circle => Geom.IntersectRayCircle(Origin, R1, start, dir),
            AOEShape.Square => Geom.IntersectRaySquare(Origin, R1, start, dir),
            _ => (float.NaN, float.NaN)
        };

        public bool Contains(Vector3 p) => Type switch
        {
            AOEShape.Circle => (p - Origin).LengthXZSq() <= R1 * R1,
            AOEShape.Square => (p - Origin).MaxAbsCoordXZ() <= R1,
            _ => false
        };

        public bool Draw(DebugDrawer d, uint color) => Type switch
        {
            AOEShape.Circle => d.DrawWorldCircle(Origin, R1, color),
            AOEShape.Square => d.DrawWorldSquare(Origin, R1, color),
            _ => false
        };
    }

    public class AOESequence
    {
        public int StartIndex;
        public int Count;
        public int NextIndex;
    }

    public struct Waypoint
    {
        public Vector3 Dest;
        public DateTime StartMoveAt;
        public bool Jump;
    }

    public class PathBuilder
    {
        public string DebugName;
        public List<Waypoint> Waypoints = new();
        public Vector3 StartPos;
        public DateTime StartTime;
        public float NextDelay;

        public PathBuilder(Vector3 p, DateTime t, string debugName)
        {
            DebugName = debugName;
            StartPos = p;
            StartTime = t;
        }

        // returns time taken by movement
        public float MoveTo(Vector3 pos, bool jump = false)
        {
            var dt = (pos - StartPos).LengthXZ() * InvSpeed;
            Service.Log.Info($"MoveTo {DebugName}: {StartPos}->{pos} with delay {NextDelay:f3}, will take {dt:f3}");
            Waypoints.Add(new() { Dest = pos, StartMoveAt = NextDelay > 0 ? StartTime.AddSeconds(NextDelay) : default, Jump = jump });
            StartTime = StartTime.AddSeconds(NextDelay + dt);
            StartPos = pos;
            NextDelay = 0;
            return dt;
        }

        public float MoveBy(Vector3 offset) => MoveTo(StartPos + offset);

        public float Wait(float time) => NextDelay = Math.Max(time, 0);

        // TODO think about this api
        public float MoveToAOEEdge(RepeatingAOE aoe, Vector3 dir, float extraDelay = 0)
        {
            float t = 0;
            var (aoeEnter, aoeExit) = aoe.Intersect(StartPos, dir);
            if (aoeEnter > 0 && aoe.ActivatesBetween(StartTime, aoeEnter * InvSpeed + extraDelay, aoeExit * InvSpeed + extraDelay) is var delay && delay > 0)
            {
                // aoe is ahead, move near it, wait until it completes and then continue
                t += MoveBy(aoeEnter * dir);
                t += Wait(delay);
            }
            return t;
        }

        public PathBuilder Branch(string name) => new(StartPos, StartTime, $"{DebugName}>{name}");
        public void Merge(PathBuilder rest) => Waypoints.AddRange(rest.Waypoints);
    }

    public const float Speed = 6;
    public const float InvSpeed = 1.0f / Speed;

    public GameEvents Events;
    public List<RepeatingAOE> AOEs = new();
    public bool PathDirty = true;
    public List<Waypoint> Path = new();
    public Vector3 PlayerPos;

    public Map(GameEvents events)
    {
        Events = events;
        events.ActionEffectEvent += OnActionEffect;
        events.StartCastEvent += OnStartCast;
    }

    public virtual void Dispose()
    {
        Events.ActionEffectEvent -= OnActionEffect;
        Events.StartCastEvent -= OnStartCast;
    }

    public void Update()
    {
        PlayerPos = Service.ClientState.LocalPlayer!.Position;
        if (PathDirty)
        {
            var t = DateTime.Now;
            Path = RebuildPath(PlayerPos, DateTime.Now);
            PathDirty = false;
            Service.Log.Info($"Path rebuilt: took {(DateTime.Now - t).TotalMilliseconds:f3}ms, len={Path.Count}");
        }
        else
        {
            while (Path.Count > 0 && PlayerPos.Z < Path[0].Dest.Z)
                Path.RemoveAt(0);
        }
    }

    protected virtual List<Waypoint> RebuildPath(Vector3 startPos, DateTime startTime) => new();
    protected virtual void OnActionEffect(uint actionId, Vector3 casterPos) { }
    protected virtual void OnStartCast(uint actionId, Vector3 casterPos) { }

    protected AOESequence CreateSequence(AOEShape type, float r1, float r2, IEnumerable<(Vector3 pos, float delay)> instances)
    {
        var startIndex = AOEs.Count;
        float repeat = 0;
        foreach (var (p, d) in instances)
        {
            AOEs.Add(new() { Type = type, R1 = r1, R2 = r2, Origin = p, SeqDelay = d });
            repeat += d;
        }
        foreach (var aoe in AOEs.Skip(startIndex))
            aoe.Repeat = repeat;
        return new() { StartIndex = startIndex, Count = AOEs.Count - startIndex };
    }

    protected void UpdateSequence(Vector3 pos, float activateIn, params AOESequence[] candidates)
    {
        foreach (var c in candidates)
        {
            var index = FindIndexInSequence(c, pos);
            if (index < 0)
                continue;
            if (AOEs[c.StartIndex].NextActivation == default)
                Service.Log.Info($"Starting sequence @ {c.StartIndex} from {index}");
            if (activateIn > 0)
            {
                c.NextIndex = index;
                var t = DateTime.Now.AddSeconds(activateIn);
                for (int i = 0; i < c.Count; ++i)
                {
                    var aoe = AOEs[c.StartIndex + (c.NextIndex + i) % c.Count];
                    aoe.NextActivation = t;
                    t = t.AddSeconds(aoe.SeqDelay);
                }
            }
            else
            {
                c.NextIndex = (index + 1) % c.Count;
                var t = DateTime.Now;
                for (int i = 0; i < c.Count; ++i)
                {
                    var aoe = AOEs[c.StartIndex + (c.NextIndex + i) % c.Count];
                    t = t.AddSeconds(aoe.SeqDelay);
                    aoe.NextActivation = t;
                }
            }
            PathDirty = true;
            return;
        }
        Service.Log.Error($"Failed to find AOE at {pos}");
    }

    protected int FindIndexInSequence(AOESequence sequence, Vector3 pos)
    {
        for (int i = 0; i < sequence.Count; ++i)
            if ((AOEs[sequence.StartIndex + i].Origin - pos).LengthSquared() < 1)
                return i;
        return -1;
    }

    protected RepeatingAOE SequenceAOE(AOESequence seq, int index) => AOEs[seq.StartIndex + index];
}
