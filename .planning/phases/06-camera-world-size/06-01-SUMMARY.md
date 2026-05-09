---
phase: 06-camera-world-size
plan: "01"
subsystem: program-commands
tags: [camera, keybinds, commands, pan]
dependency_graph:
  requires: []
  provides: [caminfo-command, arrow-key-pan]
  affects: [Program/Program.Commands.cs, Program.cs]
tech_stack:
  added: []
  patterns: [compound-lambda-keybind]
key_files:
  created: []
  modified:
    - Program/Program.Commands.cs
    - Program.cs
decisions:
  - "Compound lambda pattern used for arrow keys — keybinds.Bind is last-write-wins, not additive"
metrics:
  duration: "~8 minutes"
  completed: 2026-05-09
---

# Phase 6 Plan 01: caminfo Command + Arrow-Key Pan Summary

`caminfo` command and compound arrow-key lambdas wiring camera pan into the production keybind set.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add caminfo command | 17ece13 | Program/Program.Commands.cs |
| 2 | Compound arrow-key pan+switch lambdas | 8c005d7 | Program.cs |

## What Was Built

**Task 1:** `commandRegistry.Register("caminfo", ...)` added inside `#region map commands` immediately after the `goto` command. Outputs `World: W×H | Viewport: V×V | Camera: [X,Y]`. Null-guards camera before access.

**Task 2:** The four `ChamberPrev/Next/First/Last` `RegisterCommand` lambdas now each call `PanCamera` first (with the same guard as WASD: `!isCommandInputMode && (!IsTemperatureRendering || !IsHumidityRendering)`), then the original `chamberSwitch` logic. The four `keybinds.Bind` calls are unchanged. No second Bind calls were added.

## Verification Results

- `dotnet build` — 0 errors, 10 pre-existing warnings (all from GenerationContext.cs, unrelated)
- `grep -c "ChamberPrev|...|ChamberLast" Program.cs` → 8 (4 RegisterCommand + 4 Bind)
- `grep -c "PanCamera" Program.cs` → 9 (1 def + 4 WASD + 4 arrow lambdas)
- `grep -rn "camera" Species/ | grep -v "//"` → 0 matches (SC4 satisfied)
- `caminfo` confirmed present at `Program/Program.Commands.cs:258`

## Deviations from Plan

None — plan executed exactly as written.

## Checkpoint Awaiting Human Verification

Task 3 (`checkpoint:human-verify`) requires manual runtime testing:

- SC1: `dotnet run -- --camera-test` starts without crash and renders 300×150 world
- SC2: Arrow keys and WASD scroll the viewport during `--camera-test`; Q quits
- SC3: No full-screen flicker when holding arrow/WASD for 2–3 seconds
- SC4: `caminfo` command in normal run prints `World: W×H | Viewport: V×V | Camera: [X,Y]`
- Additional: 2+ chambers loaded, LeftArrow/RightArrow still chamber-switches (pan fires too)

## Known Stubs

None.

## Threat Flags

None — `caminfo` exposes only local process debug info (no PII). Arrow key pan uses `Interlocked.Add` with `Math.Clamp` bounds per T-06-02 accepted.

## Self-Check: PASSED

- `Program/Program.Commands.cs` contains `caminfo` at line 258 — FOUND
- Commit 17ece13 exists — FOUND
- Commit 8c005d7 exists — FOUND
- Build exits 0 — CONFIRMED
