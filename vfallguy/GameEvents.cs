using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Sound;

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

    private Hook<ActionEffectHandler.Delegates.Receive> _processActionEffectPacketHook = null!;

    private delegate void StartCastDelegate(Character* self, ActionType actionType, uint actionId, ushort* intPos, float rot, float castTime);
    private Hook<StartCastDelegate> _startCastHook = null!;

    public GameEvents()
    {
        Service.Hook.InitializeFromAttributes(this);
        _processActionEffectPacketHook = Service.Hook.HookFromAddress<ActionEffectHandler.Delegates.Receive>((nint)ActionEffectHandler.MemberFunctionPointers.Receive, ProcessActionEffectPacketDetour);
        _processActionEffectPacketHook.Enable();

        _startCastHook = Service.Hook.HookFromSignature<StartCastDelegate>("E8 ?? ?? ?? ?? 80 7E 22 11", StartCastDetour);
		_startCastHook.Enable();
        Service.Log.Information($"_processActionEffectPacketHook: 0x{_processActionEffectPacketHook.Address:X}");
        Service.Log.Information($"_startCastHook: 0x{_startCastHook.Address:X}");
    }

    public void Dispose()
    {
        _processActionEffectPacketHook.Dispose();
        _startCastHook.Dispose();
    }

    private void ProcessActionEffectPacketDetour(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds)
    {
        _processActionEffectPacketHook.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
        if (header->ActionType == (byte)ActionType.Action)
            ActionEffectEvent?.Invoke(header->ActionType, casterPtr->GameObject.Position);
    }

    private void StartCastDetour(Character* self, ActionType actionType, uint actionId, ushort* intPos, float rot, float castTime)
    {
        _startCastHook.Original(self, actionType, actionId, intPos, rot, castTime);
        if (actionType == ActionType.Action)
            StartCastEvent?.Invoke(actionId, self->GameObject.Position);
    }
}
