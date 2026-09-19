namespace PM5Control.Core.Protocols.Pm3;

/// <summary>Payload limits for the PM3 command transport variants used by PM5 Control Center.</summary>
public enum Pm3TransportKind
{
    /// <summary>Current PM5 USB/CDC NG command channel.</summary>
    Pm5Usb,
    /// <summary>Legacy PM3 command framing.</summary>
    Legacy,
    /// <summary>PM3 data forwarded through the BWM/FPC path.</summary>
    BwmFpc
}

public static class Pm3TransportLimits
{
    public static int MaxPayload(Pm3TransportKind transport) => transport switch
    {
        Pm3TransportKind.Pm5Usb => Pm3NgFrame.MaxPayload,
        Pm3TransportKind.Legacy => Pm3NgFrame.LegacyMaxPayload,
        Pm3TransportKind.BwmFpc => Pm3NgFrame.FpcMaxPayload,
        _ => throw new ArgumentOutOfRangeException(nameof(transport), transport, null)
    };

    public static bool Fits(Pm3TransportKind transport, int payloadLength) =>
        payloadLength >= 0 && payloadLength <= MaxPayload(transport);
}
