namespace PM5Control.Core.Devices;

[Flags]
public enum Pm5BootloaderFlags : uint
{
    None = 0,
    BootromPresent = 1 << 0,
    OsImagePresent = 1 << 1,
    CurrentModeBootrom = 1 << 2,
    CurrentModeOs = 1 << 3,
    UnderstandsStartFlash = 1 << 4,
    UnderstandsChipInfo = 1 << 5,
    UnderstandsVersion = 1 << 6,
    UnderstandsReadMemory = 1 << 7,
    UnderstandsChipType = 1 << 8,
    UnderstandsBwmStream = 1 << 9
}

/// <summary>
/// Read-only interpretation of the bootloader CMD_DEVICE_INFO OLD-frame
/// response. BWM stream support is a bootrom build capability; a nonzero
/// BWM baud additionally indicates that the bootrom detected a responding ESP.
/// </summary>
public sealed record Pm5BootloaderDeviceInfo(
    Pm5BootloaderFlags Flags,
    uint InfoVersion,
    uint BwmBaudRate)
{
    public bool BootromPresent => Flags.HasFlag(Pm5BootloaderFlags.BootromPresent);
    public bool IsInBootromMode => Flags.HasFlag(Pm5BootloaderFlags.CurrentModeBootrom);
    public bool UnderstandsBwmStream => Flags.HasFlag(Pm5BootloaderFlags.UnderstandsBwmStream);
    public bool BwmModuleResponded => UnderstandsBwmStream && BwmBaudRate > 0;
    public bool BwmBootloaderBridgeReady => UnderstandsBwmStream && BwmModuleResponded;

    public static bool TryParse(Pm3OldFrame frame, out Pm5BootloaderDeviceInfo? info)
    {
        info = null;
        if (frame.Command != Pm3OldFrameCodec.CommandDeviceInfo ||
            frame.Arg0 > uint.MaxValue ||
            frame.Arg1 > uint.MaxValue ||
            frame.Arg2 > uint.MaxValue)
            return false;

        info = new Pm5BootloaderDeviceInfo(
            (Pm5BootloaderFlags)(uint)frame.Arg0,
            (uint)frame.Arg1,
            (uint)frame.Arg2);
        return true;
    }
}
