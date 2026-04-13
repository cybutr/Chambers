# Chambers — Working Reference for Claude

This file is auto-loaded every session. It covers conventions, patterns, and recipes so code stays consistent and readable.

**Keep this file up to date.** After any significant refactor (new type system, renamed APIs, structural changes), update the relevant sections here before finishing the task.

---

## Coordinate System

`mapData[x, y]` where **x = column** (left→right, 0 to width-1), **y = row** (top→bottom, 0 to height-1).

Screen position from map coords:
```csharp
int screenX = leftPadding + x * 2;  // each tile = 2 chars wide
int screenY = y + topPadding;
GUI.SetCursorPosition(screenX, screenY);
```

Standard bounds check (use this everywhere before array access):
```csharp
if (nx >= 0 && nx < width && ny >= 0 && ny < height)
```

---

## Typed Data Arrays

All three map layers are now typed enums — never use raw `char` for these.

### `mapData` — terrain layer (`TileId[,]`)

| TileId | LegacyChar | Biome | Notes |
|--------|-----------|-------|-------|
| `TileId.Plains` | `P` | Plains | Default land |
| `TileId.Forest` | `F` | Forest | Dense vegetation |
| `TileId.Mountain` | `M` | Mountain peak | High cost traversal |
| `TileId.MountainDeep` | `m` | Mountain interior | Surrounded by 8 mountain neighbors |
| `TileId.Ocean` | `O` | Ocean deep | |
| `TileId.OceanShallow` | `o` | Ocean shallow | Border of ocean |
| `TileId.River` | `R` | River | |
| `TileId.RiverShallow` | `r` | River shallow | |
| `TileId.Lake` | `L` | Lake | |
| `TileId.LakeShallow` | `l` | Lake shallow | |
| `TileId.Beach` | `B` | Beach | Land/water transition |
| `TileId.BeachDark` | `b` | Beach dark | |
| `TileId.Snow` | `S` | Snow | Mountain tops |
| `TileId.Stream` | `s` | Stream | Shallow stream |
| `TileId.Border` | `@` | Border | Map edge — impassable, never overwrite |
| `TileId.Empty` | ` ` | Empty | No render |

Tile properties come from `TileRegistry.Get(TileId)` → `TileDefinition`:
- `.IsWater`, `.IsLand`, `.MovementCost`, `.BaseColor`, `.DeepVariant`, `.CreatureCategories`

### `overlayData` — entity layer (`EntityId[,]`)

| EntityId | LegacyChar | Entity |
|----------|-----------|--------|
| `EntityId.None` | ` ` | Empty tile |
| `EntityId.Crab` | `c` | Crab |
| `EntityId.Turtle` | `T` | Turtle |
| `EntityId.Cow` | `C` | Cow |
| `EntityId.Sheep` | `S` | Sheep |
| `EntityId.Wolf` | `W` | Wolf |
| `EntityId.Bear` | `B` | Bear |
| `EntityId.Goat` | `G` | Goat |
| `EntityId.Fish` | `F` | Fish |
| `EntityId.Bird` | `A` | Bird |
| `EntityId.Villager` | `V` | Villager |

### `cloudData` — cloud layer (`CloudType[,]`)

| CloudType | LegacyChar | Notes |
|-----------|-----------|-------|
| `CloudType.None` | `\0` | No cloud |
| `CloudType.Cirrus` | `1` | |
| `CloudType.Altocumulus` | `2` | |
| `CloudType.Cumulus` | `3` | |
| `CloudType.Cumulonimbus` | `4` | Storm clouds |
| `CloudType.Nimbostratus` | `5` | |
| `CloudType.Stratus` | `6` | |

### JSON serialization

All three arrays serialize to/from legacy char strings for human-readable saves:
- `TileIdArrayJsonConverter`, `EntityIdArrayJsonConverter`, `CloudTypeArrayJsonConverter` in `Program.cs`
- `previous*` arrays are `[JsonIgnore]` — not saved, cloned from live data on load

