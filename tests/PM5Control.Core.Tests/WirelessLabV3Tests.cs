using System.Linq;
using Xunit;
using PM5Control.Core.WirelessLab;
using PM5Control.Core.WirelessLab.WiFi;
using PM5Control.Core.WirelessLab.Bluetooth;

namespace PM5Control.Core.Tests;

public sealed class WirelessLabV3Tests
{
    [Fact]
    public void NotSupportedNeverPromotes()
    {
        var matrix = new WiFiCapabilityMatrix();
        matrix.RecordTestResult(WiFiCapabilityIds.Band5, CapabilityTestResult.Pass, "s");
        var cap = matrix.GetCapability(WiFiCapabilityIds.Band5)!;
        Assert.Equal(SupportStatus.NotSupported, cap.Support);
        Assert.Equal(EvidenceLevel.Documented, cap.Evidence);
        Assert.Equal(UIExposure.Hidden, cap.Exposure);
    }

    [Fact]
    public void PolicyDisabledLogsButDoesNotPromote()
    {
        var matrix = new WiFiCapabilityMatrix();
        matrix.RecordTestResult(WiFiCapabilityIds.DeauthTx, CapabilityTestResult.Pass, "s");
        var cap = matrix.GetCapability(WiFiCapabilityIds.DeauthTx)!;
        Assert.Equal(PolicyStatus.Disabled, cap.Policy);
        Assert.Equal(CapabilityTestResult.Pass, cap.LastTestResult);
        Assert.NotEqual(EvidenceLevel.HardwareVerified, cap.Evidence);
        Assert.Equal(UIExposure.Hidden, cap.Exposure);
    }

    [Fact]
    public void TestResultAloneCannotExposeCapability()
    {
        var matrix = new WiFiCapabilityMatrix();
        matrix.RecordTestResult(WiFiCapabilityIds.BeaconTx, CapabilityTestResult.Pass, "unbound");
        var cap = matrix.GetCapability(WiFiCapabilityIds.BeaconTx)!;
        Assert.Equal(EvidenceLevel.Tested, cap.Evidence);
        Assert.Equal(UIExposure.Hidden, cap.Exposure);
    }

    [Fact]
    public void SessionMustMatchEvidence()
    {
        var matrix = new WiFiCapabilityMatrix();
        var validator = new WirelessHardwareSessionValidator();
        var session = validator.StartSession(matrix.ModuleName, "Wi-Fi", "human");
        matrix.RecordTestResult(WiFiCapabilityIds.Scan, CapabilityTestResult.Pass, session.SessionId);
        var result = validator.CompleteSession(matrix, session.SessionId, matrix.ModuleName);
        var validation = result.Validations.Single(v => v.CapabilityId == WiFiCapabilityIds.Scan);
        Assert.True(validation.EvidenceMatchedSession);
        Assert.Equal(EvidenceLevel.HardwareVerified, validation.NewEvidence);
        Assert.True(matrix.IsHardwareVerified(WiFiCapabilityIds.Scan));
    }

    [Fact]
    public void WrongSessionDoesNotPromote()
    {
        var matrix = new WiFiCapabilityMatrix();
        var validator = new WirelessHardwareSessionValidator();
        var session = validator.StartSession(matrix.ModuleName, "Wi-Fi", "human");
        matrix.RecordTestResult(WiFiCapabilityIds.Scan, CapabilityTestResult.Pass, "other");
        var result = validator.CompleteSession(matrix, session.SessionId, matrix.ModuleName);
        var validation = result.Validations.Single(v => v.CapabilityId == WiFiCapabilityIds.Scan);
        Assert.False(validation.EvidenceMatchedSession);
        Assert.Equal(EvidenceLevel.Tested, matrix.GetCapability(WiFiCapabilityIds.Scan)!.Evidence);
    }

    [Fact]
    public void WrongDeviceIsRejected()
    {
        var matrix = new WiFiCapabilityMatrix();
        var validator = new WirelessHardwareSessionValidator();
        var session = validator.StartSession(matrix.ModuleName, "Wi-Fi", "human");
        Assert.Throws<System.InvalidOperationException>(() => validator.CompleteSession(matrix, session.SessionId, "different-device"));
    }

    [Fact]
    public void EmptyHumanTokenIsRejected()
    {
        var validator = new WirelessHardwareSessionValidator();
        Assert.Throws<System.InvalidOperationException>(() => validator.StartSession("ESP8684-MINI-1", "Wi-Fi", ""));
    }

    [Fact]
    public void Crc8RoundTripAndRejectsCorruption()
    {
        var frame = WirelessProtocol.BuildFrame(0x82, new byte[] { 1, 2, 3 });
        Assert.True(WirelessProtocol.TryParseFrame(frame, out var parsed, out var consumed));
        Assert.Equal(frame.Length, consumed);
        Assert.NotNull(parsed);
        frame[4] ^= 1;
        Assert.False(WirelessProtocol.TryParseFrame(frame, out _, out var badConsumed));
        Assert.Equal(1, badConsumed);
    }

    [Fact]
    public void PartialFrameWaits()
    {
        var frame = WirelessProtocol.BuildFrame(1, System.Array.Empty<byte>());
        Assert.False(WirelessProtocol.TryParseFrame(frame.AsSpan(0, 3), out _, out var consumed));
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void GarbageBeforeSofResynchronizes()
    {
        var frame = WirelessProtocol.BuildFrame(1, new byte[] { 7 });
        var buffer = new byte[frame.Length + 2];
        buffer[0] = 0x01; buffer[1] = 0x02; System.Array.Copy(frame, 0, buffer, 2, frame.Length);
        Assert.False(WirelessProtocol.TryParseFrame(buffer, out _, out var consumed));
        Assert.Equal(2, consumed);
        Assert.True(WirelessProtocol.TryParseFrame(buffer.AsSpan(consumed), out var parsed, out _));
        Assert.Equal(1, parsed!.Command);
    }

    [Fact]
    public void BackToBackFramesParseIndependently()
    {
        var a = WirelessProtocol.BuildFrame(1, new byte[] { 1 });
        var b = WirelessProtocol.BuildFrame(2, new byte[] { 2 });
        var all = new byte[a.Length + b.Length];
        System.Array.Copy(a, all, a.Length); System.Array.Copy(b, 0, all, a.Length, b.Length);
        Assert.True(WirelessProtocol.TryParseFrame(all, out _, out var consumed));
        Assert.True(WirelessProtocol.TryParseFrame(all.AsSpan(consumed), out var second, out _));
        Assert.Equal(2, second!.Command);
    }

    [Fact]
    public void BleClassicIsNotSupported()
    {
        var matrix = new BluetoothCapabilityMatrix();
        var cap = matrix.GetCapability(BluetoothCapabilityIds.BtClassic)!;
        Assert.Equal(SupportStatus.NotSupported, cap.Support);
        Assert.Equal(UIExposure.Hidden, cap.Exposure);
    }
}
