# PM5 AI Swarm Runtime

This directory contains the model-backed runtime and controlled implementation boundary for the PM5 engineering swarm.

## Runtime stages

1. model-backed specialist execution (`runner.py`)
2. artifact exchange and cross-agent challenge execution
3. model-backed synthesis/decision gate
4. controlled implementation (`implementation_runner.py`)
5. scoped tests and draft PR
6. human/release merge gate

## Non-interactive CI contract

`runner.py` is intentionally **non-interactive**:

- no TTY / prompt_toolkit / confirm modes;
- role, task file and context file are always CLI arguments;
- result is always a JSON artifact;
- suitable for GitHub Actions and Docker with stdin from `/dev/null`.

Do **not** replace this path with an interactive agent CLI (confirm/yolo prompts) unless CI forces full non-interactive flags **and** a real smoke test proves no `Input is not a terminal` / abort path remains.

See `docs/AI-SWARM-ARCHITECTURE.md` for autonomy levels and the Research Ledger lifecycle.

## Boundaries

`runner.py` is an analysis agent. It receives a role, task contract and repository context, calls the configured model through the OpenAI Responses API, and writes an auditable JSON result. It does not mutate the repository and has no PM5 hardware transport.

`implementation_runner.py` is deliberately narrower: it asks the model for a unified diff only. The surrounding workflow applies the diff only after `implementation_gate.py` passes, verifies the changed paths against the synthesis implementation scope, runs smoke tests, and creates a draft PR from an isolated branch. It does not merge, flash firmware, operate PM5 hardware, or perform destructive device operations.

`implementation_gate.py` is fail-closed. It requires an explicit `IMPLEMENT` synthesis decision, architecture/evidence/security/test/review approvals, an allowed evidence state, no pending hardware verification, no destructive operation, and a non-empty implementation scope.

## Required GitHub secret

The Actions workflow must provide `OPENAI_API_KEY` as a repository/environment secret. Never put the key in source, task artifacts or prompts.

Optional model configuration:

- `AI_MODEL` — defaults to `gpt-5.6-luna`
- `AI_API_URL` — defaults to `https://api.openai.com/v1/responses`

## Evidence boundary

Source, documentation, simulator and CI evidence remain distinct from physical PM5 evidence. A successful AI run, build, test, or draft PR never establishes physical hardware verification.

## Research Ledger

Machine-readable state: `docs/RESEARCH-LEDGER.json`.  
`RESEARCH_AGENT` owns completeness. Orchestrator must not close research with open mechanisms.

## Merge boundary

The AI runner can create a draft PR after its gates pass. It cannot merge the PR. Hardware operations remain outside this automation boundary.
