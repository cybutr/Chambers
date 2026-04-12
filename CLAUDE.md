# Chambers — Working Reference for Claude

This file is auto-loaded every session. It covers conventions, patterns, and recipes so code stays consistent and readable.

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

## Tile Character Reference

### `mapData` — terrain layer

| Char | Biome | Notes |
|------|-------|-------|
| `'P'` | Plains | Default land |
| `'F'` | Forest | Dense vegetation |
| `'M'` | Mountain peak | High cost traversal |
| `'m'` | Mountain interior | Surrounded by 8 mountain neighbors |
| `'O'` | Ocean deep | |
| `'o'` | Ocean shallow | Border of ocean |
| `'R'` | River | |
| `'r'` | River shallow | |
| `'L'` | Lake | |
| `'l'` | Lake shallow | |
| `'B'` | Beach | Land/water transition |
| `'b'` | Beach dark | |
| `'S'` | Snow | Mountain tops |
| `'s'` | Snow shallow | |
| `'@'` | Border | Map edge — impassable, never overwrite |
| `' '` | Empty | No render |

**Rule:** Uppercase = primary biome. Lowercase = depth/variant of the same biome.

### `overlayData` — entity layer (drawn on top)

| Char | Entity |
|------|--------|
| `'c'` | Crab |
| `'T'` | Turtle |
| `'C'` | Cow |
| `'S'` | Sheep |
| `'W'` | Wolf |
| `' '` / `'\0'` | Empty tile |

---

## Naming Conventions

| Thing | Convention | Example |
|-------|-----------|---------|
| Private methods | camelCase | `createRiver()`, `assignBiomes()` |
| Public methods | PascalCase | `Generate()`, `DisplayGUI()` |
| Bool flags | `is`/`Is` prefix | `isUpdating`, `IsHumidityRendering` |
| Lock objects | underscore prefix | `_consoleLock` |
| Map dimensions | always `width` × `height` | `new char[width, height]` |
| Not-found sentinel | `(-1, -1)` | for `(int, int)` returns |

---

## Utility Methods

All shared utilities live in `MapGeneration/Components/Map.Utils.cs`. **Always reuse these — don't rewrite them.**

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetDistance(x1,y1,x2,y2)` | `double` | Euclidean distance between two points |
| `GetNeighbors(x,y)` | `IEnumerable<(int,int)>` | All 8 adjacent tiles (yield return) |
| `GetCardinalNeighbors(x,y)` | `IEnumerable<(int,int)>` | 4 N/S/E/W tiles only |
| `CountSurroundingBiomes(x,y,char)` | `int` | Count matching neighbors, 0–8 |
| `GetRandomPointInBiome(char)` | `(int,int)` | Random tile of that type, `(-1,-1)` if none |
| `GetRandomPointInBiomeWithTilePool(List<char>)` | `(int,int)` | Weighted random from a tile pool |
| `GetRandomPointInBiomeInRange(char,cx,cy,min,max)` | `(int,int)` | Random tile within distance range |
| `GetClosestTileOfType(x,y,char)` | `(int,int)` | BFS nearest tile, `(-1,-1)` if none |
| `GetClosestDistanceOfType(x,y,char)` | `int` | Distance to nearest tile of type |
| `SpreadTile(x,y,chance,min,max)` | `void` | Probabilistic BFS expansion of tile |
| `FloodFillRegion(x,y,char,visited,list)` | `void` | Collects all connected tiles of type |
| `IsInBiome(x,y,char)` | `bool` | Bounds-safe tile type check |
| `ReplaceBiome(old,new)` | `void` | Replace all tiles of one type with another |

---

## "Not Found" Sentinel Pattern

`(int, int)` returns use `(-1, -1)` for "not found". **Always check before using:**

```csharp
(int x, int y) = GetRandomPointInBiome('M');
if (x == -1) return;  // no mountain tiles exist, bail out

// safe to use x,y here
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
// Get color for a tile
(int r, int g, int b) color = GetColor(mapData[x, y], x, y);

// Render it (each tile = 2 space chars with background color)
GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
```

- `GetColor(char tile, int x, int y)` — switch on tile char, returns `(r,g,b)` from `ColorSpectrum`
- All named colors are in `Other/ColorSpectrum.cs` as `(int r, int g, int b)` tuples
- Shadow/darkening: subtract a flat value from r/g/b, clamped with `Math.Clamp(..., 0, 255)`
- Cloud shadow: `GetShadowColor(x, y)` handles this automatically

---

## Partial Class Structure

`Map` is one class split across 6 files. All compile into the same class — methods from any file can call methods in any other.

| File | What goes here |
|------|---------------|
| `Map.cs` | Fields, constructor, `Generate()`, biome assignment |
| `Map.Frame.cs` | Coastline and border generation |
| `Map.Mountains.cs` | Mountain ranges, snow peaks, forest integration |
| `Map.WaterGen.cs` | Rivers, lakes, water depth |
| `Map.Waves.cs` | Wave animation, water tile rendering |
| `Map.Utils.cs` | **All shared utility methods — add new ones here** |

Use `#region name` / `#endregion` to group methods, consistent with the rest of the file.

---

## How to Add Things

### New tile type
1. Pick a char, add it to the tile table above
2. Add a case to `GetColor()` in `Map.cs` returning a `ColorSpectrum` color
3. Add a movement cost in `AStar.TerrainConfig` constructor (`Other/AStar.cs` ~line 73):
   ```csharp
   SetTerrainCost('X', 2.0f);
   ```

### New animal species
1. Add a class in `Species.cs` inheriting `Species`, implement `Behave()`
2. Define `AllowedTiles = new List<char> { 'B', 'b' }` (or whatever habitat)
3. Add a `List<YourAnimal> yourAnimals = new();` field in `Map.cs`
4. Call in `HandleGen()` in `Map.cs`:
   ```csharp
   if (conf.GenerateAnimals) InitializeSpecies(2, 5, new YourAnimal(0, 0, mapData, seed));
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

- **Don't** use `new Random()` without a seed — use the map's `rng` field
- **Don't** call `Console.*` directly — always `GUI.*`
- **Don't** add methods to `Map.cs` root — use the right Component file
- **Don't** use `List<T>.Contains()` in hot loops — use `HashSet<T>`
- **Don't** use `Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2))` — use `Math.Sqrt(dx*dx + dy*dy)`
- **Don't** use `waves.ToList()` or `list.ToList()` in per-frame loops — iterate directly, collect removals separately
- **Don't** access `mapData[x, y]` without a prior bounds check
