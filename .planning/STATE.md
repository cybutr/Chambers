# STATE — Chambers Generator Pipeline Rewrite

## Project Reference

**Core value**: New `GenerationPipeline` produces a complete, valid `mapData[,]` using typed generator passes — and the existing legacy gen still works as `GenType = 1`
**Current focus**: Phase 1 — Generator Pipeline Base

---

## Current Position

**Phase**: 6 — Camera & World Size
**Plan**: 06-01 complete (awaiting human-verify checkpoint)
**Status**: Awaiting verification
**Progress**: `[x][x][x][x][x][~]` 5/6 phases (6 in progress)

---

## Performance Metrics

| Metric | Value |
|--------|-------|
| Phases total | 6 |
| Phases complete | 3 |
| Requirements mapped | 17 (GEN-01 to GEN-09 incl. GEN-06 A-H) |
| Requirements complete | GEN-06-C verified |

---

## Accumulated Context

### Key Decisions

- Legacy gen stays as `GenType = 1` forever — regression baseline, saves must not break
- `GenerationContext` owns all mutable data — generators never touch `Map` fields directly
- Pass stubs in Phase 1 have no logic — algorithms come in Phase 2+
- `conf.GenType` already exists as `int` in `Config` — branch on it in `Map.Generate()`
- New files go in `MapGeneration/Generators/` — mirrors `Species/` and `Program/` pattern
- Compound lambda pattern for arrow keys — `keybinds.Bind` is last-write-wins, adding a second Bind would erase chamber-switch; both behaviours must live in one lambda

### Conventions (from CLAUDE.md)

- camelCase properties, PascalCase methods
- `#region` / `#endregion` groups in every file
- Expression-bodied single-statement members
- Brace-free single-line `if`/`else`
- No XML doc comments, no inline `//` unless explaining a non-obvious constant
- `new()` target-typed expressions for collections
- Tuple returns `(int x, int y)` preferred over out params

### Blockers

None.

### Todos

- [ ] Run `dotnet build` after Phase 1 to confirm zero errors
- [ ] Verify `conf.GenType = 1` regression after Map.Generate() branch is added

---

## Session Continuity

**Last updated**: 2026-05-09
**Last action**: Phase 6 plan 01 complete — caminfo command + compound arrow-key pan lambdas, dotnet build 0 errors; awaiting human-verify checkpoint
**Resume with**: Approve checkpoint (SC1–SC4), then phase 6 is done
