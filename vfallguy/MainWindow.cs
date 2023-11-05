using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace vfallguy;

public class MainWindow : Window, IDisposable
{
    private GameEvents _gameEvents = new();
    private DebugDrawer _drawer = new();
    private AutoJoinLeave _automation = new();
    private Map? _map;
    private DateTime _now;
    private Vector3 _prevPos;
    private Vector3 _movementDirection;
    private float _movementSpeed;
    private bool _autoJoin;
    private bool _autoLeaveIfNotSolo;
    private DateTime _autoJoinAt = DateTime.MaxValue;
    private DateTime _autoLeaveAt = DateTime.MaxValue;
    private int _numPlayersInDuty;
    private float _autoJoinDelay = 0.5f;
    private float _autoLeaveDelay = 3;
    private int _autoLeaveLimit = 1;

    public MainWindow() : base("vfailguy")
    {
        ShowCloseButton = false;
        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
        _map?.Dispose();
        _gameEvents.Dispose();
        _automation.Dispose();
    }

    public unsafe override void PreOpenCheck()
    {
        _automation.Update();
        _drawer.Update();

        _now = DateTime.Now;
        var playerPos = Service.ClientState.LocalPlayer?.Position ?? new();
        _prevPos = playerPos;
        _movementDirection = playerPos - _prevPos;
        _movementSpeed = _movementDirection.Length() / Framework.Instance()->FrameDeltaTime;
        _movementDirection = _movementDirection.NormalizedXZ();

        IsOpen = Service.ClientState.TerritoryType is 1165 or 1197 && !Service.Condition[ConditionFlag.BetweenAreas];

        UpdateMap();
        UpdateAutoJoin();
        UpdateAutoLeave();
        DrawOverlays();

        _drawer.DrawWorldPrimitives();
    }

    public unsafe override void Draw()
    {
        if (ImGui.Button("Queue"))
            _automation.RegisterForDuty();
        ImGui.SameLine();
        if (ImGui.Button("Leave"))
            _automation.LeaveDuty();
        ImGui.SameLine();
        ImGui.TextUnformatted($"Num players in duty: {_numPlayersInDuty} (autoleave: {(_autoLeaveAt == DateTime.MaxValue ? "never" : $"in {(_autoLeaveAt - _now).TotalSeconds:f1}s")})");

        ImGui.Checkbox("Auto register", ref  _autoJoin);
        if (_autoJoin)
        {
            ImGui.SameLine();
            ImGui.SliderFloat("Delay###j", ref _autoJoinDelay, 0, 10);
        }
        ImGui.Checkbox("Auto leave if not solo", ref _autoLeaveIfNotSolo);
        if (_autoLeaveIfNotSolo)
        {
            ImGui.SameLine();
            ImGui.SliderFloat("Delay###l", ref _autoLeaveDelay, 0, 10);
            ImGui.SameLine();
            ImGui.SliderInt("Limit", ref _autoLeaveLimit, 1, 23);
        }

        if (_map != null)
        {
            ImGui.TextUnformatted($"Pos: {_map.PlayerPos}");
            ImGui.TextUnformatted($"Path: {_map.Path.Count}");
            ImGui.TextUnformatted($"Speed: {_movementSpeed}");

            foreach (var aoe in _map.AOEs.Where(aoe => aoe.NextActivation != default))
            {
                var nextActivation = (aoe.NextActivation - _now).TotalSeconds;
                using (ImRaii.PushColor(ImGuiCol.Text, nextActivation < 0 ? 0xff0000ff : 0xffffffff))
                    ImGui.TextUnformatted($"{aoe.Type} R{aoe.R1} @ {aoe.Origin}: activate in {nextActivation:f3}, repeat={aoe.Repeat}, seqd={aoe.SeqDelay}");
            }
        }
    }

