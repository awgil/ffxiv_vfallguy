using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace vfallguy;

//public class Map1A : Map
//{
//    private List<AOESequence> _mech1Rotating = new();
//    private List<AOESequence> _mech1Pillars = new();
//    private List<AOESequence> _mech2Pendulums = new();
//    private List<AOESequence> _mech3Pillars = new();

//    public Map1A(GameEvents events) : base(events)
//    {
//        for (int i = 0; i < 5; ++i)
//        {
//            _mech1Rotating.Add(CreateRotatingSequence(new(-10, 10, -202 - 10 * i)));
//            _mech1Rotating.Add(CreateRotatingSequence(new(+10, 10, -202 - 10 * i)));
//            if ((i & 1) == 0)
//                _mech1Rotating.Add(CreateRotatingSequence(new(0, 10, -202 - 10 * i)));
//        }

//        _mech1Pillars.Add(CreatePillarSequence(2.1f, 10, -248.5f, 20.58f, -17.6f, 2.98f));
//        _mech1Pillars.Add(CreatePillarSequence(2.1f, 10, -251.5f, 19.71f, -3, 16.71f));

//        _mech2Pendulums.Add(CreatePendulumSequence(-281, false));
//        _mech2Pendulums.Add(CreatePendulumSequence(-288, true));
//        _mech2Pendulums.Add(CreatePendulumSequence(-301, false));
//        _mech2Pendulums.Add(CreatePendulumSequence(-308, true));
//        _mech2Pendulums.Add(CreatePendulumSequence(-321, false));
//        _mech2Pendulums.Add(CreatePendulumSequence(-328, true));

//        _mech3Pillars.Add(CreatePillarSequence(1.6f, 13.67f, -347.8f, 3.5f, -6.88f, -3.77f, 0, 3.74f, 6.86f));
//        _mech3Pillars.Add(CreatePillarSequence(1.6f, 15.97f, -355.8f, 3.5f, -6.88f, -3.50f, 0, 3.50f, 6.85f));
//        _mech3Pillars.Add(CreatePillarSequence(1.6f, 18.15f, -363.8f, 3.5f, -6.88f, -3.77f, 0, 3.74f, 6.86f));

//        //foreach (var a in AOEs)
//        //    Service.Log.Info($"aoe: {a.Type} r={a.R1} @ {a.Origin}");
//    }

//    protected override void OnActionEffect(uint actionId, Vector3 casterPos)
//    {
//        switch (actionId)
//        {
//            case 34773:
//                UpdateSequence(casterPos, 0, _mech1Rotating);
//                break;
//            case 34774:
//                UpdateSequence(casterPos, 0, _mech1Pillars.Concat(_mech3Pillars));
//                break;
//            case 34775:
//                UpdateSequence(casterPos, 0, _mech2Pendulums);
//                break;
//        }
//    }

//    private AOESequence CreateRotatingSequence(Vector3 pos)
//    {
//        // i think i need a better thing than aoesequence here (a single aoe with a non-zero duration?)
//        float[] delays = [0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 3.3f];
//        return CreateSequence(AOEShape.Circle, 5, 0, delays.Select(d => (pos, 0.0f, d)));
//    }

//    private AOESequence CreatePillarSequence(float delay, float y, float z, float length, params float[] xs)
//    {
//        List<(Vector3, float, float)> l = new();
//        for (int i = 0; i < xs.Length - 1; ++i)
//            l.Add((new Vector3(xs[i], y, z), MathF.PI * 0.5f, delay));
//        for (int i = 0; i < xs.Length - 1; ++i)
//            l.Add((new Vector3(xs[xs.Length - 1 - i], y, z), -MathF.PI * 0.5f, delay));
//        return CreateSequence(AOEShape.Rect, length, 1.5f, l);
//    }

//    private AOESequence CreatePendulumSequence(float z, bool left)
//    {
//        List<(Vector3, float, float)> l = new()
//        {
//            (new(left ? -5 : 5, 10, z), 0, 0.2f),
//            (new(left ? -2 : 2, 10, z), 0, 0.1f),
//            (new(left ? 1 : -1, 10, z), 0, 1.8f),
//            (new(left ? 1 : -1, 10, z), 0, 0.2f),
//            (new(left ? -2 : 2, 10, z), 0, 0.1f),
//            (new(left ? -5 : 5, 10, z), 0, 1.8f)
//        };
//        return CreateSequence(AOEShape.Square, 1.5f, 0, l);
//    }
//}
