# Continuous Integration

## Purpose

GitHub Actions is the authoritative build/test environment for this repository when a local sandbox cannot execute the .NET SDK or restore NuGet packages.

## Pipeline

`build.yml` runs on:

- push to `main`
- pull requests targeting `main`
- manual `workflow_dispatch`

The matrix currently covers:

- Ubuntu latest
- Windows latest

Android is intentionally deferred until the shared core is ready for the Android client milestone.

Each job performs:

1. checkout
2. .NET 10 setup
3. NuGet restore
4. Release build
5. Release test
6. TRX publication
7. TRX artifact upload (90 days)

`dotnet format` is intentionally not a blocking CI gate yet. Formatting can become blocking after the repository has been normalized once.

## Verification levels

These states must never be conflated:

| Level | Meaning |
|---|---|
| LOCAL STATIC ANALYSIS | Source was inspected; no executable verification is implied. |
| LOCAL BUILD | Build actually executed in the local environment. |
| CI BUILD | GitHub Actions actually built the requested target successfully. |
| REAL HARDWARE TEST | Behavior was verified against a physical Proxmark5. |

## Rules for AI-assisted development

**Never claim BUILD VERIFIED, TEST VERIFIED or CI VERIFIED based solely on static inspection. A corresponding GitHub Actions run must exist and pass.**

**Never claim HARDWARE VERIFIED without evidence from a physical Proxmark5.**

If an environment cannot install or execute the required SDK, report the limitation instead of converting static confidence into a test result.

## Current scope

The current CI verifies the protocol/core test project. It does not prove:

- compatibility with a physical Proxmark5;
- compatibility with every firmware revision;
- USB/TCP/BLE transport behavior;
- BWM behavior on the actual ESP32/BWM firmware;
- firmware flashing or update safety;
- Windows GUI behavior;
- Android behavior.

Those require later integration or hardware tests.
