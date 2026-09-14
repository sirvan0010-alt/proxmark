# AI Agent Runner Roadmap

## Objective

Provide a provider-neutral runner for the engineering swarm without allowing an AI model to bypass evidence, safety or repository boundaries.

## Architecture

```text
ORCHESTRATOR
    -> RUNNER
    -> SPECIALIST AGENT
    -> ALLOW-LISTED TOOLS
    -> RESULT JSON
    -> INDEPENDENT VERIFIER
    -> REVIEW / TEST / CI
    -> EVIDENCE PROMOTION
```

## Runner requirements

- immutable repository SHA per task;
- explicit path scope;
- bounded time, iterations and tool calls;
- cancellation at every asynchronous boundary;
- tool allow-list rather than unrestricted shell access;
- secret redaction and environment-based credentials;
- isolated implementation branches;
- machine-readable result artifacts;
- verifier separate from the implementing agent;
- no automatic merge or release;
- explicit human boundary for risky physical-device operations.

## Initial implementation order

1. Result schema and verifier contract.
2. Task loader and immutable task context.
3. Provider-neutral model adapter interface.
4. Read-only repository/GitHub tool adapter.
5. Branch/workspace adapter for implementation tasks.
6. Test runner and artifact collector.
7. Independent evidence verifier.
8. Optional CI-triggered runner with explicit opt-in.

## Evidence rule

The runner can automate evidence collection and implementation. It cannot manufacture evidence. In particular, source, simulator, unit tests and CI must never be represented as physical PM5 hardware verification.
