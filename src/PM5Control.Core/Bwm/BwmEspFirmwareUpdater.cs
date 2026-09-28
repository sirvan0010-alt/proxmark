using System.Buffers.Binary;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Bwm;

/// <summary>
/// Source-verified BWM ESP32-C2 OTA protocol.
///
/// Upstream RfidResearchGroup/proxmark3 defines CMD_PM5_BWM_ESP_OTA (0x017E):
/// [action:u8] + action-specific data, with 240-byte maximum WRITE chunks.
/// The updater is deliberately transport-independent, so the same operation
/// works over USB, BLE-forwarding or native PM5 Wi-Fi/TCP once that transport
/// implements IPm3CommandTransport.
/// </summary>
public sealed class BwmEspFirmwareUpdater
{
    public const ushort Command = 0x017E;
    public const byte ActionBegin = 0x00;
    public const byte ActionWrite = 0x01;
    public const byte ActionEnd = 0x02;
    public const byte ActionAbort = 0x03;
    public const byte ActionVersion = 0x04;
    public const byte ActionReboot = 0x05;
    public const int MaxChunkSize = 240;
    public const ushort Esp32C2ChipId = 0x000C;

    private readonly IPm3CommandTransport _transport;

    public BwmEspFirmwareUpdater(IPm3CommandTransport transport)
        => _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    public static BwmEspImageInfo InspectImage(ReadOnlySpan<byte> image)
    {
        if (image.Length < 16) return BwmEspImageInfo.Invalid("Image is shorter than the ESP extended image header.");
        if (image[0] != 0xE9) return BwmEspImageInfo.Invalid($"Invalid ESP image magic 0x{image[0]:X2}; expected 0xE9.");
        var chipId = BinaryPrimitives.ReadUInt16LittleEndian(image.Slice(12, 2));
        if (chipId != Esp32C2ChipId) return BwmEspImageInfo.Invalid($"Wrong ESP target chip id 0x{chipId:X4}; expected ESP32-C2 0x{Esp32C2ChipId:X4}.");
        return new BwmEspImageInfo(true, image.Length, chipId, "ESP32-C2 image header accepted");
    }

    public async Task<string?> ReadVersionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _transport.SendCommandAsync(Command, new[] { ActionVersion }, cancellationToken).ConfigureAwait(false);
        return response.Response.Status == 0 ? System.Text.Encoding.UTF8.GetString(response.Response.Payload).TrimEnd('\0') : null;
    }

    public async Task UpdateAsync(ReadOnlyMemory<byte> image, IProgress<BwmEspUpdateProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var info = InspectImage(image.Span);
        if (!info.IsValid) throw new InvalidDataException(info.Error);
        if (image.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(image));

        await _transport.SendCommandAsync(Command, BeginPayload((uint)image.Length), cancellationToken).ConfigureAwait(false);
        try
        {
            var sent = 0;
            while (sent < image.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = Math.Min(MaxChunkSize, image.Length - sent);
                var payload = new byte[count + 1];
                payload[0] = ActionWrite;
                image.Span.Slice(sent, count).CopyTo(payload.AsSpan(1));
                var response = await _transport.SendCommandAsync(Command, payload, cancellationToken).ConfigureAwait(false);
                if (response.Response.Status != 0)
                    throw new IOException($"BWM OTA WRITE failed at {sent}/{image.Length}; status={response.Response.Status}, reason={response.Response.Reason}.");
                sent += count;
                progress?.Report(new BwmEspUpdateProgress(sent, image.Length));
            }

            var end = await _transport.SendCommandAsync(Command, new[] { ActionEnd }, cancellationToken).ConfigureAwait(false);
            if (end.Response.Status != 0)
                throw new IOException($"BWM OTA END failed; status={end.Response.Status}, reason={end.Response.Reason}.");
        }
        catch
        {
            try { await _transport.SendCommandAsync(Command, new[] { ActionAbort }, CancellationToken.None).ConfigureAwait(false); } catch { }
            throw;
        }
    }

    private static byte[] BeginPayload(uint size)
    {
        var payload = new byte[5];
        payload[0] = ActionBegin;
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(1), size);
        return payload;
    }
}

public sealed record BwmEspImageInfo(bool IsValid, int Length, ushort ChipId, string Error)
{
    public static BwmEspImageInfo Invalid(string error) => new(false, 0, 0, error);
}

public readonly record struct BwmEspUpdateProgress(int BytesSent, int TotalBytes)
{
    public double Fraction => TotalBytes == 0 ? 0 : (double)BytesSent / TotalBytes;
}
