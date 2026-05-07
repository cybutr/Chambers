---
plan: 05-02
phase: 05-world-polish
status: complete
---

## Summary

Implemented FramePass with a single Execute method that stamps TileId.Border on all four perimeter edges.

## What was built

- Two loops: one over x (top/bottom rows), one over y (left/right columns)
- Stamps `mapData[x,0]`, `mapData[x,h-1]`, `mapData[0,y]`, `mapData[w-1,y]`
- Overwrites whatever prior passes placed on the perimeter

## Self-Check: PASSED

- 4 TileId.Border assignment lines ✓
- No Map class reference ✓
- `#region border frame` present ✓
- dotnet build exits 0 ✓

## key-files

### created
- MapGeneration/Generators/FramePass.cs
