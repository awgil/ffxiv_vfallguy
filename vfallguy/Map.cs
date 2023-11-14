using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace vfallguy;

public class Map : IDisposable
{
    public enum AOEShape
    {
        None, // uninitialized
        Circle, // r1 = radius, r2 = n/a
        Square, // r1 = half-side, r2 = n/a
        Rect, // r1 = length from beginning (origin) to end along rotation, r2 = half-width
    }

    public class RepeatingAOE
    {
        public AOEShape Type;
        public float R1;
        public float R2;
        public float Rotation;
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
            AOEShape.Rect => Geom.IntersectRayRect(Origin, R1, R2, Rotation, start, dir),
            _ => (float.NaN, float.NaN)
        };

        public bool Contains(Vector3 p) => Type switch
        {
            AOEShape.Circle => (p - Origin).LengthXZSq() <= R1 * R1,
            AOEShape.Square => (p - Origin).MaxAbsCoordXZ() <= R1,
            AOEShape.Rect => Geom.RectContains(Origin, R1, R2, Rotation, p),
            _ => false
        };

        public bool Draw(DebugDrawer d, uint color) => Type switch
        {
            AOEShape.Circle => d.DrawWorldCircle(Origin, R1, color),
            AOEShape.Square => d.DrawWorldSquare(Origin, R1, color),
            AOEShape.Rect => d.DrawWorldRect(Origin, R1, R2, Rotation, color),
            _ => false
        };

        public bool Rasterize(PathfindMap m, DateTime now, float leeway) => NextActivation != default ? Type switch
        {
            AOEShape.Circle => m.BlockPixelsInsideCircle(Origin.XZ(), R1, (float)(NextActivation - now).TotalSeconds, 0.1f, Repeat, leeway),
            AOEShape.Square => m.BlockPixelsInsideSquare(Origin.XZ(), R1, (float)(NextActivation - now).TotalSeconds, 0.1f, Repeat, leeway),
            AOEShape.Rect => false, // TODO: implement
            _ => false
        } : false;
    }

    public class AOESequence
    {
        public int StartIndex;
        public int Count;
        public int NextIndex;
        public int FirstIndex; // for very first aoe
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
            Service.Log.Debug($"MoveTo {DebugName}: {StartPos}->{pos} with delay {NextDelay:f3}, will take {dt:f3}");
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
    public PathfindMap BaseMap;
    public List<(float z, float y)> HeightProfile;
    public float GoalZ;
    public List<RepeatingAOE> AOEs = new();
    public Task<List<Waypoint>>? PathTask;
    public List<Waypoint> Path = new();
    public int PathSkip;
    public Vector3 PlayerPos;
    public float AOELeeway = 0.5f;

    public Map(GameEvents events, PathfindMap baseMap, List<(float z, float y)> heightProfile, float goalZ)
    {
        Events = events;
        BaseMap = baseMap;
        HeightProfile = heightProfile;
        GoalZ = goalZ;
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
        try
        {
            PlayerPos = Service.ClientState.LocalPlayer!.Position;
            //if (PathTask?.IsCompleted ?? false)
            //{
            //    Path = PathTask.Result;
            //    PathSkip = 0;
            //    PathTask = null;
            //}
            //if (PathTask == null)
            //{
            //    var mapWithAOEs = BaseMap.Clone();
            //    var now = DateTime.Now;
            //    foreach (var aoe in AOEs)
            //    {
            //        aoe.Rasterize(mapWithAOEs, now, AOELeeway);
            //    }
            //    PathTask = Task.Run(() => BuildWaypointsTask(mapWithAOEs, now, PlayerPos));
            //}
            Path = RebuildPath(PlayerPos, DateTime.Now);
            PathSkip = 0;
        }
        catch (Exception ex)
        {
            Service.Log.Error($"start task: {ex}");
        }

        while (Path.Count > PathSkip && PlayerPos.Z < Path[PathSkip].Dest.Z) // TODO: better condition
            ++PathSkip;
    }

    public float HeightAt(float z)
    {
        if (HeightProfile.Count == 0)
            return 0;
        var idx = HeightProfile.FindIndex(p => p.z > z);
        if (idx == 0)
            return HeightProfile[0].y;
        else if (idx < 0)
            return HeightProfile.Last().y;

        var p1 = HeightProfile[idx - 1];
        var p2 = HeightProfile[idx];
        return p1.y + (z - p1.z) / (p2.z - p1.z) * (p2.y - p1.y);
    }

    public virtual string Strats() => "";
    protected virtual List<Waypoint> RebuildPath(Vector3 startPos, DateTime startTime) => new();
    protected virtual void OnActionEffect(uint actionId, Vector3 casterPos) { }
    protected virtual void OnStartCast(uint actionId, Vector3 casterPos) { }

    protected AOESequence CreateSequence(AOEShape type, float r1, float r2, IEnumerable<(Vector3 pos, float rot, float delay)> instances)
    {
        var startIndex = AOEs.Count;
        float repeat = 0;
        foreach (var (p, rot, d) in instances)
        {
            AOEs.Add(new() { Type = type, R1 = r1, R2 = r2, Rotation = rot, Origin = p, SeqDelay = d });
            repeat += d;
        }
        foreach (var aoe in AOEs.Skip(startIndex))
            aoe.Repeat = repeat;
        return new() { StartIndex = startIndex, Count = AOEs.Count - startIndex, FirstIndex = -1 };
    }

    protected void UpdateSequence(Vector3 pos, float activateIn, params AOESequence[] candidates) => UpdateSequence(pos, activateIn, candidates.AsEnumerable());
    protected void UpdateSequence(Vector3 pos, float activateIn, IEnumerable<AOESequence> candidates)
    {
        foreach (var c in candidates)
        {
            var index = FindIndexInSequence(c, pos);
            if (index < 0)
                continue;
            if (AOEs[c.StartIndex].NextActivation == default)
            {
                Service.Log.Info($"Starting sequence @ {c.StartIndex} from {index}");
                c.FirstIndex = index;
            }
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
