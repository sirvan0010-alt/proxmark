using System.Buffers.Binary;
using PM5Control.Core.Bwm;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class BwmEspFirmwareUpdaterTests
{
    [Fact]
    public void InspectImage_AcceptsEsp32C2Header()
    {
        var image = new byte[32];
        image[0] = 0xE9;
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(12, 2), BwmEspFirmwareUpdater.Esp32C2ChipId);

        var result = BwmEspFirmwareUpdater.InspectImage(image);

        Assert.True(result.IsValid);
        Assert.Equal(0x000C, result.ChipId);
    }

    [Fact]
    public void InspectImage_RejectsWrongChip()
    {
        var image = new byte[32];
        image[0] = 0xE9;
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(12, 2), 0x0005);

        var result = BwmEspFirmwareUpdater.InspectImage(image);

        Assert.False(result.IsValid);
        Assert.Contains("Wrong ESP target", result.Error);
    }

    [Fact]
    public async Task Update_UsesVerified240ByteChunksAndBeginEnd()
    {
        var image = new byte[500];
        image[0] = 0xE9;
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(12, 2), BwmEspFirmwareUpdater.Esp32C2ChipId);
        var transport = new RecordingCommandTransport();
        var updater = new BwmEspFirmwareUpdater(transport);

        await updater.UpdateAsync(image);

        Assert.Equal(4, transport.Commands.Count);
        Assert.Equal(BwmEspFirmwareUpdater.Command, transport.Commands[0].Command);
        Assert.Equal(BwmEspFirmwareUpdater.ActionBegin, transport.Commands[0].Payload[0]);
        Assert.Equal(241, transport.Commands[1].Payload.Length);
        Assert.Equal(21, transport.Commands[2].Payload.Length);
        Assert.Equal(BwmEspFirmwareUpdater.ActionEnd, transport.Commands[3].Payload[0]);
    }

    private sealed class RecordingCommandTransport : IPm3CommandTransport
    {
        public List<(ushort Command, byte[] Payload)> Commands { get; } = new();
        public bool IsConnected => true;

        public Task<Pm3NgExchange> SendCommandAsync(ushort command, ReadOnlyMemory<byte> payload = default, CancellationToken cancellationToken = default)
        {
            Commands.Add((command, payload.ToArray()));
            var response = new Pm3NgResponse(command, 0, 0, Array.Empty<byte>(), Array.Empty<byte>());
            return Task.FromResult(new Pm3NgExchange(Array.Empty<byte>(), response, Array.Empty<Pm3NgResponse>(), Array.Empty<Pm3NgResponse>()));
        }
    }
}