    private void UpdateMap()
    {
        Type? mapType = null;
        if (IsOpen)
        {
            if (Service.ClientState.TerritoryType == 1197)
            {
                //mapType = typeof(MapTest);
            }
            else
            {
                var pos = Service.ClientState.LocalPlayer!.Position;
                if (pos.X is >= -40 and <= 40 && pos.Z is >= 100 and <= 350)
                    mapType = typeof(Map3);
            }
        }

        if (_map?.GetType() != mapType)
        {
            _map?.Dispose();
            _map = null;
            if (mapType != null)
                _map = (Map?)Activator.CreateInstance(mapType, _gameEvents);
        }

        _map?.Update();
    }

    private void UpdateAutoJoin()
    {
        bool wantAutoJoin = _autoJoin && _automation.Idle && IsOpen && Service.ClientState.TerritoryType == 1197 && !Service.Condition[ConditionFlag.WaitingForDutyFinder] && !Service.Condition[ConditionFlag.BetweenAreas];
        if (!wantAutoJoin)
        {
            _autoJoinAt = DateTime.MaxValue;
        }
        else if (_autoJoinAt == DateTime.MaxValue)
        {
            Service.Log.Info($"Auto-joining in {_autoJoinDelay:f2}s...");
            _autoJoinAt = _now.AddSeconds(_autoJoinDelay);
        }
        else if (_now >= _autoJoinAt)
        {
            Service.Log.Info($"Auto-joining");
            _automation.RegisterForDuty();
            _autoJoinAt = DateTime.MaxValue;
        }
    }

    private void UpdateAutoLeave()
    {
        _numPlayersInDuty = Service.ClientState.TerritoryType == 1165 && Service.Condition[ConditionFlag.BoundByDuty] && !Service.Condition[ConditionFlag.BetweenAreas]
            ? Service.ObjectTable.Count(o => o.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            : 0;
        bool wantAutoLeave = _autoLeaveIfNotSolo && _numPlayersInDuty > _autoLeaveLimit && _automation.Idle;
        if (!wantAutoLeave)
        {
            _autoLeaveAt = DateTime.MaxValue;
        }
        else if (_autoLeaveAt == DateTime.MaxValue)
        {
            Service.Log.Info($"Auto-leaving in {_autoLeaveDelay:f2}s...");
            _autoLeaveAt = _now.AddSeconds(_autoLeaveDelay);
        }
        else if (_now >= _autoLeaveAt)
        {
            Service.Log.Info($"Auto-leaving: {_numPlayersInDuty} players");
            _automation.LeaveDuty();
            _autoLeaveAt = DateTime.MaxValue;
        }
    }

    private void DrawOverlays()
    {
        if (_map == null)
            return;

        var from = _map.PlayerPos;
        for (int i = 0; i < _map.Path.Count; ++i)
        {
            var wp = _map.Path[i];
            var delay = (wp.StartMoveAt - _now).TotalSeconds;
            _drawer.DrawWorldLine(from, wp.Dest, i > 0 ? 0xff00ffff : delay <= 0 ? 0xff00ff00 : 0xff0000ff);
            if (delay > 0)
                _drawer.DrawWorldText(from, 0xff0000ff, $"{delay:f3}");
            from = wp.Dest;
        }

        foreach (var aoe in _map.AOEs.Where(aoe => aoe.NextActivation != default))
        {
            var nextActivation = (aoe.NextActivation - _now).TotalSeconds;
            if (nextActivation < 2.5f)
            {
                bool risky = false;
                if (_movementSpeed > 0)
                {
                    var (aoeEnter, aoeExit) = aoe.Intersect(_map.PlayerPos, _movementDirection);
                    if (!float.IsNaN(aoeEnter) && aoe.ActivatesBetween(_now, aoeEnter * Map.InvSpeed, aoeExit * Map.InvSpeed) is var delay && delay > 0)
                        risky = true;
                }

                aoe.Draw(_drawer, risky ? 0xff0000ff : 0xff00ffff);
                var dir = (aoe.Origin - _map.PlayerPos).NormalizedXZ();
                var (enter, exit) = aoe.Intersect(_map.PlayerPos, dir);
                var textPos = _map.PlayerPos + dir * MathF.Max(enter, 0);
                _drawer.DrawWorldText(textPos, risky ? 0xff0000ff : 0xff00ffff, $"{nextActivation:f3}");
            }
        }
    }
}
