---
plan: 05-03
phase: 05-world-polish
status: complete
---

## Summary

Wired overlayData (EntityId[,]) through the pipeline and implemented EntityPass to spatially seed species onto valid terrain.

## What was built

- `GenerationContext.overlayData EntityId[,]` — new field
- `Map.cs` ctx initializer — `overlayData = new EntityId[width, height]`
- `Map.cs` copy-back — `overlayData = ctx.overlayData` after pipeline.Run
- `EntityPass.Execute` — iterates `EntityRegistry.Spawnable()`, calls `SeedEntity` per def
- `SeedEntity` — builds candidate list (bounds-checked, Border skipped, existing entity skipped, AllowedTiles.Contains check), Fisher-Yates shuffle, places `rng.Next(Min, Max+1)` entities

## Self-Check: PASSED

- overlayData in GenerationContext with `EntityId[,]` ✓
- 2 overlayData lines in Map.cs (init + copy-back) ✓
- 2 SeedEntity references (declaration + call site) ✓
- AllowedTiles.Count == 0 guard (Bird) ✓
- TileId.Border skip guard ✓
- EntityRegistry.Spawnable() call ✓
- No Map class reference in EntityPass ✓
- dotnet build exits 0 ✓

## key-files

### modified
- MapGeneration/Generators/GenerationContext.cs
- MapGeneration/Map.cs

### created
- MapGeneration/Generators/EntityPass.cs