---

## Naming Conventions

| Thing | Convention | Example |
|-------|-----------|---------|
| Private methods | camelCase | `createRiver()`, `assignBiomes()` |
| Public methods | PascalCase | `Generate()`, `DisplayGUI()` |
| Bool flags | `is`/`Is` prefix | `isUpdating`, `IsHumidityRendering` |
| Map dimensions | always `width` × `height` | `new TileId[width, height]` |
| Not-found sentinel | `(-1, -1)` | for `(int, int)` returns |

---

## Utility Methods

All shared utilities live in `MapGeneration/Components/Map.Utils.cs`. **Always reuse these — don't rewrite them.**

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetDistance(x1,y1,x2,y2)` | `double` | Euclidean distance between two points |
| `GetNeighbors(x,y)` | `IEnumerable<(int,int)>` | All 8 adjacent tiles (yield return) |
| `GetCardinalNeighbors(x,y)` | `IEnumerable<(int,int)>` | 4 N/S/E/W tiles only |
| `CountSurroundingBiomes(x,y,TileId)` | `int` | Count matching neighbors, 0–8 |
| `GetRandomPointInBiome(TileId)` | `(int,int)` | Random tile of that type, `(-1,-1)` if none |
| `GetRandomPointInBiomeWithTilePool(List<TileId>)` | `(int,int)` | Weighted random from a tile pool |
| `GetRandomPointInBiomeInRange(TileId,cx,cy,min,max)` | `(int,int)` | Random tile within distance range |
| `GetClosestTileOfType(x,y,TileId)` | `(int,int)` | BFS nearest tile, `(-1,-1)` if none |
| `GetClosestDistanceOfType(x,y,TileId)` | `int` | Distance to nearest tile of type |
| `SpreadTile(x,y,chance,min,max)` | `void` | Probabilistic BFS expansion of tile |
| `FloodFillRegion(x,y,TileId,visited,list)` | `void` | Collects all connected tiles of type |
| `IsInBiome(x,y,TileId)` | `bool` | Bounds-safe tile type check |
| `ReplaceBiome(TileId,TileId)` | `void` | Replace all tiles of one type with another |

---

## "Not Found" Sentinel Pattern

`(int, int)` returns use `(-1, -1)` for "not found". **Always check before using:**

```csharp
(int x, int y) = GetRandomPointInBiome(TileId.Mountain);
if (x == -1) return;  // no mountain tiles exist, bail out
```

Never index `mapData[-1, -1]` — it will throw.

---

## Threading Rules

Four threads run concurrently. Respect these boundaries:

| Thread | What it does | What it owns |
|--------|-------------|-------------|
| `updateThread` | advances simulation | writes to `mapData`, `overlayData` |
| `guiThread` | renders screen | reads map state, writes console |
| `keyListenerThread` | captures input | sets flags only |
| `weatherThread` | updates climate | writes to `weather`, `clouds` |

**Rules:**
- **Never** call `Console.*` directly — always use `GUI.*` (it handles locking)
- **Never** write to `mapData`/`overlayData` from `guiThread`
- Log events from any thread via `outputBuffer.Add("message")` — it's safe
- When reading map state on the GUI thread, wrap in `lock (mapLock)`

---

## Color & Rendering Pattern

```csharp
// Get color for a tile (char-based legacy path, still in use)
(int r, int g, int b) color = GetColor(mapData[x, y], x, y);

