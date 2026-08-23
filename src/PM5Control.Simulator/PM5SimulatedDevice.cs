namespace PM5Control.Simulator;

/// <summary>
/// Deterministic PM5 behavioural model for offline development.
/// It is not evidence of real hardware behaviour.
/// </summary>
public sealed class PM5SimulatedDevice
{
    public DeviceState State { get; } = new();

    public SimulatedResponse Execute(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        return command.Trim().ToUpperInvariant() switch
        {
            "GET_DEVICE_INFO" => SimulatedResponse.Unknown("Device-specific identity is not verified yet."),
            "GET_FIRMWARE_INFO" => SimulatedResponse.Unknown("Firmware information is intentionally unknown until sourced or observed."),
            "GET_CAPABILITIES" => SimulatedResponse.Unknown("Capabilities are intentionally unknown until sourced or observed."),
            "GET_BWM_STATUS" => State.BwmAvailable
                ? SimulatedResponse.Ok("BWM_AVAILABLE")
                : SimulatedResponse.Unknown("BWM availability has not been verified."),
            _ => SimulatedResponse.Error("UNSUPPORTED_SIMULATED_COMMAND")
        };
    }
}

public sealed record SimulatedResponse(string Status, string? Value, string Evidence)
{
    public static SimulatedResponse Ok(string value) => new("OK", value, "SIMULATED");
    public static SimulatedResponse Unknown(string reason) => new("UNKNOWN", null, reason);
    public static SimulatedResponse Error(string reason) => new("ERROR", null, reason);
}
