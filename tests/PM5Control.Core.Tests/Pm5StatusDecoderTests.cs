using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm5StatusDecoderTests
{
    [Fact]
    public void DecodeBattery_UsesUpstreamPackedLittleEndianLayout()
    {
        var payload = new byte[]
        {
            1, 1,
            75, 0,
            0xD8, 0x0F,
            0x2E, 0xFB,
            0x58, 0x02,
            0x20, 0x03,
            0x84, 0x03,
            0x6E, 0x01,
            0x1A, 0x01,
            0x00,
            0x02,
            0x63
        };

        var status = Pm5StatusDecoder.DecodeBattery(payload);

        Assert.True(status.BwmPresent);
        Assert.True(status.GaugeOk);
        Assert.Equal((ushort)75, status.SocPercent);
        Assert.Equal((ushort)4056, status.VoltageMv);
        Assert.Equal(-1234, status.CurrentMa);
        Assert.Equal((ushort)600, status.RemainingMah);
        Assert.Equal((ushort)800, status.FullChargeMah);
        Assert.Equal((ushort)388, status.DesignCapacityMah);
        Assert.Equal(36.6, status.TemperatureC10 / 10.0, 1);
        Assert.Equal((byte)0, status.ChargerFault);
        Assert.Equal((byte)2, status.ChargeStatus);
        Assert.Equal((byte)99, status.HealthPercent);
    }

    [Fact]
    public void DecodeBattery_RejectsWrongLength()
    {
        Assert.Throws<InvalidDataException>(() => Pm5StatusDecoder.DecodeBattery(new byte[18]));
    }

    [Fact]
    public void DecodeCepStatus_UsesBatteryPrefixAndNulTerminatedVersion()
    {
        var payload = new byte[Pm5StatusDecoder.CepStatusPayloadLength];
        payload[0] = 1;
        payload[1] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), 88);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), 4100);
        Encoding.UTF8.GetBytes("PM5-TEST").CopyTo(payload.AsSpan(20));

        var status = Pm5StatusDecoder.DecodeCepStatus(payload);

        Assert.True(status.CepActive);
        Assert.Equal((ushort)88, status.Battery.SocPercent);
        Assert.Equal((ushort)4100, status.Battery.VoltageMv);
        Assert.Equal("PM5-TEST", status.FirmwareVersion);
    }

    [Fact]
    public void StructuredCommands_AreReadOnlyProbes()
    {
        Assert.True(Pm3CommandCode.IsSafeReadOnlyProbe(Pm3CommandCode.Pm5BwmGetBattery));
        Assert.True(Pm3CommandCode.IsSafeReadOnlyProbe(Pm3CommandCode.CepStatus));
        Assert.False(Pm3CommandCode.IsSafeReadOnlyProbe(Pm3CommandCode.Pm5BwmEspOta));
        Assert.Equal((ushort)0x0184, Pm5StatusDecoder.BatteryCommand);
        Assert.Equal((ushort)0x0185, Pm5StatusDecoder.CepStatusCommand);
    }
}
