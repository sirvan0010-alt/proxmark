using System.Buffers.Binary;
using System.Text;

namespace PM5Control.Core.Protocols.Pm3;

/// <summary>
/// Source-backed decoder for the structured PM5 status commands added upstream
/// in October 2026. This is a protocol model only until observed on real hardware.
/// </summary>
public sealed record Pm5BwmBatteryStatus(
    bool BwmPresent,
    bool GaugeOk,
    ushort SocPercent,
    ushort VoltageMv,
    short CurrentMa,
    ushort RemainingMah,
    ushort FullChargeMah,
    ushort DesignCapacityMah,
    short TemperatureC10,
    byte ChargerFault,
    byte ChargeStatus,
    byte HealthPercent);

public sealed record Pm5CepStatus(
    bool CepActive,
    Pm5BwmBatteryStatus Battery,
    string FirmwareVersion);

public static class Pm5StatusDecoder
{
    public const int BatteryPayloadLength = 19;
    public const int CepStatusPayloadLength = 44;
    public const ushort BatteryCommand = Pm3CommandCode.Pm5BwmGetBattery;
    public const ushort CepStatusCommand = Pm3CommandCode.CepStatus;

    public static Pm5BwmBatteryStatus DecodeBattery(ReadOnlySpan<byte> payload)
    {
        if (payload.Length != BatteryPayloadLength)
            throw new InvalidDataException($"CMD_PM5_BWM_GET_BATTERY payload must be {BatteryPayloadLength} bytes, got {payload.Length}.");

        return new Pm5BwmBatteryStatus(
            payload[0] != 0,
            payload[1] != 0,
            BinaryPrimitives.ReadUInt16LittleEndian(payload[2..]),
            BinaryPrimitives.ReadUInt16LittleEndian(payload[4..]),
            BinaryPrimitives.ReadInt16LittleEndian(payload[6..]),
            BinaryPrimitives.ReadUInt16LittleEndian(payload[8..]),
            BinaryPrimitives.ReadUInt16LittleEndian(payload[10..]),
            BinaryPrimitives.ReadUInt16LittleEndian(payload[12..]),
            BinaryPrimitives.ReadInt16LittleEndian(payload[14..]),
            payload[16],
            payload[17],
            payload[18]);
    }

    public static Pm5CepStatus DecodeCepStatus(ReadOnlySpan<byte> payload)
    {
        if (payload.Length != CepStatusPayloadLength)
            throw new InvalidDataException($"CMD_CEP_STATUS payload must be {CepStatusPayloadLength} bytes, got {payload.Length}.");

        var battery = DecodeBattery(payload[..BatteryPayloadLength]);
        var versionBytes = payload.Slice(20, 24);
        var nul = versionBytes.IndexOf((byte)0);
        if (nul >= 0)
            versionBytes = versionBytes[..nul];

        return new Pm5CepStatus(
            payload[0] != 0,
            battery,
            Encoding.UTF8.GetString(versionBytes));
    }
}