// Render it (each tile = 2 space chars with background color)
GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
```

- `GetColor(TileId tile, x, y)` — reads `TileRegistry.Get(tile).BaseColor`, then applies temperature/humidity tinting. No legacy char conversion.
- All named colors are in `Other/ColorSpectrum.cs` as `(int r, int g, int b)` tuples
- Shadow/darkening: subtract a flat value from r/g/b, clamped with `Math.Clamp(..., 0, 255)`
- Cloud shadow: `GetShadowColor(x, y)` handles this automatically

---

## Partial Class Structure

`Map` is one class split across many files. All compile into the same class — methods from any file can call methods in any other.

| File | What goes here |
|------|---------------|
| `Map.cs` | Fields, constructor, `Generate()`, biome assignment, `Update()` |
| `Map.Frame.cs` | Coastline and border generation |
| `Map.Mountains.cs` | Mountain ranges, snow peaks, forest integration |
| `Map.WaterGen.cs` | Rivers, lakes, water depth |
| `Map.Waves.cs` | Wave animation, water tile rendering, draw-current helpers |
| `Map.Utils.cs` | **All shared utility methods — add new ones here** |
| `Map.Clouds.cs` | All cloud spawning, updating, rendering, state |
| `Map.Weather.cs` | Weather state, wind, temperature, humidity, day/night cycle |
| `Map.Display.cs` | `DisplayMap()`, tile/overlay rendering, temperature/humidity noise |
| `Map.GUI.cs` | `DisplayGUI()`, all GUI widgets, map config GUI |
| `Map.Species.cs` | Species lists, `InitializeSpecies()`, `UpdateCrabs/Turtles/Cows/Sheeps()` |

`Program` is also split into partial class files:

| File | What goes here |
|------|---------------|
| `Program.cs` | `Main()`, fields, threading methods, key listener, commands |
| `Program/Program.Serialization.cs` | JSON converters, `SaveMap()`, `LoadMap()`, sync helpers |
| `Program/Program.GUI.cs` | Save selection GUI, slot management |
| `Program/Program.Tests.cs` | Test methods, `Testing()` runner |

Use `#region name` / `#endregion` to group methods, consistent with the rest of the file.

---

## How to Add Things

### New tile type
1. Add a value to `TileId` enum in `Core/TileId.cs`
2. Add a `Register(new TileDefinition { ... })` entry in `Core/TileRegistry.cs` with `LegacyChar`, `BaseColor`, `MovementCost`, `IsLand`/`IsWater`, `CreatureCategories`
3. Update `TileIdArrayJsonConverter._toChar` array size if needed
4. That's it — color, movement cost, and water/land flags are all in the registry

### New animal species
1. Add a class in `Species/Species.cs` inheriting `Species`
2. Call `base(name, habitat, x, y, seedOffset)` — infrastructure is inherited
3. Set `allowedTiles` in the constructor
4. Implement `Behave(TileId[,] mapData, EntityId[,] overlayData)`
5. Add `EntityId.YourAnimal` to the `EntityId` enum and update `EntityIdArrayJsonConverter`
6. Add a `List<YourAnimal> yourAnimals = new();` field in `Map.cs`
7. Call in `HandleGen()`:
   ```csharp
   if (conf.GenerateAnimals) InitializeSpecies(2, 5, new YourAnimal(0, 0, mapData, overlayData, seed));
   ```

### New command
1. Add a `case "yourcommand":` block to `ProcessCommand()` in `Program.cs` (~line 1149)
2. Use `outputBuffer.Add("feedback text")` to report results
3. `break;` at the end of the case

### New biome / Config toggle
1. Add to `Habitat` enum in `Other/Config.cs`
2. Add a `public bool EnableFeature { get; set; } = false;` to `Config`
3. Gate generation code with `if (conf.EnableFeature)`

---

## What NOT to Do

- **Don't** use raw `char` for `mapData`, `overlayData`, or `cloudData` — use `TileId`, `EntityId`, `CloudType`
- **Don't** use `new Random()` without a seed — use the map's `rng` field or the inherited `rng` in species
- **Don't** call `Console.*` directly — always `GUI.*`
- **Don't** add methods to `Map.cs` root — use the right Component file
- **Don't** use `List<T>.Contains()` in hot loops — use `HashSet<T>`
- **Don't** use `Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2))` — use `Math.Sqrt(dx*dx + dy*dy)`
- **Don't** use `waves.ToList()` or `list.ToList()` in per-frame loops — iterate directly, collect removals separately
- **Don't** access `mapData[x, y]` without a prior bounds check
