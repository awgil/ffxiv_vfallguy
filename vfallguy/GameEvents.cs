using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace vfallguy;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ActionEffectHeader
{
    public ulong animationTargetId;  // who the animation targets
    public uint actionId; // what the casting player casts, shown in battle log / ui
    public uint globalEffectCounter;
    public float animationLockTime;
    public uint SomeTargetID;
    public ushort SourceSequence; // 0 = initiated by server, otherwise corresponds to client request sequence id
    public ushort rotation;
    public ushort actionAnimationId;
    public byte variation; // animation
    public ActionType actionType;
    public byte unknown20;
    public byte NumTargets; // machina calls it 'effectCount', but it is misleading imo
    public ushort padding21;
    public ushort padding22;
    public ushort padding23;
    public ushort padding24;
}

public unsafe class GameEvents : IDisposable
{
    public delegate void ActionEffectEventDelegate(uint actionId, Vector3 casterPos);
    public event ActionEffectEventDelegate? ActionEffectEvent;

    public delegate void StartCastEventDelegate(uint actionId, Vector3 casterPos);
    public event StartCastEventDelegate? StartCastEvent;

    private delegate void ProcessActionEffectPacketDelegate(uint casterId, Character* casterObj, Vector3* targetPos, ActionEffectHeader* header, ulong* effects, ulong* targets);
    [Signature("E8 ?? ?? ?? ?? 48 8B 4C 24 68 48 33 CC E8 ?? ?? ?? ?? 4C 8D 5C 24 70 49 8B 5B 20 49 8B 73 28 49 8B E3 5F C3")]
    private Hook<ProcessActionEffectPacketDelegate> _processActionEffectPacketHook = null!;

    private delegate void StartCastDelegate(Character* self, ActionType actionType, uint actionId, ushort* intPos, float rot, float castTime);
    [Signature("E8 ?? ?? ?? ?? 41 80 7E ?? ?? 0F 85 ?? ?? ?? ?? F3 0F 10 1D")]
    private Hook<StartCastDelegate> _startCastHook = null!;

    public GameEvents()
    {
        Service.Hook.InitializeFromAttributes(this);
        Service.Log.Information($"_processActionEffectPacketHook: 0x{_processActionEffectPacketHook.Address:X}");
        Service.Log.Information($"_startCastHook: 0x{_startCastHook.Address:X}");

        _processActionEffectPacketHook.Enable();
        _startCastHook.Enable();
    }

    public void Dispose()
    {
        _processActionEffectPacketHook.Dispose();
        _startCastHook.Dispose();
    }

    private void ProcessActionEffectPacketDetour(uint casterId, Character* casterObj, Vector3* targetPos, ActionEffectHeader* header, ulong* effects, ulong* targets)
    {
        _processActionEffectPacketHook.Original(casterId, casterObj, targetPos, header, effects, targets);
        if (header->actionType == ActionType.Action)
            ActionEffectEvent?.Invoke(header->actionId, casterObj->GameObject.Position);
    }

    private void StartCastDetour(Character* self, ActionType actionType, uint actionId, ushort* intPos, float rot, float castTime)
    {
        _startCastHook.Original(self, actionType, actionId, intPos, rot, castTime);
        if (actionType == ActionType.Action)
            StartCastEvent?.Invoke(actionId, self->GameObject.Position);
    }
}
