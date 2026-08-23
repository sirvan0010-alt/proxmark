namespace PM5Control.Core.Protocols.Bwm;

/// <summary>
/// Describes whether the host currently has a verified path to the BWM UART.
/// The PM5 USB serial port is not automatically the BWM UART: upstream PM5
/// documentation states that the ARM↔BWM communication driver is still TODO.
/// </summary>
public enum BwmTransportPath
{
    Unknown = 0,
    DirectBwmUart = 1,
    Pm5ArmBridge = 2,
}

public sealed record BwmTransportCapability(
    BwmTransportPath Path,
    bool CanSendReadOnlyCommands,
    string Evidence,
    string Limitation)
{
    public static BwmTransportCapability Unknown() => new(
        BwmTransportPath.Unknown,
        false,
        "No verified BWM transport path has been established.",
        "A Windows COM port alone does not prove access to the BWM UART.");

    public static BwmTransportCapability DirectBwmUart() => new(
        BwmTransportPath.DirectBwmUart,
        true,
        "The selected serial endpoint is explicitly verified as the BWM UART.",
        "This mode is intended for a directly accessible BWM UART endpoint, not the normal PM5 USB host port.");

    public static BwmTransportCapability Pm5ArmBridgeUnavailable() => new(
        BwmTransportPath.Pm5ArmBridge,
        false,
        "Upstream RfidResearchGroup/proxmark3 documents the PM5 ARM↔BWM communication driver as TODO.",
        "The normal PM5 USB/serial host connection therefore cannot be assumed to forward BWM binary commands yet.");
}
