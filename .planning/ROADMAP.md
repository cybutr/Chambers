# ROADMAP — Chambers Generator Pipeline Rewrite

## Phases

- [ ] **Phase 1: Generator Pipeline Base** — Full infrastructure scaffold: folders, interfaces, context, pipeline runner, NoiseStack, pass stubs, Map.Generate() branch, camera decoupling
- [ ] **Phase 2: Elevation & Climate** — Implement ElevationPass (multi-octave noise) and ClimatePass (latitude + coast distance)
- [ ] **Phase 3: Biome Assignment** — Implement BiomePass with Whittaker-style (elevation, temperature, humidity) → TileId lookup
- [ ] **Phase 4: Terrain Features** — Implement MountainPass (elevation peak detection) and HydroPass (gradient-following rivers, lakes, water depth)
- [ ] **Phase 5: World Polish** — Implement FeaturePass (beaches, snow, forests, shallow water) and FramePass (coastline framing, border tiles) and EntityPass (species seeding on completed terrain)
- [ ] **Phase 6: Camera & World Size** — Fully decouple world size from viewport; enable, test, and stabilize large worlds with scrolling camera

---

## Phase Details

### Phase 1: Generator Pipeline Base
**Goal**: The complete infrastructure scaffold exists — every file, interface, and wiring point is in place so Phase 2 can drop real algorithms into stubs without touching structure
**Depends on**: Nothing (first phase)
**Requirements**: GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06-A, GEN-06-B, GEN-06-C, GEN-06-D, GEN-06-E, GEN-06-F, GEN-06-G, GEN-06-H, GEN-07, GEN-08, GEN-09
**Success Criteria** (what must be TRUE):
  1. `dotnet build` succeeds with zero errors or warnings on the new files
  2. Running with `conf.GenType = 1` produces identical output to current legacy generation — no regression
  3. Running with `conf.GenType = 2` executes the pipeline, each pass logs its name and elapsed ms, and the world has valid dimensions with all tiles defaulted
  4. `MapGeneration/Generators/` exists with each pass in its own file, matching the `Species/` and `Program/` folder pattern used in this codebase
  5. `Camera` viewport is not capped to `conf.Width`/`conf.Height` — world can be specified larger than the console window
**Plans**: 5 plans

Plans:
- [ ] 01-01-PLAN.md — GenerationContext and IGenerationPass (infrastructure contracts)
- [ ] 01-02-PLAN.md — GenerationPipeline and NoiseStack (pipeline runner + real multi-octave noise)
- [ ] 01-03-PLAN.md — All 8 pass stubs (ElevationPass through EntityPass)
- [ ] 01-04-PLAN.md — Map.Generate() branch on conf.GenType + copy-back (GEN-07, GEN-08)
- [ ] 01-05-PLAN.md — World size decoupled from console dimensions (GEN-09)

### Phase 2: Elevation & Climate
**Goal**: The pipeline produces a meaningful elevation map and climate arrays — the world has highlands, lowlands, warm equatorial bands, and coast-influenced humidity before any biome is assigned
**Depends on**: Phase 1
**Requirements**: ElevationPass implementation (v2 deferred), ClimatePass implementation (v2 deferred)
**Success Criteria** (what must be TRUE):
  1. `ctx.elevation[,]` is fully populated with values in [0, 1] using at least 3 octaves of Perlin noise seeded from `conf.seed`
  2. `ctx.temperatureData[,]` decreases from equator to poles and from lowlands to highlands — observable in temperature render mode
  3. `ctx.humidityData[,]` is higher near coastlines and lower inland — observable in humidity render mode
  4. Two maps generated with the same seed produce identical elevation and climate arrays
**Plans**: 3 plans

Plans:
- [ ] 02-01-PLAN.md — ElevationPass implementation (Wave 1)
- [ ] 02-02-PLAN.md — ClimatePass implementation (Wave 2, depends on 02-01)
- [ ] 02-03-PLAN.md — Map.cs post-pipeline avarageTempature/avarageHumidity (Wave 3, depends on 02-02)

### Phase 3: Biome Assignment
**Goal**: The pipeline assigns geographically coherent biomes — a Whittaker-style lookup produces forests where it is warm and wet, tundra where cold, desert where hot and dry
**Depends on**: Phase 2
**Requirements**: GEN-06-C
**Success Criteria** (what must be TRUE):
  1. Every non-border land tile has a `TileId` assigned by BiomePass based on its `(elevation, temperature, humidity)` triple — no tiles left at default/empty
  2. Deserts appear at low humidity + high temperature; forests at moderate-to-high humidity + moderate temperature; snow at elevation peaks — visible in normal render mode
  3. Biome boundaries are not jagged pixel noise — smooth transitions visible when zooming around the map
**Plans**: 1 plan

Plans:
- [ ] 03-01-PLAN.md — BiomePass.Execute — water gate, snow gate, Whittaker switch, boundary smoothing

### Phase 4: Terrain Features
**Goal**: The world gains mountain ranges on elevation peaks and river networks that follow the elevation gradient down to the ocean or a lake
**Depends on**: Phase 3
**Requirements**: MountainPass implementation (v2 deferred), HydroPass implementation (v2 deferred)
**Success Criteria** (what must be TRUE):
  1. Mountain tiles (`TileId.Mountain`, `TileId.MountainDeep`) appear on high-elevation areas — not randomly scattered
  2. Rivers flow downhill from source to mouth — no river segment climbs elevation
  3. Lakes form in enclosed low-elevation basins rather than being randomly placed
  4. Water depth (shallow/deep variants) transitions correctly at lake and river edges
**Plans**: TBD

### Phase 5: World Polish
**Goal**: The generated world passes visual quality bar — beaches on coasts, snow on peaks, forests on moderate elevations, a clean border frame, and species seeded on valid terrain
**Depends on**: Phase 4
**Requirements**: FeaturePass implementation (v2 deferred), FramePass implementation (v2 deferred), EntityPass implementation (v2 deferred)
**Success Criteria** (what must be TRUE):
  1. Beach tiles appear at land/water transitions — no land tile directly borders deep ocean without beach or shallow water
  2. Snow tiles appear at the tops of mountain ranges — not at sea level
  3. Border tiles form a clean unbroken frame around the world — no gaps or overwrites
  4. Species spawn on valid terrain (crabs on beaches, wolves in forests, fish in water) — no species on impassable or wrong-habitat tiles
**Plans**: TBD

### Phase 6: Camera & World Size
**Goal**: The camera scrolls over worlds larger than the console window — a 300x150 world renders correctly through a 120x40 viewport with camera panning
**Depends on**: Phase 5
**Requirements**: GEN-09 full implementation (viewport decoupling beyond the stub in Phase 1)
**Success Criteria** (what must be TRUE):
  1. Setting `conf.Width = 300, conf.Height = 150` on a standard terminal generates and saves without crashing or truncation
  2. The camera pans across the world — arrow keys or WASD scroll the viewport over the larger world
  3. The framebuffer dirty renderer correctly invalidates only the changed cells when scrolling — no full-screen flicker per scroll step
  4. Species and simulation logic operate on the full world size, not just the viewport region
**Plans**: TBD

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Generator Pipeline Base | 0/5 | Not started | - |
| 2. Elevation & Climate | 0/3 | Not started | - |
| 3. Biome Assignment | 0/1 | Not started | - |
| 4. Terrain Features | 0/0 | Not started | - |
| 5. World Polish | 0/0 | Not started | - |
| 6. Camera & World Size | 0/0 | Not started | - |
