using System;
using System.Numerics;

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

    public static int CalculateCircleSegments(float radius, float angularLengthRad, float maxError)
    {
        // select max angle such that tesselation error is smaller than desired
        // error = R * (1 - cos(phi/2)) => cos(phi/2) = 1 - error/R
        float tessAngle = 2 * MathF.Acos(1 - MathF.Min(maxError / radius, 1));
        int tessNumSegments = (int)MathF.Ceiling(angularLengthRad / tessAngle);
        tessNumSegments = (tessNumSegments + 1) & ~1; // round up to even for symmetry
        return Math.Clamp(tessNumSegments, 4, 512);
    }
}
