using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace vfallguy;

public unsafe class DebugDrawer
{
    public Vector3 Origin;
    public Matrix4x4 View;
    public Matrix4x4 Proj;
    public Matrix4x4 ViewProj;
    public Vector4 NearPlane;
    public float CameraAzimuth; // facing north = 0, facing west = pi/4, facing south = +-pi/2, facing east = -pi/4
    public float CameraAltitude; // facing horizontally = 0, facing down = pi/4, facing up = -pi/4
    public Vector2 ViewportSize;

    private List<(Vector2 from, Vector2 to, uint col)> _worldDrawLines = [];
    private List<(Vector2 pos, uint col, string text)> _worldText = [];

    public void Update()
    {
        var controlCamera = CameraManager.Instance()->GetActiveCamera();
        var renderCamera = controlCamera != null ? controlCamera->SceneCamera.RenderCamera : null;
        if (renderCamera == null)
            return;

        Origin = renderCamera->Origin;
        View = renderCamera->ViewMatrix;
        View.M44 = 1; // for whatever reason, game doesn't initialize it...
        Proj = renderCamera->ProjectionMatrix;
        ViewProj = View * Proj;

        // note that game uses reverse-z by default, so we can't just get full plane equation by reading column 3 of vp matrix
        // so just calculate it manually: column 3 of view matrix is plane equation for a plane equation going through origin
        // proof:
        // plane equation p is such that p.dot(Q, 1) = 0 if Q lines on the plane => pw = -Q.dot(n); for view matrix, V43 is -origin.dot(forward)
        // plane equation for near plane has Q.dot(n) = O.dot(n) - near => pw = V43 + near
        NearPlane = new(View.M13, View.M23, View.M33, View.M43 + renderCamera->NearPlane);

        CameraAzimuth = MathF.Atan2(View.M13, View.M33);
        CameraAltitude = MathF.Asin(View.M23);
        var device = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance();
        ViewportSize = new(device->Width, device->Height);
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
        var p1w = start;
        var p2w = end;
        if (!ClipLineToNearPlane(ref p1w, ref p2w))
            return;

        var p1p = Vector4.Transform(p1w, ViewProj);
        var p2p = Vector4.Transform(p2w, ViewProj);
        var p1c = new Vector2(p1p.X, p1p.Y) * (1 / p1p.W);
        var p2c = new Vector2(p2p.X, p2p.Y) * (1 / p2p.W);
        var p1screen = new Vector2(0.5f * ViewportSize.X * (1 + p1c.X), 0.5f * ViewportSize.Y * (1 - p1c.Y)) + ImGuiHelpers.MainViewport.Pos;
        var p2screen = new Vector2(0.5f * ViewportSize.X * (1 + p2c.X), 0.5f * ViewportSize.Y * (1 - p2c.Y)) + ImGuiHelpers.MainViewport.Pos;
        _worldDrawLines.Add((p1screen, p2screen, color));
    }

    public void DrawWorldText(Vector3 pos, uint color, string text)
    {
        var pn = Vector4.Dot(new(pos, 1), NearPlane);
        if (pn >= 0)
            return;

        var pp = Vector4.Transform(pos, ViewProj);
        var pc = new Vector2(pp.X, pp.Y) * (1 / pp.W);
        var pscreen = new Vector2(0.5f * ViewportSize.X * (1 + pc.X), 0.5f * ViewportSize.Y * (1 - pc.Y)) + ImGuiHelpers.MainViewport.Pos;
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

    private bool ClipLineToNearPlane(ref Vector3 a, ref Vector3 b)
    {
        var an = Vector4.Dot(new(a, 1), NearPlane);
        var bn = Vector4.Dot(new(b, 1), NearPlane);
        if (an >= 0 && bn >= 0)
            return false; // line fully behind near plane

        if (an > 0 || bn > 0)
        {
            var ab = b - a;
            var abn = Vector3.Dot(ab, new(NearPlane.X, NearPlane.Y, NearPlane.Z));
            var t = -an / abn;
            var p = a + t * ab;
            if (an > 0)
                a = p;
            else
                b = p;
        }
        return true;
    }
}
