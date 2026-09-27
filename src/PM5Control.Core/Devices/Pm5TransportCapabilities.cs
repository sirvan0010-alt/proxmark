namespace PM5Control.Core.Devices;

/// <summary>
/// Capability state exposed by the PM5 ARM firmware. This is deliberately
/// separate from physical presence and runtime link state.
/// </summary>
public enum Pm5CapabilityState
{
    Unknown,
    NotCompiled,
    Compiled,
    Reported
}

/// <summary>
/// Transport/capability matrix derived from CMD_CAPABILITIES and explicit
/// transport observations. A compiled transport is not the same as an
/// attached/usable transport.
/// </summary>
public sealed record Pm5TransportCapabilities(
    Pm5CapabilityState Usb,
    Pm5CapabilityState Bwm,
    Pm5CapabilityState Cep,
    Pm5CapabilityState BleForwarding,
    Pm5CapabilityState WifiForwarding,
    int CapabilitiesSchemaVersion,
    ushort MaxCommandDataSize,
    uint BigBufferSize)
{
    public static Pm5TransportCapabilities FromCapabilities(
        int schemaVersion,
        bool viaUsb,
        bool compiledWithBwm,
        bool compiledWithCep,
        uint bigBufferSize,
        ushort maxCommandDataSize)
        => new(
            viaUsb ? Pm5CapabilityState.Reported : Pm5CapabilityState.Unknown,
            schemaVersion >= 12
                ? (compiledWithBwm ? Pm5CapabilityState.Compiled : Pm5CapabilityState.NotCompiled)
                : Pm5CapabilityState.Unknown,
            schemaVersion >= 13
                ? (compiledWithCep ? Pm5CapabilityState.Compiled : Pm5CapabilityState.NotCompiled)
                : Pm5CapabilityState.Unknown,
            compiledWithBwm ? Pm5CapabilityState.Compiled : Pm5CapabilityState.Unknown,
            compiledWithBwm ? Pm5CapabilityState.Compiled : Pm5CapabilityState.Unknown,
            schemaVersion,
            maxCommandDataSize,
            bigBufferSize);
}
