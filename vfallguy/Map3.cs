using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace vfallguy;

public class Map3 : Map
{
    private AOESequence _mech1Rotating;
    private AOESequence _mech2Exaflares;
    private AOESequence _mech3Rotating;
    private AOESequence _mech3Exaflares;
    private AOESequence _mech4RectsL;
    private AOESequence _mech4RectsR;
    private AOESequence _mech4Exaflare;
    private AOESequence _mech5PairL1;
    private AOESequence _mech5PairL2;
    private AOESequence _mech5PairR1;
    private AOESequence _mech5PairR2;
    private AOESequence _mech6Exaflare;
    private AOESequence _mech7PairL;
    private AOESequence _mech7PairR;

    private static PathfindMap BuildBaseMap()
    {
        var map = new PathfindMap(0.5f, 0.25f, new(0, 0, 223), 14, 101, 60);

        // columns at entrance
        map.BlockPixelsInsideCircle(new(-4, 292), 1.5f, 0, 60, 100, 0);
        map.BlockPixelsInsideCircle(new(+4, 292), 1.5f, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(-10, -3.3f, 285.5f, 293, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(+3.3f, +10, 285.5f, 293, 0, 60, 100, 0);

        // central column
        map.BlockPixelsInsideAlignedRect(-2, 2, 271, 278, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(-5.5f, 5.5f, 262.5f, 271, 0, 60, 100, 0);

        // exaflare lane columns
        map.BlockPixelsInsideAlignedRect(-9.5f, -5.5f, 247.5f, 256, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(-2.0f, +2.0f, 247.5f, 256, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(+5.5f, +9.5f, 247.5f, 256, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(-9.5f, -5.5f, 238.5f, 246.5f, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(-2.0f, +2.0f, 238.5f, 246.5f, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(+5.5f, +9.5f, 238.5f, 246.5f, 0, 60, 100, 0);

        // rect lane prisms
        BlockPrism(map, -9, 211, 4);
        BlockPrism(map, +9, 211, 4);
        BlockPrism(map, -2.5f, 202, 4);
        BlockPrism(map, +2.5f, 202, 4);
        BlockPrism(map, -9, 194, 4);
        BlockPrism(map, +9, 194, 4);
        BlockPrism(map, 0, 188, 4);

        // stairs center
        BlockTrapezium(map, 1.1f, 151.5f, 3.3f, 139);

        // final corridor
        BlockTrapezium(map, 4.3f, 133.5f, 8.5f, 123.7f);
        BlockCorners(map, 14, 139, 7, 136, 13, 123);

        return map;
    }

    private static void BlockPrism(PathfindMap m, float cx, float cy, float s)
    {
        var center = new Vector2(cx, cy);
        var vs = new Vector2(s);
        m.BlockPixelsInside(center - vs, center + vs, v => Math.Abs(v.X - cx) + Math.Abs(v.Y - cy) <= s, 0, 60, 100, 0);
    }

    private static void BlockTrapezium(PathfindMap m, float dx1, float y1, float dx2, float y2)
    {
        float coeff = (dx2 - dx1) / (y2 - y1);
        float cons = dx1 - y1 * coeff;
        m.BlockPixelsInside(new(-dx2, y2), new(dx2, y1), v => Math.Abs(v.X) < cons + coeff * v.Y, 0, 60, 100, 0);
    }

    private static void BlockCorners(PathfindMap m, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        var a = new Vector2(x1, y1);
        var b = new Vector2(x2, y2);
        var c = new Vector2(x3, y3);
        var ab = b - a;
        var bc = c - b;
        var n1 = new Vector2(ab.Y, -ab.X);
        var n2 = new Vector2(bc.Y, -bc.X);
        m.BlockPixelsInside(new(-x1, y3), new(x1, y1), v => Vector2.Dot(n1, new(Math.Abs(v.X) - x1, v.Y - y1)) < 0 && Vector2.Dot(n2, new(Math.Abs(v.X) - x2, v.Y - y2)) < 0, 0, 60, 100, 0);
    }

    private static List<(float z, float y)> _heightProfile = new()
    {
        (135.6f, 36.2f),
        (139.0f, 35.5f),
        (139.1f, 34.5f),
        (143.0f, 34.3f),
        (143.1f, 33.6f),
        (147.0f, 33.4f),
        (147.1f, 32.8f),
        (150.9f, 32.5f),
        (151.0f, 31.9f),
        (180.7f, 28.8f),
        (218.9f, 15.1f),
        (229.7f, 14.5f),
        (236.8f, 13.5f),
        (262.9f, 6.0f),
        (273.2f, 6.0f),
        (286.9f, 3.2f),
    };

    public Map3(GameEvents events) : base(events, BuildBaseMap(), _heightProfile, 130)
    {
        _mech1Rotating = CreateRotatingSequence(1.2f, 6, 267.5f, [-10, 10]);
        _mech2Exaflares = CreateDoubleExaflareSequence(9.39f, 251, 11.75f, 243);
        _mech3Rotating = CreateRotatingSequence(1.0f, 14.76f, 225.3f, [-10, 0, 10]);
        _mech3Exaflares = CreateDoubleExaflareSequence(14.54f, 229.3f, 14.97f, 221.3f);
        _mech4RectsL = CreateRectsSequence(-12, -6);
        _mech4RectsR = CreateRectsSequence(12, 6);
        _mech4Exaflare = CreateSingleExaflareSequence(22.52f, 198.7f, [-12, -4, 4, 12]);
        _mech5PairL1 = CreatePairsSequence(5, new(-10, 29.95f, 170), new(-2, 29.95f, 170));
        _mech5PairL2 = CreatePairsSequence(5, new(-10, 31.39f, 156), new(-4.34f, 30.81f, 161.66f));
        _mech5PairR1 = CreatePairsSequence(5, new(10, 29.95f, 170), new(4.34f, 29.37f, 175.66f));
        _mech5PairR2 = CreatePairsSequence(5, new(10, 31.39f, 156), new(2, 31.39f, 156));
        _mech6Exaflare = CreateSingleExaflareSequence(33.47f, 145.7f, [-4, 4, 12, -12]);
        _mech7PairL = CreatePairsSequence(3, new(-6.78f, 35.91f, 136.91f), new(-3.22f, 36.30f, 135.09f));
        _mech7PairR = CreatePairsSequence(3, new(6.78f, 35.91f, 136.91f), new(3.22f, 36.30f, 135.09f));

        //foreach (var a in AOEs)
        //    Service.Log.Info($"aoe: {a.Type} r={a.R1} @ {a.Origin}");
    }

    protected override List<Waypoint> RebuildPath(Vector3 startPos, DateTime startTime)
    {
        return new();
        //var p = new PathBuilder(startPos, startTime, "");
        //var end = Pathfind1(p);
        //Service.Log.Info($"Final path length: {(end - startTime).TotalSeconds}s");
        //return p.Waypoints;
    }

    protected override void OnActionEffect(uint actionId, Vector3 casterPos)
    {
        switch (actionId)
        {
            case 34801:
                UpdateSequence(casterPos, 0, _mech2Exaflares, _mech3Exaflares, _mech4Exaflare, _mech6Exaflare);
                break;
            case 34802:
                UpdateSequence(casterPos, 0, _mech1Rotating, _mech3Rotating);
                break;
            case 34804:
            case 34812:
                UpdateSequence(casterPos, 0, _mech4RectsL, _mech4RectsR);
                break;
            case 34796:
                UpdateSequence(casterPos, 0, _mech5PairL1, _mech5PairL2, _mech5PairR1, _mech5PairR2);
                break;
            case 29795:
                UpdateSequence(casterPos, 0, _mech7PairL, _mech7PairR);
                break;
        }
    }

    protected override void OnStartCast(uint actionId, Vector3 casterPos)
    {
        if (actionId == 34812)
            UpdateSequence(casterPos, 1, _mech4RectsL, _mech4RectsR);
    }

    private AOESequence CreateRotatingSequence(float repeat, float y, float z, params float[] x)
    {
        return CreateSequence(AOEShape.Circle, 5, 0, x.Select(x => (new Vector3(x, y, z), 0.0f, repeat)));
    }

    private AOESequence CreateSingleExaflareSequence(float y, float z, params float[] x)
    {
        return CreateSequence(AOEShape.Circle, 6, 0, x.Select(xx => (new Vector3(xx, y, z), 0.0f, xx == x.Last() ? 1.9f : 1.4f)));
    }

    private AOESequence CreateDoubleExaflareSequence(float y1, float z1, float y2, float z2)
    {
        float[] x = [-12, -4, 4, 12];
        var l1 = x.Select(x => (new Vector3(x, y1, z1), 0.0f, 1.4f));
        var l2 = x.Select(x => (new Vector3(x, y2, z2), 0.0f, 1.4f));
        return CreateSequence(AOEShape.Circle, 6, 0, l1.Concat(l2));
    }

    private AOESequence CreateRectsSequence(float x1, float x2)
    {
        (float y, float z, float d)[] l = [(25.59f, 190.4f, 0.5f), (23.37f, 196.4f, 0.5f), (21.15f, 202.4f, 0.5f), (18.94f, 208.4f, 0.5f), (16.73f, 214.4f, 1.1f)];
        var l1 = l.Select(e => (new Vector3(x1, e.y, e.z), 0.0f, e.d));
        var l2 = l.Select(e => (new Vector3(x2, e.y, e.z), 0.0f, e.d));
        return CreateSequence(AOEShape.Square, 3, 0, l1.Concat(l2));
    }

    private AOESequence CreatePairsSequence(float r, Vector3 p1, Vector3 p2)
    {
        return CreateSequence(AOEShape.Circle, r, 0, [(p1, 0.0f, 2.5f), (p2, 0.0f, 2.5f)]);
    }

    // step 1: from starting position to the beginning of the divider
    private DateTime Pathfind1(PathBuilder path)
    {
        if (path.StartPos.Z <= 277.5f)
            return Pathfind2(path, path.StartPos.X > 0); // step is already done

        var pathL = path.Branch("L");
        var pathR = path.Branch("R");

        // path around that portal
        var firstEdgeL = new Vector3(-5, 6, 270);
        var firstEdgeR = new Vector3(5, 6, 270);
        if (path.StartPos.Z >= 293)
        {
            pathL.MoveTo(Geom.ClampX(Geom.IntersectZPlane(pathL.StartPos, firstEdgeL, 292.5f), -3.5f, 3.5f));
            pathR.MoveTo(Geom.ClampX(Geom.IntersectZPlane(pathR.StartPos, firstEdgeR, 292.5f), -3.5f, 3.5f));
        }

        // path to the edge of the barrier
        pathL.MoveTo(new(Math.Min(pathL.StartPos.X, firstEdgeL.X), firstEdgeL.Y, Math.Min(pathL.StartPos.Z, firstEdgeL.Z)));
        pathR.MoveTo(new(Math.Max(pathR.StartPos.X, firstEdgeR.X), firstEdgeR.Y, Math.Min(pathR.StartPos.Z, firstEdgeR.Z)));

        // and now continue to the end
        var eL = Pathfind2(pathL, false);
        var eR = Pathfind2(pathR, true);
        if (eL < eR)
        {
            path.Merge(pathL);
            return eL;
        }
        else
        {
            path.Merge(pathR);
            return eR;
        }
    }

    // step 1: from beginning of the divider to the end of the first hazard (rotating things) - hug the inner wall
    private DateTime Pathfind2(PathBuilder path, bool right)
    {
        if (path.StartPos.Z <= 263)
            return Pathfind3(path, right); // step is already done

        // path to the edge of the barrier
        if (path.StartPos.Z > 270 || Math.Abs(path.StartPos.X) < 5)
        {
            var destX = right ? Math.Max(path.StartPos.X, 5) : Math.Min(path.StartPos.X, -5);
            path.MoveTo(new Vector3(destX, 6, Math.Min(path.StartPos.Z, 270)));
        }

        // aoe is ahead, move near it, wait until it completes and then continue
        var dest = new Vector3(right ? 5 : -5, 6, 263);
        var dir = (dest - path.StartPos).NormalizedXZ();
        var aoe = SequenceAOE(_mech1Rotating, right ? 1 : 0);
        path.MoveToAOEEdge(aoe, dir);
        path.MoveTo(dest);

        // and now continue to the end
        return Pathfind3(path, right);
    }

    // step 3: from the end of the first hazard to the either side of the barriers that double exaflares pass through
    private DateTime Pathfind3(PathBuilder path, bool right)
    {
        // note: barriers begin at ~254.5, but corners are clipped by exaflares
        if (path.StartPos.Z <= 257)
            return Pathfind4(path, right, Math.Abs(path.StartPos.X) > 7 ? true : false); // step is already done

        var pathIn = path.Branch("I");
        var pathOut = path.Branch("O");

        pathIn.MoveTo(new(right ? 5 : -5, 8.3f, 257));
        pathOut.MoveTo(new(right ? 9 : -9, 8.3f, 257));

        // and now continue to the end
        var eIn = Pathfind4(pathIn, right, false);
        var eOut = Pathfind4(pathOut, right, true);
        if (eIn < eOut)
        {
            path.Merge(pathIn);
            return eIn;
        }
        else
        {
            path.Merge(pathOut);
            return eOut;
        }
    }

    // step 4: double exaflares, hugging either side of the side barriers
    private DateTime Pathfind4(PathBuilder path, bool right, bool outer)
    {
        if (path.StartPos.Z <= 238.5f)
            return Pathfind5(path, right); // step is already done

        var dest = new Vector3((outer ? 9 : 6) * (right ? 1 : -1), 13, 238.5f);
        var dir = (dest - path.StartPos).NormalizedXZ();

        var ex1Out = SequenceAOE(_mech2Exaflares, right ? 3 : 0);
        var ex2Out = SequenceAOE(_mech2Exaflares, right ? 7 : 4);
        var ex1In = SequenceAOE(_mech2Exaflares, right ? 2 : 1);
        var ex2In = SequenceAOE(_mech2Exaflares, right ? 6 : 5);
        if (outer)
        {
            if (right)
            {
                // if we'll be clipped by outer, just wait
                if (path.MoveToAOEEdge(ex1Out, dir) == 0)
                {
                    // nope, but we might get clipped by preceeding inner - in such case we can't just wait on its edge, we won't escape next outer - we'll have to wait by outer edge
                    var (aoeEnter, aoeExit) = ex1In.Intersect(path.StartPos, dir);
                    if (aoeEnter > 0 && ex1In.ActivatesBetween(path.StartTime, aoeEnter * InvSpeed, aoeExit * InvSpeed) is var innerDelay && innerDelay > 0)
                    {
                        path.MoveToAOEEdge(ex1Out, dir, innerDelay);
                    }
                }
                if (path.MoveToAOEEdge(ex2Out, dir) == 0)
                {
                    // nope, but we might get clipped by preceeding inner - in such case we can't just wait on its edge, we won't escape next outer - we'll have to wait by outer edge
                    var (aoeEnter, aoeExit) = ex2In.Intersect(path.StartPos, dir);
                    if (aoeEnter > 0 && ex2In.ActivatesBetween(path.StartTime, aoeEnter * InvSpeed, aoeExit * InvSpeed) is var innerDelay && innerDelay > 0)
                    {
                        path.MoveToAOEEdge(ex2Out, dir, innerDelay);
                    }
                }
            }
            else
            {
                // outer left: x=-9, so risky aoes are at -12 and then -4; worst case is we wait for -12, move a bit, wait for -4, then run
                path.MoveToAOEEdge(ex1Out, dir);
                path.MoveToAOEEdge(ex1In, dir);
                // repeat same for second row
                path.MoveToAOEEdge(ex2Out, dir);
                path.MoveToAOEEdge(ex2In, dir);
            }
        }
        else
        {
            // only risky is at +-4
            // TODO: reconsider x, if we're really hugging a wall at -6.5, we might get clipped out outer aoe too...
            path.MoveToAOEEdge(ex1In, dir);
            path.MoveToAOEEdge(ex2In, dir);
        }
        path.MoveTo(dest);

        return Pathfind5(path, right);
    }

    // step 5: double exaflares + rotating things, up to the lower barrier on rect line, on either inner or outer side
    private DateTime Pathfind5(PathBuilder path, bool right)
    {
        if (path.StartPos.Z <= 214)
            return Pathfind6(path, right, Math.Abs(path.StartPos.X) >= 9); // step is already done

        var pathIn = path.Branch("I");
        var pathOut = path.Branch("O");

        var eIn = Pathfind5Branch(pathIn, right, false);
        var eOut = Pathfind5Branch(pathOut, right, true);
        if (eIn < eOut)
        {
            path.Merge(pathIn);
            return eIn;
        }
        else
        {
            path.Merge(pathOut);
            return eOut;
        }
    }

    private DateTime Pathfind5Branch(PathBuilder path, bool right, bool lowOuter)
    {
        var dest = new Vector3((lowOuter ? 12 : 6) * (right ? 1 : -1), 18, 211);
        var dir = (dest - path.StartPos).NormalizedXZ();

        // we care about in/out exaflares and single rotating piece
        // TODO consider using central rotating piece to route around unfortunate inner one?
        var ex1Out = SequenceAOE(_mech3Exaflares, right ? 3 : 0);
        var ex2Out = SequenceAOE(_mech3Exaflares, right ? 7 : 4);
        var ex1In = SequenceAOE(_mech3Exaflares, right ? 2 : 1);
        var ex2In = SequenceAOE(_mech3Exaflares, right ? 6 : 5);
        var rotating = SequenceAOE(_mech3Rotating, right ? 2 : 0);

        // TODO: this isn't right, we could move towards rotating/second line, wait and then get caught by first line/rotating...
        if (right)
        {
            if (path.MoveToAOEEdge(ex1Out, dir) == 0)
            {
                // nope, but we might get clipped by preceeding inner - in such case we can't just wait on its edge, we won't escape next outer - we'll have to wait by outer edge
                var (aoeEnter, aoeExit) = ex1In.Intersect(path.StartPos, dir);
                if (aoeEnter > 0 && ex1In.ActivatesBetween(path.StartTime, aoeEnter * InvSpeed, aoeExit * InvSpeed) is var innerDelay && innerDelay > 0)
                {
                    path.MoveToAOEEdge(ex1Out, dir, innerDelay);
                }
            }
            path.MoveToAOEEdge(rotating, dir);
            if (path.MoveToAOEEdge(ex2Out, dir) == 0)
            {
                // nope, but we might get clipped by preceeding inner - in such case we can't just wait on its edge, we won't escape next outer - we'll have to wait by outer edge
                var (aoeEnter, aoeExit) = ex2In.Intersect(path.StartPos, dir);
                if (aoeEnter > 0 && ex2In.ActivatesBetween(path.StartTime, aoeEnter * InvSpeed, aoeExit * InvSpeed) is var innerDelay && innerDelay > 0)
                {
                    path.MoveToAOEEdge(ex2Out, dir, innerDelay);
                }
            }
        }
        else
        {
            path.MoveToAOEEdge(ex1Out, dir);
            path.MoveToAOEEdge(ex1In, dir);
            path.MoveToAOEEdge(rotating, dir);
            path.MoveToAOEEdge(ex2Out, dir);
            path.MoveToAOEEdge(ex2In, dir);
        }
        path.MoveTo(dest);

        // TODO: this should be improved...
        var rects = right ? _mech4RectsR : _mech4RectsL;
        if (lowOuter)
        {
            if (rects.NextIndex is >= 2 and <= 6)
                return DateTime.MaxValue; // we'll die here
        }
        else
        {
            if (rects.NextIndex is >= 7 or <= 1)
                return DateTime.MaxValue; // we'll die here
        }

        return Pathfind6(path, right, lowOuter);
    }

    // step 6: between two blockers, avoiding lines
    private DateTime Pathfind6(PathBuilder path, bool right, bool outer)
    {
        // we have three major options here: stay in current lane (only reasonable when we're almost near high corner), change lane or change lane twice (avoid incoming line then return)
        if (path.StartPos.Z <= 198)
            return Pathfind7(path, right, outer); // step is already done

        var rects = right ? _mech4RectsR : _mech4RectsL;
        if (path.StartPos.Z >= 211.4)
        {
            // we're still in last rect and did not reach the corner - continue to corner, ensuring we're not clipped by an aoe
            var dest = new Vector3((outer ? 12 : 6) * (right ? 1 : -1), 18, 211);
            var now = path.StartTime;
            var t = path.MoveTo(dest);
            if (SequenceAOE(rects, outer ? 4 : 9).ActivatesBetween(now, 0, t) > 0)
                return DateTime.MaxValue;
        }

        //if (path.StartPos.Z >= 205.4)
        //{
        //    var aoe4 = SequenceAOE(rects, outer ? 3 : 8);
        //    var sideEscape = new Vector3(9, 0, Math.Min(207.5f, path.StartPos.Z));
        //    var aoe4Activation = aoe4.TimeUntilNextActivation(path.StartTime);
        //    if (aoe4Activation < sideEscape.LengthXZ() * InvSpeed)
        //        return DateTime.MaxValue; // we can't escape
        //}

        var pathStay = path.Branch("Stay");
        var pathChange = path.Branch("Cross");

        var eStay = Pathfind6Stay(pathStay, right, outer);
        var eChange = Pathfind6Change(pathChange, right, outer);
        if (eStay < eChange)
        {
            path.Merge(pathStay);
            return eStay;
        }
        else
        {
            path.Merge(pathChange);
            return eChange;
        }
    }

    private DateTime Pathfind6Stay(PathBuilder path, bool right, bool outer)
    {
        var dest = new Vector3((outer ? 12 : 6) * (right ? 1 : -1), 18, 194.5f);
        var dir = (dest - path.StartPos).NormalizedXZ();

        var rects = right ? _mech4RectsR : _mech4RectsL;
        // try going straight, see where we hit first aoe
        float maxDistInLane = MaxDistanceInLane(rects, 3, 1, outer, path, dir);

        if (maxDistInLane == float.MaxValue)
        {
            // go straight to destination
            path.MoveTo(dest);
            return Pathfind7(path, right, outer);
        }

        // we need to cross
        var interX = (outer ? 8.5f : 9.5f) * (right ? 1 : -1);
        var deltaX = Math.Abs(interX - path.StartPos.Z);
        if (deltaX > maxDistInLane)
            return DateTime.MaxValue; // won't escape the lane
        var interZ = path.StartPos.Z - MathF.Sqrt(maxDistInLane * maxDistInLane - deltaX * deltaX);
        if (interZ > 207.5f)
            return DateTime.MaxValue; // won't escape the line because of lower blocker
        interZ = Math.Max(interZ, 197.5f); // upper blocker
        path.MoveTo(new(interX, 0, interZ));
        // and continue from there...
        return Pathfind6Change(path, right, !outer);
    }

    private DateTime Pathfind6Change(PathBuilder path, bool right, bool outer)
    {
        var dest = new Vector3((outer ? 6 : 12) * (right ? 1 : -1), 18, 194.5f);
        // we pass this point if we go straight
        var inter = Geom.IntersectXPlane(path.StartPos, dest, 9);
        if (inter.Z < 197.5f)
            inter.Z = 197.5f; // upper blocker

        // if we get hit by aoes on our side before, we might need to cross faster
        var rects = right ? _mech4RectsR : _mech4RectsL;
        float maxDistInLane = MaxDistanceInLane(rects, 3, 1, outer, path, (inter - path.StartPos).NormalizedXZ());
        if (maxDistInLane < float.MaxValue)
        {
            var interX = (outer ? 8.5f : 9.5f) * (right ? 1 : -1);
            var deltaX = Math.Abs(interX - path.StartPos.Z);
            if (deltaX > maxDistInLane)
                return DateTime.MaxValue; // won't escape the lane
            var interZ = path.StartPos.Z - MathF.Sqrt(maxDistInLane * maxDistInLane - deltaX * deltaX);
            if (interZ > 207.5f)
                return DateTime.MaxValue; // won't escape the line because of lower blocker
            interZ = Math.Max(interZ, 197.5f); // upper blocker
            path.MoveTo(new(interX, 18, interZ));
            // and continue from there...
            return Pathfind6Stay(path, right, !outer);
        }

        // we also might want to stay longer in our lane to avoid neighbouring aoes
        var pathCross = path.Branch("Test");
        pathCross.MoveTo(inter);
        maxDistInLane = MaxDistanceInLane(rects, 3, 1, !outer, pathCross, (dest - pathCross.StartPos).NormalizedXZ());
        if (maxDistInLane == float.MaxValue)
        {
            // all good
            path.MoveTo(inter);
            path.MoveTo(dest);
        }
        else
        {
            // need to stay longer
            maxDistInLane += (inter - path.StartPos).LengthXZ();
            var interX = 9;
            var deltaX = Math.Abs(interX - path.StartPos.Z);
            var deltaZ = deltaX > maxDistInLane ? 0 : MathF.Sqrt(maxDistInLane * maxDistInLane - deltaX * deltaX);
            var interZ = path.StartPos.Z - deltaZ;
            if (interZ < 197.5f)
                inter.Z = 197.5f; // upper blocker
            var delay = maxDistInLane * InvSpeed - path.MoveTo(new(interX, 0, interZ));
            if (delay > 0)
                path.Wait(delay);
            path.MoveTo(dest);
        }

        return Pathfind7(path, right, !outer);
    }

    private float MaxDistanceInLane(AOESequence seq, int firstIndex, int lastIndex, bool outer, PathBuilder path, Vector3 dir)
    {
        for (int i = firstIndex; i >= lastIndex; --i)
        {
            var aoe = SequenceAOE(seq, outer ? i : i + 5);
            var (enter, exit) = aoe.Intersect(path.StartPos, dir);
            if (enter > 0 && aoe.ActivatesBetween(path.StartTime, enter * InvSpeed, exit * InvSpeed) is var delay && delay > 0)
            {
                return (enter * InvSpeed + delay) * Speed;
            }
        }
        return float.MaxValue;
    }

    // step 7: around upper blocker, avoiding first rect aoe
    private DateTime Pathfind7(PathBuilder path, bool right, bool outer)
    {
        if (path.StartPos.Z <= 187.4f)
            return Pathfind8(path, right); // step is already done

        var aoe = SequenceAOE(right ? _mech4RectsR : _mech4RectsL, outer ? 0 : 5);
        var timeLeft = (aoe.NextActivation - path.StartTime).TotalSeconds;
        if (path.StartPos.Z > 194.5f)
        {
            // move to corner
            timeLeft -= path.MoveTo(new Vector3((outer ? 12 : 6) * (right ? 1 : -1), 18, 194.5f));
            if (timeLeft <= 0)
                return DateTime.MaxValue;
        }

        var dest = new Vector3(right ? 5 : -5, 0, 136); // very approximate end corridor entry point
        var exit = aoe.Intersect(path.StartPos, (dest - path.StartPos).NormalizedXZ()).exit;
        if (exit < timeLeft * Speed)
        {
            return Pathfind8(path, right); // just ignore this and continue forward
        }

        // try going straight up
        exit = path.StartPos.Z - 187.4f;
        if (exit < timeLeft * Speed)
        {
            path.MoveTo(new(path.StartPos.X, path.StartPos.Z, 187.4f));
            return Pathfind8(path, right);
        }

        // try crossing
        dest = new Vector3((outer ? 8.5f : 9.5f) * (right ? 1 : -1), 0, Math.Min(191, path.StartPos.Z));
        timeLeft -= path.MoveTo(dest);
        return timeLeft <= 0 ? DateTime.MaxValue : Pathfind8(path, right);
    }

    // step 8: first row of hammers, either side
    private DateTime Pathfind8(PathBuilder path, bool right)
    {
        var hammers = right ? _mech5PairR1 : _mech5PairL1;
        var aoeOut = SequenceAOE(hammers, 0);
        var aoeIn = SequenceAOE(hammers, 1);
        var hammerCenter = (aoeIn.Origin + aoeOut.Origin) * 0.5f;
        var hammerDir = right ? new Vector3(-0.70710678f, 0, -0.70710678f) : new Vector3(0, 0, -1);
        var hammerOut = right ? new Vector3(+0.70710678f, 0, -0.70710678f) : new Vector3(-1, 0, 0);
        var hammerBase = hammerCenter - 1.5f * hammerDir;
        var relPos = path.StartPos - hammerBase;
        if (hammerDir.DotXZ(relPos) >= 0)
            return Pathfind9(path, right, hammerOut.DotXZ(relPos) > 0); // step is already done

        var pathIn = path.Branch("I");
        var pathOut = path.Branch("O");

        var cornerIn = hammerBase - 0.5f * hammerOut;
        var cornerOut = hammerBase + 0.5f * hammerOut;
        pathIn.MoveToAOEEdge(aoeIn, (cornerIn - path.StartPos).NormalizedXZ());
        pathIn.MoveTo(cornerIn);
        pathOut.MoveToAOEEdge(aoeOut, (cornerOut - path.StartPos).NormalizedXZ());
        pathOut.MoveTo(cornerOut);

        var eIn = Pathfind9(pathIn, right, false);
        var eOut = Pathfind9(pathOut, right, true);
        if (eIn < eOut)
        {
            path.Merge(pathIn);
            return eIn;
        }
        else
        {
            path.Merge(pathOut);
            return eOut;
        }
    }

    // step 9: along one side of the hammer on first row
    private DateTime Pathfind9(PathBuilder path, bool right, bool outer)
    {
        var hammers = right ? _mech5PairR1 : _mech5PairL1;
        var aoeOut = SequenceAOE(hammers, 0);
        var aoeIn = SequenceAOE(hammers, 1);
        var hammerCenter = (aoeIn.Origin + aoeOut.Origin) * 0.5f;
        var hammerDir = right ? new Vector3(-0.70710678f, 0, -0.70710678f) : new Vector3(0, 0, -1);
        var hammerEnd = hammerCenter + 1.5f * hammerDir;
        var relPos = path.StartPos - hammerEnd;
        if (hammerDir.DotXZ(relPos) >= 0)
            return Pathfind10(path, right); // step is already done

        var hammerOut = right ? new Vector3(+0.70710678f, 0, -0.70710678f) : new Vector3(-1, 0, 0);
        var corner = hammerEnd + hammerOut * (outer ? 0.5f : -0.5f);
        var aoe = outer ? aoeOut : aoeIn;
        var timeLeft = (aoe.NextActivation - path.StartTime).TotalSeconds;
        timeLeft -= path.MoveTo(corner);
        if (timeLeft <= 0.5f) // TODO: rethink this leeway...
            return DateTime.MaxValue;

        return Pathfind10(path, right);
    }

    // step 10: second row of hammers, either side
    private DateTime Pathfind10(PathBuilder path, bool right)
    {
        var hammers = right ? _mech5PairR2 : _mech5PairL2;
        var aoeOut = SequenceAOE(hammers, 0);
        var aoeIn = SequenceAOE(hammers, 1);
        var hammerCenter = (aoeIn.Origin + aoeOut.Origin) * 0.5f;
        var hammerDir = right ? new Vector3(0, 0, -1) : new Vector3(+0.70710678f, 0, -0.70710678f);
        var hammerOut = right ? new Vector3(+1, 0, 0) : new Vector3(-0.70710678f, 0, -0.70710678f);
        var hammerBase = hammerCenter - 1.5f * hammerDir;
        var relPos = path.StartPos - hammerBase;
        if (hammerDir.DotXZ(relPos) >= 0)
            return Pathfind11(path, right, hammerOut.DotXZ(relPos) > 0); // step is already done

        var pathIn = path.Branch("I");
        var pathOut = path.Branch("O");

        var cornerIn = hammerBase - 0.5f * hammerOut;
        var cornerOut = hammerBase + 0.5f * hammerOut;
        pathIn.MoveToAOEEdge(aoeIn, (cornerIn - path.StartPos).NormalizedXZ());
        pathIn.MoveTo(cornerIn);
        pathOut.MoveToAOEEdge(aoeOut, (cornerOut - path.StartPos).NormalizedXZ());
        pathOut.MoveTo(cornerOut);

        var eIn = Pathfind11(pathIn, right, false);
        var eOut = Pathfind11(pathOut, right, true);
        if (eIn < eOut)
        {
            path.Merge(pathIn);
            return eIn;
        }
        else
        {
            path.Merge(pathOut);
            return eOut;
        }
    }

    // step 11: along one side of the hammer on second row
    private DateTime Pathfind11(PathBuilder path, bool right, bool outer)
    {
        var hammers = right ? _mech5PairR2 : _mech5PairL2;
        var aoeOut = SequenceAOE(hammers, 0);
        var aoeIn = SequenceAOE(hammers, 1);
        var hammerCenter = (aoeIn.Origin + aoeOut.Origin) * 0.5f;
        var hammerDir = right ? new Vector3(0, 0, -1) : new Vector3(+0.70710678f, 0, -0.70710678f);
        var hammerEnd = hammerCenter + 1.5f * hammerDir;
        var relPos = path.StartPos - hammerEnd;
        if (hammerDir.DotXZ(relPos) >= 0)
            return Pathfind12(path, right); // step is already done

        var hammerOut = right ? new Vector3(+1, 0, 0) : new Vector3(-0.70710678f, 0, -0.70710678f);
        var corner = hammerEnd + hammerOut * (outer ? 0.5f : -0.5f);
        var aoe = outer ? aoeOut : aoeIn;
        var timeLeft = (aoe.NextActivation - path.StartTime).TotalSeconds;
        timeLeft -= path.MoveTo(corner);
        if (timeLeft <= 0.5f) // TODO: rethink this leeway...
            return DateTime.MaxValue;

        return Pathfind12(path, right);
    }

    // step 12: steps, avoiding exaflare
    private DateTime Pathfind12(PathBuilder path, bool right)
    {
        if (path.StartPos.Z > 152)
        {
            path.MoveTo(new Vector3(right ? 4 : -4, 31.8f, 152));
        }

        var dest = new Vector3(right ? 4 : -4, 0, 136); // very approximate end corridor entry point
        if (path.StartPos.Z > 151.7f)
        {
            var aoe = SequenceAOE(_mech6Exaflare, right ? 1 : 0);
            path.MoveToAOEEdge(aoe, (dest - path.StartPos).NormalizedXZ());
        }

        if (path.StartPos.Z > 150)
        {
            path.MoveTo(new(right ? 4 : -4, 32.6f, 150), true);
        }

        if (path.StartPos.Z > 148)
        {
            path.MoveTo(new(right ? 4 : -4, 32.6f, 148));
        }

        if (path.StartPos.Z > 146)
        {
            path.MoveTo(new(right ? 4 : -4, 33.6f, 146), true);
        }

        if (path.StartPos.Z > 144)
        {
            path.MoveTo(new(right ? 4 : -4, 33.6f, 144));
        }

        if (path.StartPos.Z > 142)
        {
            path.MoveTo(new(right ? 4 : -4, 34.3f, 142), true);
        }

        if (path.StartPos.Z > 140)
        {
            path.MoveTo(new(right ? 4 : -4, 34.3f, 140));
        }

        if (path.StartPos.Z > 138)
        {
            path.MoveTo(new(right ? 4 : -4, 35.6f, 138), true);
        }

        return Pathfind13(path, right);
    }

    // step 13: small hammers
    private DateTime Pathfind13(PathBuilder path, bool right)
    {
        var hammers = right ? _mech7PairR : _mech7PairL;
        var aoeOut = SequenceAOE(hammers, 0);
        var aoeIn = SequenceAOE(hammers, 1);
        var hammerCenter = (aoeIn.Origin + aoeOut.Origin) * 0.5f;
        var hammerOut = (aoeOut.Origin - aoeIn.Origin).NormalizedXZ();
        var hammerDir = right ? new Vector3(hammerOut.Z, 0, -hammerOut.X) : new Vector3(-hammerOut.Z, 0, hammerOut.X);
        var relPos = path.StartPos - hammerCenter;
        if (hammerDir.DotXZ(relPos) >= 0)
            return Pathfind14(path, right); // step is already done

        var pathIn = path.Branch("I");
        var pathOut = path.Branch("O");

        var pointIn = hammerCenter - hammerOut;
        var pointOut = hammerCenter + hammerOut;
        pathIn.MoveToAOEEdge(aoeIn, (pointIn - path.StartPos).NormalizedXZ());
        pathIn.MoveTo(pointIn);
        pathOut.MoveToAOEEdge(aoeOut, (pointOut - path.StartPos).NormalizedXZ());
        pathOut.MoveTo(pointOut);

        var eIn = Pathfind14(pathIn, right);
        var eOut = Pathfind14(pathOut, right);
        if (eIn < eOut)
        {
            path.Merge(pathIn);
            return eIn;
        }
        else
        {
            path.Merge(pathOut);
            return eOut;
        }
    }

    // step 14: towards exit
    private DateTime Pathfind14(PathBuilder path, bool right)
    {
        path.MoveTo(new(right ? 8 : -8, 39.6f, 124.5f));
        return path.StartTime;
    }
}
