using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
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
    private AOESequence _mech4RectsC;
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

    public Map3(GameEvents events) : base(events, BuildBaseMap(), _heightProfile, 123)
    {
        _mech1Rotating = CreateRotatingSequence(1.2f, 6, 267.5f, [-10, 10]);
        _mech2Exaflares = CreateDoubleExaflareSequence(9.39f, 251, 11.75f, 243);
        _mech3Rotating = CreateRotatingSequence(1.0f, 14.76f, 225.3f, [-10, 0, 10]);
        _mech3Exaflares = CreateDoubleExaflareSequence(14.54f, 229.3f, 14.97f, 221.3f);
        _mech4RectsL = CreateRectsSequence(-12, -6);
        _mech4RectsR = CreateRectsSequence(12, 6);
        _mech4RectsC = CreateRectsSequenceCenter();
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

    public override string Strats()
    {
        if (_mech2Exaflares.FirstIndex < 0 || _mech3Exaflares.FirstIndex < 0 || _mech4RectsL.FirstIndex < 0 || _mech4Exaflare.FirstIndex < 0)
            return "";

        bool mech3Left = _mech3Exaflares.FirstIndex is 1 or 2 or 3 or 6 or 7;
        var lane1 = _mech2Exaflares.FirstIndex switch
        {
            0 or 4 => mech3Left ? 1 : 4,
            1 or 5 => mech3Left ? 1 : 2,
            2 or 6 => mech3Left ? 2 : 3,
            3 or 7 => mech3Left ? 3 : 4,
            _ => 0
        };
        bool initialLeft = lane1 <= 2;
        // by the time we reach bottom of the ramp with rects, the full cycle should end - so if it started from outside, we'll be at '2'
        bool mech4InnerRectsWhenReached = _mech4RectsL.FirstIndex < 5;
        var lane2 = (_mech4Exaflare.FirstIndex + (mech4InnerRectsWhenReached ? 0 : 1)) % 4 + 1;
        return $"{(initialLeft ? "L" : "R")} -> {lane1} -> {(mech3Left ? "L" : "R")} -> {lane2} [{(mech4InnerRectsWhenReached ? "inner" : "outer")}, {_mech2Exaflares.FirstIndex % 4 + 1} {_mech3Exaflares.FirstIndex % 4 + 1} {_mech4Exaflare.FirstIndex % 4 + 1} {_mech6Exaflare.FirstIndex % 4 + 1}]";
    }

    protected override List<Waypoint> RebuildPath(Vector3 startPos, DateTime startTime)
    {
        if (_mech2Exaflares.FirstIndex < 0 || _mech3Exaflares.FirstIndex < 0 || _mech4RectsL.FirstIndex < 0 || _mech4Exaflare.FirstIndex < 0)
            return new();

        bool mech3Left = _mech3Exaflares.FirstIndex is 1 or 2 or 3 or 6 or 7;
        var lane1 = _mech2Exaflares.FirstIndex switch
        {
            0 or 4 => mech3Left ? 1 : 4,
            1 or 5 => mech3Left ? 1 : 2,
            2 or 6 => mech3Left ? 2 : 3,
            3 or 7 => mech3Left ? 3 : 4,
            _ => 0
        };
        bool initialLeft = lane1 <= 2;
        // by the time we reach bottom of the ramp with rects, the full cycle should end - so if it started from outside, we'll be at '2'
        bool mech4InnerRectsWhenReached = _mech4RectsL.FirstIndex < 5;
        var lane2 = (_mech4Exaflare.FirstIndex + (mech4InnerRectsWhenReached ? 0 : 1)) % 4 + 1;

        var res = new List<Waypoint>();
        void MoveTo(float x, float z) => res.Add(new() { Dest = new(x, HeightAt(z), z) });

        // TODO: first portal
        MoveTo(initialLeft ? -5.5f : 5.5f, 270.5f);
        MoveTo(initialLeft ? -5.5f : 5.5f, 263.5f);
        var lane1X = lane1 switch
        {
            1 => -9.5f,
            2 => -5.5f,
            3 => +5.5f,
            4 => +9.5f,
            _ => 0
        };
        MoveTo(lane1X, 255.5f);
        var lane1EndX = lane1 switch
        {
            2 => mech3Left ? -5.5f : -2f,
            3 => mech3Left ? +2f : +5.5f,
            _ => lane1X
        };
        MoveTo(lane1EndX, 238.5f);
        MoveTo(mech3Left ? -5 : +5, 228);
        MoveTo(mech3Left ? -5 : +5, 223);

        if (mech4InnerRectsWhenReached)
        {
            MoveTo(lane2 <= 2 ? -6 : +6, 203.5f);

            var midX = lane2 switch
            {
                1 => -12.5f,
                4 => +12.5f,
                _ => 0
            };
            MoveTo(midX, 194.5f);

            var topX = lane2 switch
            {
                1 => -12f,
                2 => -3.5f,
                3 => +3.5f,
                4 => +12f,
                _ => 0
            };
            MoveTo(topX, 188);
        }
        else
        {
            var lowX = lane2 switch
            {
                1 => mech3Left ? - 12 : 0,
                4 => mech3Left ? 0 : 12,
                _ => 0,
            };
            MoveTo(lowX, 212);
            MoveTo(lowX, 208);

            var midX = lane2 switch
            {
                1 => -12,
                2 => -6,
                3 => 6,
                4 => 12,
                _ => 0,
            };
            MoveTo(midX, 203);
        }

        var lane2X = lane2 switch
        {
            1 => -12,
            2 => -5,
            3 => 5,
            4 => 12,
            _ => 0,
        };
        MoveTo(lane2X, 145.5f);

        MoveTo(lane2X <= 2 ? -6.5f : 6.5f, 135);
        MoveTo(lane2X <= 2 ? -8.5f : 8.5f, 124);

        return res;
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
                UpdateSequence(casterPos, 0, _mech4RectsL, _mech4RectsR, _mech4RectsC);
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

    private AOESequence CreateRectsSequenceCenter()
    {
        (float y, float z, float d)[] l = [(25.59f, 190.4f, 0.5f), (23.37f, 196.4f, 0.5f), (21.15f, 202.4f, 0.5f), (18.94f, 208.4f, 0.5f), (16.73f, 214.4f, 4.2f)];
        var l1 = l.Select(e => (new Vector3(0, e.y, e.z), 0.0f, e.d));
        return CreateSequence(AOEShape.Square, 3, 0, l1);
    }

    private AOESequence CreatePairsSequence(float r, Vector3 p1, Vector3 p2)
    {
        return CreateSequence(AOEShape.Circle, r, 0, [(p1, 0.0f, 2.5f), (p2, 0.0f, 2.5f)]);
    }
}
