using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace vfallguy;

public static class Geom
{
    public static Vector3 IntersectZPlane(Vector3 a, Vector3 b, float z)
    {
        // p = a + ab*t, p.z == z => t = (z - a.z) / (b.z - a.z)
        var ab = b - a;
        var t = (z - a.Z) / ab.Z;
        return a + ab * t;
    }

    public static Vector3 IntersectXPlane(Vector3 a, Vector3 b, float x)
    {
        // p = a + ab*t, p.x == x => t = (x - a.x) / (b.x - a.x)
        var ab = b - a;
        var t = (x - a.X) / ab.X;
        return a + ab * t;
    }

    public static Vector3 ClampX(Vector3 v, float min, float max) => new(Math.Clamp(v.X, min, max), v.Y, v.Z);

    public static Vector2 XZ(this Vector3 v) => new(v.X, v.Z);
    public static float DotXZ(this Vector3 a, Vector3 b) => a.X * b.X + a.Z * b.Z;
    public static float LengthXZSq(this Vector3 v) => v.X * v.X + v.Z * v.Z;
    public static float LengthXZ(this Vector3 v) => MathF.Sqrt(v.LengthXZSq());
    public static Vector3 NormalizedXZ(this Vector3 v) => v / v.LengthXZ();

    public static float MaxAbsCoordXZ(this Vector3 v) => Math.Max(v.X, v.Z);

    public static (float, float) IntersectRayCircle(Vector3 origin, float radius, Vector3 start, Vector3 dir)
    {
        var oa = start - origin;
        var dirDotOA = dir.DotXZ(oa);
        var d = MathF.Sqrt(dirDotOA * dirDotOA - oa.LengthXZSq() + radius * radius);
        return (-dirDotOA - d, -dirDotOA + d);
    }

    public static (float, float) IntersectRaySquare(Vector3 origin, float halfSide, Vector3 start, Vector3 dir)
    {
        var oa = start - origin;
        var (enterX, exitX) = dir.X switch
        {
            > 0.05f => ((-halfSide - oa.X) / dir.X, (halfSide - oa.X) / dir.X),
            < -0.05f => ((halfSide - oa.X) / dir.X, (-halfSide - oa.X) / dir.X),
            _ => Math.Abs(oa.X) <= halfSide ? (float.MinValue, float.MaxValue) : (float.NaN, float.NaN)
        };
        var (enterZ, exitZ) = dir.Z switch
        {
            > 0.05f => ((-halfSide - oa.Z) / dir.Z, (halfSide - oa.Z) / dir.Z),
            < -0.05f => ((halfSide - oa.Z) / dir.Z, (-halfSide - oa.Z) / dir.Z),
            _ => Math.Abs(oa.Z) <= halfSide ? (float.MinValue, float.MaxValue) : (float.NaN, float.NaN)
        };
        if (float.IsNaN(enterX) || float.IsNaN(enterZ))
            return (float.NaN, float.NaN);
        return (Math.Max(enterX, enterZ), Math.Min(exitX, exitZ));
    }

    public static (float, float) IntersectRayRect(Vector3 origin, float length, float halfWidth, float rot, Vector3 start, Vector3 dir)
    {
        var rd = new Vector3(MathF.Sin(rot), 0, MathF.Cos(rot));
        var rn = new Vector3(rd.Z, 0, -rd.X);
        var a = origin + halfWidth * rn;
        var b = origin - halfWidth * rn;
        var c = b + rd * length;
        var d = a + rd * length;
        var enter = float.NaN;
        var exit = float.NaN;
        foreach (var (p, q) in EnumerateEdges(a, b, c, d))
        {
            var t = IntersectRaySegment(p, q, start, dir);
            if (t is >= 0 and <= 1)
            {
                var ed = q - p;
                var en = new Vector3(ed.Z, 0, -ed.X);
                var inter = p + ed * t;
                var dist = (inter - start).LengthXZ();
                bool startInside = en.DotXZ(start - p) > 0;
                // note: rect is convex => max 1 enter and 1 exit
                if (startInside)
                    exit = dist;
                else
                    enter = dist;
            }
        }
        return (enter, exit);
    }

    public static bool RectContains(Vector3 origin, float length, float halfWidth, float rot, Vector3 point)
    {
        var rd = new Vector3(MathF.Sin(rot), 0, MathF.Cos(rot));
        var rn = new Vector3(rd.Z, 0, -rd.X);
        var a = origin + halfWidth * rn;
        var b = origin - halfWidth * rn;
        var c = b + rd * length;
        var d = a + rd * length;
        foreach (var (p, q) in EnumerateEdges(a, b, c, d))
        {
            var ed = q - p;
            var en = new Vector3(ed.Z, 0, -ed.X);
            bool inside = en.DotXZ(point - p) > 0;
            if (!inside)
                return false;
        }
        return true;
    }

    public static int CalculateCircleSegments(float radius, float angularLengthRad, float maxError)
    {
        // select max angle such that tesselation error is smaller than desired
        // error = R * (1 - cos(phi/2)) => cos(phi/2) = 1 - error/R
        float tessAngle = 2 * MathF.Acos(1 - MathF.Min(maxError / radius, 1));
        int tessNumSegments = (int)MathF.Ceiling(angularLengthRad / tessAngle);
        tessNumSegments = (tessNumSegments + 1) & ~1; // round up to even for symmetry
        return Math.Clamp(tessNumSegments, 4, 512);
    }

    private static float IntersectRaySegment(Vector3 a, Vector3 b, Vector3 start, Vector3 dir)
    {
        var rayNormal = new Vector3(dir.Z, 0, -dir.X);
        return rayNormal.DotXZ(start - a) / rayNormal.DotXZ(b - a);
    }

    private static IEnumerable<(Vector3, Vector3)> EnumerateEdges(params Vector3[] vertices)
    {
        var from = vertices.Last();
        foreach (var v in vertices)
        {
            yield return (from, v);
            from = v;
        }
    }

    public static ref T Ref<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];
    public static Span<T> AsSpan<T>(this List<T> list) => CollectionsMarshal.AsSpan(list);
}
