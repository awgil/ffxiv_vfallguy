using System;
using System.Collections.Generic;
using System.Numerics;

namespace vfallguy;

public class MapTest : Map
{
    public MapTest(GameEvents events) : base(events)
    {
        AOEs.Add(new() { Type = AOEShape.Circle, Origin = Service.ClientState.LocalPlayer!.Position + new Vector3(-4.5f, 0, -5f), R1 = 5, NextActivation = DateTime.Now.AddSeconds(5), Repeat = 2.5f });
    }

    protected override List<Waypoint> RebuildPath(Vector3 startPos, DateTime startTime)
    {
        var pb = new PathBuilder(startPos, startTime, "");
        var dest = startPos + new Vector3(0, 0, -5);
        var dir = (dest - startPos).NormalizedXZ();
        var (a, b) = AOEs[0].Intersect(startPos, dir);
        var t1 = pb.MoveToAOEEdge(AOEs[0], dir);
        Service.Log.Info($"foo: {t1} {a} {b}");
        pb.MoveTo(dest);
        return pb.Waypoints;
    }
}
