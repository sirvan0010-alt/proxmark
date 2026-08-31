using System;
using PM5Control.Core.WirelessLab;

namespace PM5Control.Core.WirelessLab.WiFi;

public static class WiFiCapabilityIds
{
    public const byte SoftAp=0x01, Sta=0x02, Scan=0x03, PromiscuousRx=0x04, BeaconTx=0x05, ProbeReqTx=0x06, ProbeRspTx=0x07, ActionTx=0x08, DeauthTx=0x09, DisassocTx=0x0A, ApSta=0x0B, Band24=0x10, Band5=0x11, PowerSave=0x20, ModemSleep=0x21;
}

public sealed class WiFiCapabilityMatrix : CapabilityMatrixBase
{
    public string ChipModel { get; } = "ESP32-C2";
    public string ModuleName { get; } = "ESP8684-MINI-1";
    public string FirmwareVersion { get; set; } = "";
    public DateTime DiscoveredAt { get; set; }
    public WiFiCapabilityMatrix() { Initialize(); }
    private void Initialize()
    {
        RegisterDocumentedSupported(WiFiCapabilityIds.SoftAp,"SoftAP","Wi-Fi AP mode (2.4GHz)",WirelessCapabilityCategory.Connectivity,"ESP-IDF Wi-Fi API");
        RegisterDocumentedSupported(WiFiCapabilityIds.Sta,"STA","Wi-Fi station mode (2.4GHz)",WirelessCapabilityCategory.Connectivity,"ESP-IDF Wi-Fi API");
        RegisterDocumentedSupported(WiFiCapabilityIds.Scan,"Scan","2.4GHz Wi-Fi scanning",WirelessCapabilityCategory.Scanning,"ESP-IDF Wi-Fi scan API");
        RegisterDocumentedSupported(WiFiCapabilityIds.PromiscuousRx,"Promiscuous RX","Raw 802.11 frame reception",WirelessCapabilityCategory.Monitoring,"ESP-IDF promiscuous API");
        RegisterDocumentedSupported(WiFiCapabilityIds.BeaconTx,"Beacon TX","Beacon-frame transmit capability test",WirelessCapabilityCategory.FrameInjection,"ESP-IDF 802.11 TX API");
        RegisterDocumentedSupported(WiFiCapabilityIds.ProbeReqTx,"Probe Request TX","Probe request transmit capability test",WirelessCapabilityCategory.FrameInjection,"ESP-IDF 802.11 TX API");
        RegisterDocumentedSupported(WiFiCapabilityIds.ProbeRspTx,"Probe Response TX","Probe response transmit capability test",WirelessCapabilityCategory.FrameInjection,"ESP-IDF 802.11 TX API");
        RegisterDocumentedSupported(WiFiCapabilityIds.ActionTx,"Action TX","802.11 action-frame transmit capability test",WirelessCapabilityCategory.FrameInjection,"ESP-IDF 802.11 TX API");
        RegisterDocumentedPolicyDisabled(WiFiCapabilityIds.DeauthTx,"Deauth TX","Deauthentication transmit","FrameInjection","Active deauthentication is policy-gated","Project wireless policy");
        RegisterDocumentedPolicyDisabled(WiFiCapabilityIds.DisassocTx,"Disassoc TX","Disassociation transmit","FrameInjection","Active disassociation is policy-gated","Project wireless policy");
        RegisterDocumentedSupported(WiFiCapabilityIds.ApSta,"AP+STA","Concurrent AP and station operation",WirelessCapabilityCategory.Connectivity,"ESP-IDF APSTA mode");
        RegisterDocumentedSupported(WiFiCapabilityIds.Band24,"2.4 GHz","2.4GHz Wi-Fi band",WirelessCapabilityCategory.Connectivity,"ESP32-C2 datasheet");
        RegisterDocumentedNotSupported(WiFiCapabilityIds.Band5,"5 GHz","5GHz Wi-Fi band",WirelessCapabilityCategory.Connectivity,"ESP32-C2 is 2.4GHz-only");
        RegisterDocumentedSupported(WiFiCapabilityIds.PowerSave,"Power Save","Wi-Fi power-save modes",WirelessCapabilityCategory.PowerManagement,"ESP-IDF power-save API");
        RegisterDocumentedSupported(WiFiCapabilityIds.ModemSleep,"Modem Sleep","Wi-Fi modem-sleep mode",WirelessCapabilityCategory.PowerManagement,"ESP-IDF modem-sleep API");
    }
    public void ExposeVerifiedCapabilities() { foreach(var c in _capabilities.Values) c.RecomputeExposure(); }
}
