# PM5 AI Swarm Runtime

This directory contains the first real model-backed runtime for the PM5 engineering swarm.

## Boundary

`runner.py` is an **analysis agent**, not a hardware controller. It receives a role,
task contract and repository context, calls the configured model through the
OpenAI Responses API, and writes an auditable JSON result. It does not mutate the
repository and has no PM5 hardware transport.

## Required GitHub secret

The Actions workflow must provide `OPENAI_API_KEY` as a repository/environment
secret. Never put the key in source, task artifacts or prompts.

Optional model configuration:

- `AI_MODEL` — defaults to `gpt-5.6-luna`
- `AI_API_URL` — defaults to `https://api.openai.com/v1/responses`

## Evidence boundary

The runner may analyze source, documentation, simulator output and CI artifacts,
but it must not promote those to physical hardware verification. Hardware claims
remain gated by the PM5 hardware/evidence agents and the explicit evidence ladder.

## Next runtime stages

1. model-backed specialist execution (this stage)
2. artifact exchange and cross-agent challenge inputs
3. model-backed challenge/rebuttal execution
4. model-backed synthesis/decision gate
5. controlled implementation runner after all gates pass

The implementation runner must remain a separate capability so analysis agents
cannot accidentally gain repository mutation or hardware-control authority.
