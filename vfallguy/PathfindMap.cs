using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace vfallguy;

// voxelized 3d 'spacetime'
public class PathfindMap
{
    public float SpaceResolution { get; private init; } // voxel size, in world units
    public float TimeResolution { get; private init; } // voxel size, in seconds
    public int Width { get; private init; } // in voxels, x coord
    public int Height { get; private init; } // in voxels, y coord
    public int Duration { get; private init; } // in voxels, t coord
    public BitArray Voxels { get; private set; } // true if voxel is blocked

    public Vector2 Center { get; private init; }
    public float InvSpaceResolution { get; private init; }
    public float InvTimeResolution { get; private init; }

    public bool this[int x, int y, int t] => InBounds(x, y, t) ? Voxels[t * Width * Height + y * Width + x] : false;

    public PathfindMap(float spaceResolution, float timeResolution, Vector3 center, float worldHalfWidth, float worldHalfHeight, float maxDuration)
    {
        SpaceResolution = spaceResolution;
        TimeResolution = timeResolution;
        Width = 2 * (int)MathF.Ceiling(worldHalfWidth / spaceResolution);
        Height = 2 * (int)MathF.Ceiling(worldHalfHeight / spaceResolution);
        Duration = (int)MathF.Ceiling(maxDuration / timeResolution);
        Voxels = new(Width * Height * Duration);

        Center = center.XZ();
        InvSpaceResolution = 1.0f / SpaceResolution;
        InvTimeResolution = 1.0f / TimeResolution;
    }

    public PathfindMap Clone()
    {
        var res = (PathfindMap)MemberwiseClone();
        res.Voxels = new(Voxels);
        return res;
    }

    public int ClampX(int x) => Math.Clamp(x, 0, Width - 1);
    public int ClampZ(int z) => Math.Clamp(z, 0, Height - 1);
    public int ClampT(int t) => Math.Clamp(t, 0, Duration - 1);

    public Vector2 WorldToGridFrac(Vector3 world) => new Vector2(Width / 2, Height / 2) + (world.XZ() - Center) * InvSpaceResolution;
    public (int x, int y) FracToGrid(Vector2 frac) => ((int)MathF.Floor(frac.X), (int)MathF.Floor(frac.Y));
    public (int x, int y) WorldToGrid(Vector3 world) => FracToGrid(WorldToGridFrac(world));
    public bool InBounds(int x, int y, int t) => x >= 0 && x < Width && y >= 0 && y < Height && t >= 0 && t < Duration;

    public Vector3 GridToWorld(int gx, int gy, float fx, float fy)
    {
        var p = Center + new Vector2(gx - Width / 2 + fx, gy - Height / 2 + fy) * SpaceResolution;
        // TODO: reconstruct Y from height profile
        return new(p.X, 0, p.Y);
    }

    // block all the voxels for which the shape function returns true for specified time intervals
    public bool BlockPixelsInside(Vector2 boundsMin, Vector2 boundsMax, Func<Vector2, bool> shape, float tStart, float tDuration, float tRepeat, float tLeeway)
    {
        List<(int begin, int end)> intTime = new();
        var tMax = Duration * TimeResolution;
        for (var t = tStart; t < tMax; t += tRepeat)
        {
            var ib = ClampT((int)MathF.Floor((t - tLeeway) * InvTimeResolution));
            var ie = ClampT((int)MathF.Floor((t + tDuration + tLeeway) * InvTimeResolution));
            intTime.Add((ib, ie));
        }
        if (intTime.Count == 0)
            return false;

        var relMin = boundsMin - Center;
        var relMax = boundsMax - Center;
        var xmin = ClampX(Width / 2 + (int)MathF.Floor(relMin.X * InvSpaceResolution));
        var xmax = ClampX(Width / 2 + (int)MathF.Ceiling(relMax.X * InvSpaceResolution));
        var ymin = ClampZ(Height / 2 + (int)MathF.Floor(relMin.Y * InvSpaceResolution));
        var ymax = ClampZ(Height / 2 + (int)MathF.Ceiling(relMax.Y * InvSpaceResolution));

        var tOff = Width * Height;
        var cy = Center + new Vector2(xmin - Width / 2 + 0.5f, ymin - Height / 2 + 0.5f) * SpaceResolution;
        for (int y = ymin; y <= ymax; ++y)
        {
            var cx = cy;
            for (int x = xmin; x <= xmax; ++x)
            {
                if (shape(cx))
                {
                    var xy = y * Width + x;
                    foreach (var (t0, t1) in intTime)
                    {
                        for (int t = t0; t <= t1; ++t)
                        {
                            Voxels[t * tOff + xy] = true;
                        }
                    }
                }
                cx.X += SpaceResolution;
            }
            cy.Y += SpaceResolution;
        }
        return true;
    }

    public bool BlockPixelsInsideCircle(Vector2 center, float radius, float tStart, float tDuration, float tRepeat, float tLeeway)
    {
        var vr = new Vector2(radius);
        var rsq = radius * radius;
        return BlockPixelsInside(center - vr, center + vr, v => (v - center).LengthSquared() <= rsq, tStart, tDuration, tRepeat, tLeeway);
    }

    public bool BlockPixelsInsideAlignedRect(float xmin, float xmax, float ymin, float ymax, float tStart, float tDuration, float tRepeat, float tLeeway)
    {
        return BlockPixelsInside(new(xmin, ymin), new(xmax, ymax), _ => true, tStart, tDuration, tRepeat, tLeeway);
    }

    public bool BlockPixelsInsideSquare(Vector2 center, float halfSide, float tStart, float tDuration, float tRepeat, float tLeeway)
    {
        return BlockPixelsInsideAlignedRect(center.X - halfSide, center.X + halfSide, center.Y - halfSide, center.Y + halfSide, tStart, tDuration, tRepeat, tLeeway);
    }
}
