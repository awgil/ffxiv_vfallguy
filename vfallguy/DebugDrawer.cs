using Dalamud.Interface.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace vfallguy;

public unsafe class DebugDrawer
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetMatrixSingletonDelegate();

    private GetMatrixSingletonDelegate _getMatrixSingleton { get; init; }

    public SharpDX.Matrix ViewProj { get; private set; }
    public SharpDX.Matrix Proj { get; private set; }
    public SharpDX.Matrix View { get; private set; }
    public SharpDX.Matrix CameraWorld { get; private set; }
    public float CameraAzimuth { get; private set; } // facing north = 0, facing west = pi/4, facing south = +-pi/2, facing east = -pi/4
    public float CameraAltitude { get; private set; } // facing horizontally = 0, facing down = pi/4, facing up = -pi/4
    public SharpDX.Vector2 ViewportSize { get; private set; }

    private List<(Vector2 from, Vector2 to, uint col)> _worldDrawLines = new();
    private List<(Vector2 pos, uint col, string text)> _worldText = new();

    public DebugDrawer()
    {
        var funcAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
        _getMatrixSingleton = Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(funcAddress);
    }

    public void Update()
    {
        var matrixSingleton = _getMatrixSingleton();
        ViewProj = ReadMatrix(matrixSingleton + 0x1b4);
        Proj = ReadMatrix(matrixSingleton + 0x174);
        View = ViewProj * SharpDX.Matrix.Invert(Proj);
        CameraWorld = SharpDX.Matrix.Invert(View);
        CameraAzimuth = MathF.Atan2(View.Column3.X, View.Column3.Z);
        CameraAltitude = MathF.Asin(View.Column3.Y);
        ViewportSize = ReadVec2(matrixSingleton + 0x1f4);
    }

    public void DrawWorldPrimitives()
    {
        if (_worldDrawLines.Count == 0 && _worldText.Count == 0)
            return;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));
        ImGui.Begin("world_overlay", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
        ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

        var dl = ImGui.GetWindowDrawList();
        foreach (var l in _worldDrawLines)
            dl.AddLine(l.from, l.to, l.col, 2);
        var font = ImGui.GetFont();
        foreach (var t in _worldText)
            dl.AddText(font, 32, t.pos, t.col, t.text);
        _worldDrawLines.Clear();
        _worldText.Clear();

        ImGui.End();
        ImGui.PopStyleVar();
    }

    public void DrawWorldLine(Vector3 start, Vector3 end, uint color)
    {
        var p1 = new SharpDX.Vector3(start.X, start.Y, start.Z);
        var p2 = new SharpDX.Vector3(end.X, end.Y, end.Z);
        if (!ClipLineToNearPlane(ref p1, ref p2))
            return;

        p1 = SharpDX.Vector3.TransformCoordinate(p1, ViewProj);
        p2 = SharpDX.Vector3.TransformCoordinate(p2, ViewProj);
        var p1screen = new Vector2(0.5f * ViewportSize.X * (1 + p1.X), 0.5f * ViewportSize.Y * (1 - p1.Y)) + ImGuiHelpers.MainViewport.Pos;
        var p2screen = new Vector2(0.5f * ViewportSize.X * (1 + p2.X), 0.5f * ViewportSize.Y * (1 - p2.Y)) + ImGuiHelpers.MainViewport.Pos;
        _worldDrawLines.Add((p1screen, p2screen, color));
    }

    public void DrawWorldText(Vector3 pos, uint color, string text)
    {
        var p = new SharpDX.Vector3(pos.X, pos.Y, pos.Z);
        var n = ViewProj.Column3; // near plane
        if (SharpDX.Vector4.Dot(new(p, 1), n) <= 0)
            return;

        p = SharpDX.Vector3.TransformCoordinate(p, ViewProj);
        var pscreen = new Vector2(0.5f * ViewportSize.X * (1 + p.X), 0.5f * ViewportSize.Y * (1 - p.Y)) + ImGuiHelpers.MainViewport.Pos;
        _worldText.Add((pscreen, color, text));
    }

    public bool DrawWorldCircle(Vector3 center, float radius, uint color)
    {
        int numSegments = Geom.CalculateCircleSegments(radius, 2 * MathF.PI, 0.1f);
        var prev = center + new Vector3(0, 0, radius);
        for (int i = 1; i <= numSegments; ++i)
        {
            var angle = i * 2 * MathF.PI / numSegments;
            var dir = new Vector3(MathF.Sin(angle), 0, MathF.Cos(angle));
            var curr = center + radius * dir;
            DrawWorldLine(curr, prev, color);
            prev = curr;
        }
        return true;
    }

    public bool DrawWorldSquare(Vector3 center, float halfSide, uint color)
    {
        var a = center + new Vector3(-halfSide, 0, -halfSide);
        var b = center + new Vector3(-halfSide, 0, +halfSide);
        var c = center + new Vector3(+halfSide, 0, +halfSide);
        var d = center + new Vector3(+halfSide, 0, -halfSide);
        return DrawWorldRect(a, b, c, d, color);
    }

    public bool DrawWorldRect(Vector3 origin, float length, float halfWidth, float rotation, uint color)
    {
        var rd = new Vector3(MathF.Sin(rotation), 0, MathF.Cos(rotation));
        var rn = new Vector3(rd.Z, 0, -rd.X);
        var a = origin + halfWidth * rn;
        var b = origin - halfWidth * rn;
        var c = b + rd * length;
        var d = a + rd * length;
        return DrawWorldRect(a, b, c, d, color);
    }

    public bool DrawWorldRect(Vector3 a, Vector3 b, Vector3 c, Vector3 d, uint color)
    {
        DrawWorldLine(a, b, color);
        DrawWorldLine(b, c, color);
        DrawWorldLine(c, d, color);
        DrawWorldLine(d, a, color);
        return true;
    }

    private unsafe SharpDX.Matrix ReadMatrix(IntPtr address)
    {
        var p = (float*)address;
        SharpDX.Matrix mtx = new();
        for (var i = 0; i < 16; i++)
            mtx[i] = *p++;
        return mtx;
    }

    private unsafe SharpDX.Vector2 ReadVec2(IntPtr address)
    {
        var p = (float*)address;
        return new(p[0], p[1]);
    }

    private bool ClipLineToNearPlane(ref SharpDX.Vector3 a, ref SharpDX.Vector3 b)
    {
        var n = ViewProj.Column3; // near plane
        var an = SharpDX.Vector4.Dot(new(a, 1), n);
        var bn = SharpDX.Vector4.Dot(new(b, 1), n);
        if (an <= 0 && bn <= 0)
            return false;

        if (an < 0 || bn < 0)
        {
            var ab = b - a;
            var abn = SharpDX.Vector3.Dot(ab, new(n.X, n.Y, n.Z));
            var t = -an / abn;
            if (an < 0)
                a = a + t * ab;
            else
                b = a + t * ab;
        }
        return true;
    }
}
