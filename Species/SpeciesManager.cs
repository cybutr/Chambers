using System;
using System.Collections.Generic;
using System.Linq;

public class SpeciesManager
{
    private List<Species> _all = [];
    private float[,]      _fearMap = new float[0, 0];
    [System.Text.Json.Serialization.JsonIgnore] private Species?[,] _grid = new Species?[0, 0];
    [System.Text.Json.Serialization.JsonIgnore] private Random      _rng  = new();
    public HerdManager Herds { get; set; } = new();
    private int           _w, _h;

    public List<Species>  Species  => _all;
    public float[,]       FearMap  => _fearMap;

    public Species? GetAt(int x, int y) => x >= 0 && x < _w && y >= 0 && y < _h ? _grid[x, y] : null;

    public void Initialize(Map map)
    {
        _w       = map.width;
        _h       = map.height;
        _fearMap = new float[_w, _h];
        _grid    = new Species?[_w, _h];
        _rng     = new Random(map.rng.Next());
        _all.Clear();

        foreach (var def in EntityRegistry.Spawnable())
        {
            if (def.Spawn.SpawnInHerds)
                SpawnHerd(def, map);
            else
                SpawnScatter(def, map);
        }
        AssignInitialHerds();
    }

    private void AssignInitialHerds()
    {
        foreach (var s in _all)
        {
            var def = EntityRegistry.Get(s.EntityId);
            if (def.MinHerdSize == 0 || s.HerdId != -1) continue;
            Herds.CreateSingletonHerd(s, def, _rng);
        }
        for (int i = 0; i < 3; i++)
            Herds.UpdateAll(_all, _grid, _w, _h, id => EntityRegistry.Get(id), _rng);
    }

    public void PostLoad(int width, int height)
    {
        _w       = width;
        _h       = height;
        _fearMap = new float[_w, _h];
        // Restore allowedTiles from registry for each loaded species (JsonIgnore field)
        foreach (var s in _all)
        {
            var def = EntityRegistry.Get(s.EntityId);
            if (def != null) s.allowedTiles = def.AllowedTiles;
        }
        _grid = new Species?[width, height];
        foreach (var s in _all)
            if (s.X >= 0 && s.X < width && s.Y >= 0 && s.Y < height) _grid[s.X, s.Y] = s;
        Herds.RebuildFromSpecies(_all);
    }

    public void UpdateAll(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx, Func<int, int, bool> isUnderCloud)
    {
        if (ctx.EnablePredators) UpdateFearMap(overlayData, ctx.IsNight);
        else Array.Clear(_fearMap);

        for (int i = 0; i < _all.Count; i++)
        {
            var s    = _all[i];
            int oldX = s.X, oldY = s.Y;

            s.Behave(mapData, overlayData, ctx);

            if (overlayData[s.X, s.Y] != EntityId.None)
            {
                s.X = oldX;
                s.Y = oldY;
            }
            else
            {
                overlayData[oldX, oldY] = EntityId.None;
                _grid[oldX, oldY] = null;
                if (!isUnderCloud(s.X, s.Y)) overlayData[s.X, s.Y] = s.EntityId;
                _grid[s.X, s.Y] = s;
            }
        }

        for (int i = 0; i < _all.Count; i++) _all[i].DecayPulse();
        Herds.UpdateAll(_all, _grid, _w, _h, id => EntityRegistry.Get(id), _rng);
        ProcessAttacks(overlayData, ctx);
        ProcessHunger(mapData, overlayData, ctx);
        ProcessAge(overlayData, ctx);
        if (ctx.EnableAnimalBreeding) ProcessBreeding(mapData, overlayData, ctx);
    }

    public int CountOf(EntityId id) => _all.Count(s => s.EntityId == id);

    public string GetSummary()
    {
        var groups = _all.GroupBy(s => s.EntityId).OrderBy(g => g.Key);
        return string.Join(" | ", groups.Select(g => $"{g.Key}: {g.Count()}"));
    }

    public Species? Spawn(EntityId id, int x, int y, int seed, EntityId[,] overlayData)
    {
        var def = EntityRegistry.Get(id);
        if (def?.Factory == null) return null;
        var s = def.Factory(x, y, seed);
        _all.Add(s);
        if (x >= 0 && x < _w && y >= 0 && y < _h) _grid[x, y] = s;
        overlayData[x, y] = id;
        EventBus.Emit(new EntitySpawnedEvent(id, x, y));
        return s;
    }

