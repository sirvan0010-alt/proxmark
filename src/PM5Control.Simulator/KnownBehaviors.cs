namespace PM5Control.Simulator;

/// <summary>
/// Behaviour contracts that are safe to test without pretending they are PM5 facts.
/// Each contract must gain stronger evidence when real hardware becomes available.
/// </summary>
public static class KnownBehaviors
{
    public const string DeterministicResponses = "SIMULATOR-001";
    public const string UnknownDataRemainsUnknown = "SIMULATOR-002";
    public const string UnsupportedCommandIsExplicit = "SIMULATOR-003";
}
