# AI Swarm Architecture — Proxmark5 Control Center

**Status:** Contract + model-backed runtime exists; Research Ledger + autonomy levels formalized here.  
**Goal:** Autonomous multi-agent engineering under strict evidence and hardware safety boundaries.

## 1. What already works better than interactive CLI agents

This repository does **not** depend on interactive CLIs (e.g. mini-swe-agent confirm/yolo prompts) for CI.

`tools/ai-swarm/runner.py` is deliberately:

- non-interactive (no TTY, no prompt_toolkit, no stdin prompts);
- stateless (role + task + context in → JSON artifact out);
- read-only on the repository (analysis path does not mutate code);
- free of PM5 hardware transport.

That is the correct pattern for GitHub Actions and Docker smoke tests. Any future external runner (including third-party agent CLIs) must preserve the same contract:

```text
NO TTY required
NO confirm / human / interactive mode in CI
Task always supplied explicitly (never prompted)
Exit on finish without confirmation prompts
stdin may be /dev/null
```

If a third-party tool defaults to interactive mode, CI must force non-interactive flags **and** verify them in a real smoke test before claiming VERIFIED.

## 2. Autonomy levels

| Level | Name | Allowed |
|-------|------|---------|
| L0 | Deterministic gates | Contract validation, ledger schema, handoff artifacts |
| L1 | Research-only | Fill Research Ledger, open research notes; no code mutation |
| L2 | Analysis swarm | Model-backed specialists + challenge + synthesis artifacts |
| L3 | Implementation draft | Unified diff on isolated branch + tests + **draft** PR only |
| L4 | Continuous maintenance | Scheduled L1–L3 with hard stop conditions |

**Never automated without human authority:** merge to protected branches, release, flash/erase/downgrade, charger/FPGA power writes, irreversible device operations.

## 3. Research Ledger lifecycle

```text
PENDING → AUDITED → MAPPED → DECISION → IMPLEMENTED → TESTED → VERIFIED → CLOSED
                                              ↘ REJECTED | BLOCKED
```

- `RESEARCH_AGENT` owns ledger completeness (`docs/RESEARCH-LEDGER.json`).
- `ORCHESTRATOR_AGENT` must not close research work while required mechanisms remain open.
- PM3 upstream existence ≠ PM5 support.
- Simulator / CI / unit tests ≠ hardware verification.

## 4. Collaboration graph (current)

```text
ORCHESTRATOR_AGENT
    ├─ RESEARCH_AGENT (+ Research Ledger)
    ├─ ARCHITECT_AGENT
    ├─ DOMAIN: PM5_HARDWARE / PM5_FIRMWARE / PM5_PROTOCOL / BWM_ESP /
    │          RFID_NFC / TRANSPORT / SIMULATOR / DIAGNOSTICS / COMPATIBILITY
    ├─ EVIDENCE_AGENT + SECURITY_AGENT
    ├─ FEATURE_ARCHITECT_AGENT + EFFICIENCY_AGENT + UX_AGENT + DOCUMENTATION_AGENT
    ├─ TEST_AGENT + REVIEW_AGENT
    └─ RELEASE_AGENT

Runtime: ai-agent-swarm.yml (contracts)
      → ai-agent-runtime.yml (model-backed findings/challenges)
      → implementation_gate.py (fail-closed IMPLEMENT decision)
      → draft PR only
```

## 5. Non-interactive CI contract (mandatory)

Any agent execution in Actions/Docker must satisfy:

1. Explicit task input (`-t` / env / file) — never prompt for task.
2. No confirmation mode for actions or exit.
3. No reliance on `isatty()` for control flow success.
4. Bounded steps/cost/time with clean stop (no interactive limit raise).
5. Machine-readable result schema (`docs/AI-AGENT-RESULT-SCHEMA.json`).
6. Independent verification gate (`docs/AI-AGENT-VERIFICATION-GATE.md`).

Failure mode preferred over silent guessing: `BLOCKED` / `NEEDS-EVIDENCE`.

## 6. Source of truth order

1. `main` source + tests  
2. Green CI evidence  
3. Physical hardware observations (when present and labeled)  
4. `docs/RESEARCH-LEDGER.json`  
5. `AI_AGENT_REGISTRY.md` + verification gate docs  
6. Chat / session notes

## 7. Next steps toward fuller autonomy

1. Keep Research Ledger populated only with pinned, source-backed entries.  
2. Strengthen contract gate CI (roles + ledger schema).  
3. Optional: scheduler that selects next open ledger item (L1 automation).  
4. Keep implementation path fail-closed via `implementation_gate.py`.  
5. Never mark hardware capabilities VERIFIED from CI alone.