    public int ClearType(EntityId id, EntityId[,] overlayData)
    {
        int removed = 0;
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            var s = _all[i];
            if (s.EntityId != id) continue;
            overlayData[s.X, s.Y] = EntityId.None;
            _grid[s.X, s.Y] = null;
            _all.RemoveAt(i);
            removed++;
        }
        return removed;
    }

    private void UpdateFearMap(EntityId[,] overlayData, bool isNight)
    {
        if (_fearMap.GetLength(0) != _w || _fearMap.GetLength(1) != _h)
            _fearMap = new float[_w, _h];

        Array.Clear(_fearMap);

        float nightBonus = isNight ? 1.5f : 1.0f;

        foreach (var s in _all)
        {
            var def = EntityRegistry.Get(s.EntityId);
            if (def.FearRadius <= 0f) continue;
            int radius = (int)def.FearRadius;
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int cx = s.X + dx, cy = s.Y + dy;
                    if (cx < 0 || cx >= _w || cy < 0 || cy >= _h) continue;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist > def.FearRadius) continue;
                    _fearMap[cx, cy] += nightBonus * def.FearStrength / (1.0f + dist);
                }
        }
    }

    private void ProcessAttacks(EntityId[,] overlayData, in SimContext ctx)
    {
        ReadOnlySpan<int> dx4 = stackalloc int[4] { -1, 1, 0, 0 };
        ReadOnlySpan<int> dy4 = stackalloc int[4] {  0, 0,-1, 1 };

        foreach (var predator in _all.Where(s => EntityRegistry.Get(s.EntityId).AttackDamage > 0f).ToList())
        {
            var predDef = EntityRegistry.Get(predator.EntityId);
            if (predator.attackCooldown > 0) { predator.attackCooldown--; continue; }
            for (int d = 0; d < 4; d++)
            {
                int cx = predator.X + dx4[d], cy = predator.Y + dy4[d];
                if (cx < 0 || cx >= _w || cy < 0 || cy >= _h) continue;
                var preyId = overlayData[cx, cy];
                bool isValidPrey = false;
                foreach (var p in predDef.PreyIds)
                    if (preyId == p) { isValidPrey = true; break; }
                if (!isValidPrey) continue;

                var prey = _grid[cx, cy];
                if (prey == null) continue;

                prey.health -= predDef.AttackDamage;
                prey.Pulse(ColorSpectrum.RED);
                predator.Pulse(ColorSpectrum.CRIMSON, 0.5f);

                if (predDef.AttackCooldown > 0) predator.attackCooldown = predDef.AttackCooldown;
                if (prey.health <= 0f && ctx.EnableAnimalDeath)
                {
                    overlayData[cx, cy] = EntityId.None;
                    _grid[cx, cy] = null;
                    for (int i = _all.Count - 1; i >= 0; i--)
                        if (_all[i] == prey) { _all.RemoveAt(i); break; }
                    Herds.RemoveMember(prey);
                    predator.hunger = MathF.Max(0f, predator.hunger - 0.6f);
                    predator.Pulse(ColorSpectrum.RED, 1.0f);
                    EventBus.Emit(new EntityDiedEvent(preyId, cx, cy, predator.EntityId));
                }
                break;
            }
        }
    }

    private void ProcessHunger(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            var s   = _all[i];
            var def = EntityRegistry.Get(s.EntityId);
            if (def.HungerRate <= 0f) continue;
            float rate = s.isHibernating ? def.HungerRate * 0.05f : def.HungerRate;
            s.hunger += rate;
            if (def.FoodTiles != null && def.FoodTiles.Contains(mapData[s.X, s.Y]))
                s.hunger = MathF.Max(0f, s.hunger - def.GrazeRate);
            if (s.hunger >= def.StarvationThreshold)
            {
                s.health -= def.HungerRate * 2f;
                s.Pulse(ColorSpectrum.ORANGE, 0.5f);
                if (ctx.EnableAnimalDeath && s.health <= 0f)
                {
                    overlayData[s.X, s.Y] = EntityId.None;
                    _grid[s.X, s.Y] = null;
                    _all.RemoveAt(i);
                    Herds.RemoveMember(s);
                    EventBus.Emit(new EntityDiedEvent(s.EntityId, s.X, s.Y, EntityId.None));
                }
            }
        }
    }

    private void ProcessAge(EntityId[,] overlayData, in SimContext ctx)
    {
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            var s   = _all[i];
            var def = EntityRegistry.Get(s.EntityId);
            s.age++;

            if (!s.isMature && def.MaturityAge > 0 && s.age >= def.MaturityAge)
            {
                s.isMature = true;
                s.Pulse(ColorSpectrum.GREEN, 0.8f);
            }

            if (def.HealRate > 0f && s.health < def.MaxHealth && s.hunger < def.StarvationThreshold * 0.3f)
            {
                float ageRatio = s.maxAge > 0 ? (float)s.age / s.maxAge : 0f;
                s.health = MathF.Min(def.MaxHealth, s.health + def.HealRate * (1f - ageRatio * 0.5f));
            }

            if (s.maxAge > 0 && (float)s.age / s.maxAge > 0.88f)
                s.Pulse(ColorSpectrum.LIGHT_GREY, 0.15f);

            if (ctx.EnableAnimalDeath && s.maxAge > 0 && s.age >= s.maxAge)
            {
                s.Pulse(ColorSpectrum.WHITE, 1f);
                overlayData[s.X, s.Y] = EntityId.None;
                _grid[s.X, s.Y] = null;
                _all.RemoveAt(i);
                Herds.RemoveMember(s);
                EventBus.Emit(new EntityDiedEvent(s.EntityId, s.X, s.Y, EntityId.None));
            }
        }
    }

    private void ProcessBreeding(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        int startCount = _all.Count;
        for (int i = 0; i < startCount; i++)
        {
            var s   = _all[i];
            var def = EntityRegistry.Get(s.EntityId);
            if (def.BreedChance <= 0f) continue;
            if (s.breedCooldown > 0) { s.breedCooldown--; continue; }
            if (!s.isMature) continue;
            if (def.BreedSeason >= 0 && ctx.Season != def.BreedSeason) continue;
            if (def.HungerRate > 0f && s.hunger > def.StarvationThreshold * 0.5f) continue;
            int population = CountOf(s.EntityId);
            if (population >= def.PopMax) continue;
            if (def.MinHerdSize > 0 && s.HerdId != -1)
            {
                var herd = Herds.GetHerd(s.HerdId);
                if (herd != null && herd.Count >= herd.SizeCap) continue;
            }
            if (!HasPartnerNearby(s, def.BreedRadius, def.BreedRequiresMaturePartner)) continue;

            float populationRoom = 1f - (float)population / def.PopMax;
            float breedChance = def.BreedChance * MathF.Pow(Math.Clamp(populationRoom, 0f, 1f), def.BreedPopulationPressure);
            if (!s.RollChance(breedChance)) continue;

            var (bx, by) = FindBirthSpot(s, mapData, overlayData, def);
            if (bx == -1) continue;

            int litter = _rng.Next(def.LitterMin, def.LitterMax + 1);
            var firstOffspring = Spawn(s.EntityId, bx, by, _rng.Next(), overlayData);
            if (firstOffspring != null)
            {
                firstOffspring.Pulse(ColorSpectrum.LIGHT_PINK, 0.9f);
                if (s.HerdId != -1) Herds.AddMember(firstOffspring, s.HerdId);
            }
            for (int li = 1; li < litter; li++)
            {
                var (ox, oy) = FindBirthSpot(s, mapData, overlayData, def);
                if (ox == -1) break;
                var o = Spawn(s.EntityId, ox, oy, _rng.Next(), overlayData);
                if (o != null)
                {
                    o.Pulse(ColorSpectrum.LIGHT_PINK, 0.9f);
                    if (s.HerdId != -1) Herds.AddMember(o, s.HerdId);
                }
            }
            s.Pulse(ColorSpectrum.LIGHT_PINK, 0.6f);
            s.breedCooldown = def.BreedCooldown;
        }
    }

    public string GetHerdSummary() => Herds.GetSummary();

    private bool HasPartnerNearby(Species s, int radius, bool requireMature = false)
    {
        int x1 = Math.Max(0, s.X - radius), x2 = Math.Min(_w - 1, s.X + radius);
        int y1 = Math.Max(0, s.Y - radius), y2 = Math.Min(_h - 1, s.Y + radius);
        for (int x = x1; x <= x2; x++)
            for (int y = y1; y <= y2; y++)
            {
                if (x == s.X && y == s.Y) continue;
                if (_grid[x, y]?.EntityId != s.EntityId) continue;
                if (requireMature && _grid[x, y]?.isMature != true) continue;
                return true;
            }
        return false;
    }

    private (int x, int y) FindBirthSpot(Species parent, TileId[,] mapData, EntityId[,] overlayData, EntityDefinition def)
    {
        int r = def.BreedRadius;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int nx = parent.X + _rng.Next(-r, r + 1);
            int ny = parent.Y + _rng.Next(-r, r + 1);
            if (nx < 0 || nx >= _w || ny < 0 || ny >= _h) continue;
            if (overlayData[nx, ny] != EntityId.None) continue;
            if (!parent.allowedTiles.Contains(mapData[nx, ny])) continue;
            return (nx, ny);
        }
        return (-1, -1);
    }

    #region spawn helpers
    private void SpawnScatter(EntityDefinition def, Map map)
    {
        var  allowed  = def.AllowedTiles;
        int  count    = CountHabitatTiles(allowed, map.mapData, map.width, map.height);
        int  maxCount = count < def.Spawn.Max
            ? (int)Math.Round((double)count / map.rng.Next(1, 3))
            : map.rng.Next(def.Spawn.Min, def.Spawn.Max + 1);

        for (int i = 0; i < maxCount; i++)
        {
            var (x, y) = GetRandomPointInAllowedTiles(allowed, def.AllowedTiles.Count == 0, map);
            if (x == -1) break;
            if (map.overlayData[x, y] != EntityId.None) continue;
            var s = def.Factory!(x, y, map.rng.Next());
            _all.Add(s);
            _grid[x, y] = s;
            map.overlayData[x, y] = def.Id;
            EventBus.Emit(new EntitySpawnedEvent(def.Id, x, y));
        }
    }

    private void SpawnHerd(EntityDefinition def, Map map)
    {
        int herdCount = map.rng.Next(def.Spawn.MinHerdCount, def.Spawn.MaxHerdCount + 1);
        for (int h = 0; h < herdCount; h++)
        {
            var (startX, startY) = FindHerdStart(def, map);
            if (startX == -1) { Map.outputBuffer.Add($"No valid start for {def.Name} herd."); continue; }

            int size = map.rng.Next(def.Spawn.MinHerdSize, def.Spawn.MaxHerdSize + 1);
            for (int i = 0; i < size; i++)
            {
                var (x, y) = GetRandomPointInRange(startX, startY, 1, 3, map);
                    if (x == -1 || !def.AllowedTiles.Contains(map.mapData[x, y])) continue;
                if (map.overlayData[x, y] != EntityId.None) continue;
                var s = def.Factory!(x, y, map.rng.Next());
                _all.Add(s);
                _grid[x, y] = s;
                map.overlayData[x, y] = def.Id;
                EventBus.Emit(new EntitySpawnedEvent(def.Id, x, y));
            }
        }
    }

    private (int x, int y) FindHerdStart(EntityDefinition def, Map map)
    {
        int minDist  = def.Spawn.MinMountainDistance;
        int attempts = 0;
        while (attempts++ < 100)
        {
            var (x, y) = GetRandomPointInAllowedTiles(def.AllowedTiles, false, map);
            if (x == -1) return (-1, -1);
            if (minDist == 0 || IsAtLeastDistanceFromMountains(x, y, minDist, map)) return (x, y);
        }
        return (-1, -1);
    }

    private static int CountHabitatTiles(HashSet<TileId> allowed, TileId[,] mapData, int w, int h)
    {
        int count = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (allowed.Contains(mapData[x, y])) count++;
        return count;
    }

    private static (int x, int y) GetRandomPointInAllowedTiles(HashSet<TileId> allowed, bool anyNonBorder, Map map)
    {
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int x = map.rng.Next(0, map.width);
            int y = map.rng.Next(0, map.height);
            var tile = map.mapData[x, y];
            if (tile == TileId.Border) continue;
            if (anyNonBorder || allowed.Contains(tile)) return (x, y);
        }
        Map.outputBuffer.Add("Failed to find valid spawn tile.");
        return (-1, -1);
    }

    private static (int x, int y) GetRandomPointInRange(int startX, int startY, int minDist, int maxDist, Map map)
    {
        int minDistSq = minDist * minDist;
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int endX = map.rng.Next(startX - maxDist, startX + maxDist + 1);
            int endY = map.rng.Next(startY - maxDist, startY + maxDist + 1);
            int dx   = endX - startX, dy = endY - startY;
            if (dx * dx + dy * dy < minDistSq) continue;
            if (endX < 0 || endX >= map.width || endY < 0 || endY >= map.height) continue;
            return (endX, endY);
        }
        return (startX, startY);
    }

    private static bool IsAtLeastDistanceFromMountains(int x, int y, int minDist, Map map)
    {
        for (int dx = -minDist; dx <= minDist; dx++)
            for (int dy = -minDist; dy <= minDist; dy++)
            {
                int cx = x + dx, cy = y + dy;
                if (cx < 0 || cx >= map.width || cy < 0 || cy >= map.height) continue;
                var t = map.mapData[cx, cy];
                if (t == TileId.Mountain || t == TileId.MountainDeep) return false;
            }
        return true;
    }
    #endregion
}
