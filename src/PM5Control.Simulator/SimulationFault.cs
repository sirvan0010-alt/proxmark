// PM5 Control Center — simulator fault model
// PURPOSE: deterministic fault injection for offline protocol/transport tests.
// STATUS: SIMULATED_ONLY. These modes are test fixtures, not claims about PM5.

namespace PM5Control.Simulator;

public enum SimulationFault
{
    None,
    Timeout,
    MalformedResponse,
    WrongCommandId,
    BroadcastInsteadOfResponse,
    DisconnectBeforeSend,
    UnsupportedCommand
}
