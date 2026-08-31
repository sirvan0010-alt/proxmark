using System;
using PM5Control.Core.WirelessLab;
namespace PM5Control.Core.WirelessLab.Bluetooth;
public static class BluetoothCapabilityIds { public const byte BleSupported=1,Ble50=2,BleAdvertising=3,BleScanning=4,BleConnection=5,BleGattServer=6,BleGattClient=7,BleRssi=8,BlePhy1M=0x10,BlePhy2M=0x11,BlePhyCoded=0x12,BleExtendedAdv=0x13,BtClassic=0x20,BtEdr=0x21; }
public sealed class BluetoothCapabilityMatrix : CapabilityMatrixBase
{
    public string ChipModel { get; } = "ESP32-C2"; public string ModuleName { get; } = "ESP8684-MINI-1"; public string FirmwareVersion { get; set; } = ""; public DateTime DiscoveredAt { get; set; }
    public BluetoothCapabilityMatrix(){Initialize();}
    private void Initialize(){
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleSupported,"BLE Supported","Bluetooth Low Energy",WirelessCapabilityCategory.Connectivity,"ESP32-C2 datasheet");
        RegisterDocumentedSupported(BluetoothCapabilityIds.Ble50,"BLE 5.0","BLE 5.x feature set",WirelessCapabilityCategory.Informational,"ESP32-C2 datasheet");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleAdvertising,"BLE Advertising","BLE advertising",WirelessCapabilityCategory.Connectivity,"ESP-IDF BLE GAP API");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleScanning,"BLE Scanning","BLE scanning",WirelessCapabilityCategory.Scanning,"ESP-IDF BLE GAP API");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleConnection,"BLE Connection","BLE connection establishment",WirelessCapabilityCategory.Connectivity,"ESP-IDF BLE GAP API");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleGattServer,"BLE GATT Server","GATT server role",WirelessCapabilityCategory.Connectivity,"ESP-IDF GATT API");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleGattClient,"BLE GATT Client","GATT client role",WirelessCapabilityCategory.Connectivity,"ESP-IDF GATT API");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleRssi,"BLE RSSI","Received signal strength",WirelessCapabilityCategory.Informational,"ESP-IDF BLE API");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BlePhy1M,"BLE 1M PHY","1 Mbps PHY",WirelessCapabilityCategory.Connectivity,"BLE specification");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BlePhy2M,"BLE 2M PHY","2 Mbps PHY",WirelessCapabilityCategory.Connectivity,"BLE 5.x specification");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BlePhyCoded,"BLE Coded PHY","LE Coded PHY",WirelessCapabilityCategory.Connectivity,"BLE 5.x specification");
        RegisterDocumentedSupported(BluetoothCapabilityIds.BleExtendedAdv,"BLE Extended Advertising","Extended advertising",WirelessCapabilityCategory.Connectivity,"BLE 5.x specification");
        RegisterDocumentedNotSupported(BluetoothCapabilityIds.BtClassic,"Bluetooth Classic","BR/EDR",WirelessCapabilityCategory.Connectivity,"ESP32-C2 BLE-only radio");
        RegisterDocumentedNotSupported(BluetoothCapabilityIds.BtEdr,"Bluetooth EDR","Enhanced Data Rate",WirelessCapabilityCategory.Connectivity,"ESP32-C2 BLE-only radio");
    }
    public void ExposeVerifiedCapabilities(){foreach(var c in _capabilities.Values)c.RecomputeExposure();}
}
