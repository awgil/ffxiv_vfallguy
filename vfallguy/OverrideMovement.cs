using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace vfallguy;

[StructLayout(LayoutKind.Explicit, Size = 0x18)]
public unsafe struct PlayerMoveControllerFlyInput
{
    [FieldOffset(0x0)] public float Forward;
    [FieldOffset(0x4)] public float Left;
    [FieldOffset(0x8)] public float Up;
    [FieldOffset(0xC)] public float Turn;
    [FieldOffset(0x10)] public float u10;
    [FieldOffset(0x14)] public byte DirMode;
    [FieldOffset(0x15)] public byte HaveBackwardOrStrafe;
}

[StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
public unsafe struct CameraEx
{
    [FieldOffset(0x130)] public float DirH; // 0 is north, increases CW
    [FieldOffset(0x134)] public float DirV; // 0 is horizontal, positive is looking up, negative looking down
    [FieldOffset(0x138)] public float InputDeltaHAdjusted;
    [FieldOffset(0x13C)] public float InputDeltaVAdjusted;
    [FieldOffset(0x140)] public float InputDeltaH;
    [FieldOffset(0x144)] public float InputDeltaV;
    [FieldOffset(0x148)] public float DirVMin; // -85deg by default
    [FieldOffset(0x14C)] public float DirVMax; // +45deg by default
}

public unsafe class OverrideMovement : IDisposable
{
    public bool Enabled
    {
        get => _rmiWalkHook.IsEnabled;
        set
        {
            if (value)
            {
                _rmiWalkHook.Enable();
            }
            else
            {
                _rmiWalkHook.Disable();
            }
        }
    }

    public bool IgnoreUserInput; // if true - override even if user tries to change camera orientation, otherwise override only if user does nothing
    public Vector3 DesiredPosition;
    public float Precision = 0.1f;

    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    [Signature("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D")]
    private Hook<RMIWalkDelegate> _rmiWalkHook = null!;

    public OverrideMovement()
    {
        Service.Hook.InitializeFromAttributes(this);
        Service.Log.Information($"RMIWalk address: 0x{_rmiWalkHook.Address:X}");
    }

    public void Dispose()
    {
        _rmiWalkHook.Dispose();
    }

    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        _rmiWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);
        // TODO: we really need to introduce some extra checks that PlayerMoveController::readInput does - sometimes it skips reading input, and returning something non-zero breaks stuff...
        if (bAdditiveUnk == 0 && (IgnoreUserInput || *sumLeft == 0 && *sumForward == 0) && DirectionToDestination(false) is var relDir && relDir != null)
        {
            *sumLeft = MathF.Sin(relDir.Value);
            *sumForward = MathF.Cos(relDir.Value);
        }
    }

    private float? DirectionToDestination(bool allowVertical)
    {
        var player = Service.ClientState.LocalPlayer;
        if (player == null)
            return null;

        var dist = DesiredPosition - player.Position;
        if (dist.LengthSquared() <= Precision * Precision)
            return null;

        var dir = MathF.Atan2(dist.X, dist.Z);

        var camera = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
        var cameraDir = camera->DirH + MathF.PI;
        return dir - cameraDir;
    }
}
