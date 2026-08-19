/*
 * PM5 Control Center
 *
 * PURPOSE: Defines device operations above the raw transport layer.
 * WHY: The client must not bind the UI directly to legacy PM3 commands or
 *      PM5/BWM-specific wire details.
 * RULE: Implementations must return structured diagnostic data and preserve
 *      evidence/source state. Do not silently fall back to guesses.
 * SEE: README.md, AI_CONTEXT.md, docs/ARCHITECTURE.md
 */

using PM5Control.Core.Devices;

namespace PM5Control.Core.Protocols;

public interface IProxmarkProtocol
{
    string ProtocolName { get; }

    Task<ProxmarkDeviceInfo> ReadDeviceInfoAsync(CancellationToken cancellationToken = default);
}
