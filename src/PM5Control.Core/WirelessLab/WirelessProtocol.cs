using System;

namespace PM5Control.Core.WirelessLab;

public static class WirelessProtocol
{
    public const byte FrameSof = 0xAA;
    public const byte FrameEof = 0x55;
    public const int MaxPayloadLength = 240;
    public const int MinFrameLength = 5;
    public const int MaxFrameLength = 245;
    public const byte CmdPing = 0x01, CmdGetCapabilities = 0x02, CmdRunTest = 0x03, CmdStartSoftAp = 0x04, CmdStopSoftAp = 0x05, CmdStartScan = 0x06, CmdStartSniffer = 0x07, CmdStopSniffer = 0x08, CmdSetPowerMode = 0x09, CmdGetStatus = 0x0A;
    public const byte CmdStartBleScan = 0x10, CmdStopBleScan = 0x11, CmdStartBleAdv = 0x12, CmdStopBleAdv = 0x13;
    public const byte EvtPong = 0x81, EvtCapabilityResult = 0x82, EvtTestResult = 0x83, EvtScanResult = 0x84, EvtSnifferFrame = 0x85, EvtApClient = 0x86, EvtStatus = 0x87, EvtBleScanResult = 0x88, EvtBleAdvResult = 0x89, EvtError = 0xFF;
    public const byte ResultPass = 0x01, ResultFail = 0x02, ResultError = 0x03, ResultTimeout = 0x04;

    public static byte CalculateCrc8Ccitt(ReadOnlySpan<byte> data)
    {
        byte crc = 0;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++) crc = (byte)((crc & 0x80) != 0 ? (crc << 1) ^ 0x07 : crc << 1);
        }
        return crc;
    }

    public static byte[] BuildFrame(byte command, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > MaxPayloadLength) throw new ArgumentException($"Payload too large: {payload.Length} > {MaxPayloadLength}", nameof(payload));
        var frame = new byte[MinFrameLength + payload.Length];
        frame[0] = FrameSof; frame[1] = command; frame[2] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(3));
        frame[3 + payload.Length] = CalculateCrc8Ccitt(frame.AsSpan(1, 2 + payload.Length));
        frame[4 + payload.Length] = FrameEof;
        return frame;
    }

    public static bool TryParseFrame(ReadOnlySpan<byte> buffer, out ParsedFrame? frame, out int bytesConsumed)
    {
        frame = null; bytesConsumed = 0;
        int sof = buffer.IndexOf(FrameSof);
        if (sof < 0) { bytesConsumed = buffer.Length; return false; }
        if (sof > 0) { bytesConsumed = sof; return false; }
        if (buffer.Length < 3) return false;
        int total = MinFrameLength + buffer[2];
        if (buffer[2] > MaxPayloadLength) { bytesConsumed = 1; return false; }
        if (buffer.Length < total) return false;
        if (buffer[total - 1] != FrameEof) { bytesConsumed = 1; return false; }
        byte expected = CalculateCrc8Ccitt(buffer.Slice(1, 2 + buffer[2]));
        if (buffer[3 + buffer[2]] != expected) { bytesConsumed = 1; return false; }
        frame = new ParsedFrame(buffer[1], buffer.Slice(3, buffer[2]).ToArray());
        bytesConsumed = total; return true;
    }
}

public sealed record ParsedFrame(byte Command, byte[] Payload);
