/*
 * PM5 Control Center
 *
 * PURPOSE: Structured device snapshot used by Inspector and reports.
 * WHY: The UI must consume structured facts rather than scrape CLI text.
 * RULE: Unknown data remains unknown; do not invent defaults.
 * SEE: AI_CONTEXT.md, docs/ARCHITECTURE.md
 */

using PM5Control.Core.Diagnostics;

namespace PM5Control.Core.Devices;

public sealed record ProxmarkDeviceInfo(
    DiagnosticValue<string> Model,
    DiagnosticValue<string> HardwareRevision,
    DiagnosticValue<string> UsbVidPid,
    DiagnosticValue<string> ArmFirmware,
    DiagnosticValue<string> FpgaFirmware,
    DiagnosticValue<string> BwmFirmware,
    DiagnosticValue<long> MemoryBytes,
    DiagnosticValue<string> Esp32Model,
    DiagnosticValue<string> WifiStatus,
    DiagnosticValue<string> BluetoothStatus,
    DiagnosticValue<string> PowerStatus,
    DiagnosticValue<double> BatteryVoltage);
