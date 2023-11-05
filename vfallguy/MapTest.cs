using System;
using System.Collections.Generic;
using System.Numerics;

namespace vfallguy;

public class MapTest : Map
{
    public MapTest(GameEvents events) : base(events) { }

    protected override List<Waypoint> RebuildPath(Vector3 startPos, DateTime startTime)
    {
        var pb = new PathBuilder(startPos, startTime, "");
        var aoe = new RepeatingAOE() { Type = AOEShape.Circle, Origin = startPos + new Vector3(-5, 0, -2.5f), R1 = 5, NextActivation = startTime.AddSeconds(0.41666667f) };
        var dest = startPos + new Vector3(0, 0, -5);
        var dir = (dest - startPos).NormalizedXZ();
        var (a, b) = aoe.Intersect(startPos, dir);
        var t1 = pb.MoveToAOEEdge(aoe, dir);
        Service.Log.Info($"foo: {t1} {a} {b}");
        pb.MoveTo(dest);
        return pb.Waypoints;
    }
}
