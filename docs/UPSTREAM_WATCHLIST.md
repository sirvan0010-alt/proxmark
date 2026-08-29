# Proxmark upstream watchlist

Reference repositories for future Proxmark checks and updates:

- RfidResearchGroup/proxmark3 — upstream Proxmark3/Iceman project
- NielDK/proxmark3 — NielDK development fork, especially PM5/BWM/BLE work
- cindersocket/proxmark3 — relevant T55xx improvement work when applicable
- sirvan0010-alt/proxmark — this repository; upstream changes are evaluated before integration

## Policy

When a Proxmark check/update is requested, inspect the current state of the reference repositories and compare relevant changes with this repository. Only changes relevant to PM5 Control Center should be integrated. Firmware-only changes that do not belong in the Windows client are not copied blindly.

Prefer small, reviewable commits. Preserve the existing read-only safety model and do not introduce firmware flashing, reset, destructive commands, or automatic hardware writes as part of discovery/diagnostics.
