using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace vfallguy;

public unsafe class AutoJoinLeave : IDisposable
{
    private delegate void AbandonDuty(bool a1);
    private AbandonDuty _abandonDuty;

    private List<Func<bool>> _actions = new();

    public AutoJoinLeave()
    {
        _abandonDuty = Marshal.GetDelegateForFunctionPointer<AbandonDuty>(Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 43 28 B1 01"));
    }

    public void Dispose()
    {
    }

    public bool Idle => _actions.Count == 0;

    public void RegisterForDuty()
    {
        _actions.Add(() =>
        {
            if (Service.Condition[ConditionFlag.BoundByDuty])
                return true;

            if (Service.Condition[ConditionFlag.BetweenAreas])
                return false;

            var registrator = Service.ObjectTable.FirstOrDefault(o => o.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc && o.DataId == 0xFF7A8);
            if (registrator == null)
                return false;

            TargetSystem.Instance()->InteractWithObject((GameObject*)registrator.Address);
            return true;
        });

        _actions.Add(() =>
        {
            if (Service.Condition[ConditionFlag.BoundByDuty])
                return true;

            var addon = RaptureAtkUnitManager.Instance()->GetAddonByName("FGSEnterDialog");
            if (addon == null || !addon->IsVisible || addon->UldManager.LoadedState != AtkLoadState.Loaded)
                return false;

            Service.Log.Debug($"registering...");
            var eventData = new AtkEvent();
            var inputData = stackalloc int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            addon->ReceiveEvent(AtkEventType.ButtonClick, 0, &eventData, (nint)inputData);
            return true;
        });

        _actions.Add(() =>
        {
            if (Service.Condition[ConditionFlag.BoundByDuty])
                return true;

            var addon = RaptureAtkUnitManager.Instance()->GetAddonByName("ContentsFinderConfirm");
            if (addon == null || !addon->IsVisible || addon->UldManager.LoadedState != AtkLoadState.Loaded)
                return false;

            Service.Log.Debug($"commencing...");
            var eventData = new AtkEvent();
            var inputData = stackalloc int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            addon->ReceiveEvent(AtkEventType.ButtonClick, 8, &eventData, (nint)inputData);
            return true;
        });
    }

    public void LeaveDuty()
    {
        _actions.Add(() => {
            if (!Service.Condition[ConditionFlag.BoundByDuty])
                return true;
            Service.Log.Debug("leaving...");
            _abandonDuty(false);
            return true;
        });

        _actions.Add(() => {
            if (!Service.Condition[ConditionFlag.BoundByDuty])
                return true;
            if (!Service.Condition[ConditionFlag.OccupiedInCutSceneEvent])
                return false; // wait a bit for a cutscene to start...
            Service.Log.Debug("leaving for real...");
            _abandonDuty(false);
            return true;
        });
    }

    public void Update()
    {
        while (_actions.Count > 0 && _actions.First()())
            _actions.RemoveAt(0);
    }

    public static AtkValue SynthesizeEvent(AgentInterface* receiver, ulong eventKind, Span<AtkValue> args)
    {
        AtkValue res = new();
        receiver->ReceiveEvent(&res, args.GetPointer(0), (uint)args.Length, eventKind);
        return res;
    }
}
