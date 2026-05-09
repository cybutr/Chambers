using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Crab),    "Crab")]
[JsonDerivedType(typeof(Turtle),  "Turtle")]
[JsonDerivedType(typeof(Cow),     "Cow")]
[JsonDerivedType(typeof(Sheep),   "Sheep")]
[JsonDerivedType(typeof(Wolf),    "Wolf")]
[JsonDerivedType(typeof(Bear),    "Bear")]
[JsonDerivedType(typeof(Goat),    "Goat")]
[JsonDerivedType(typeof(Fish),    "Fish")]
[JsonDerivedType(typeof(Bird),    "Bird")]
public abstract class Species
{
    [JsonIgnore] public string          Name        { get; set; }
    [JsonIgnore] public string          Habitat     { get; set; }
    public              int             X           { get; set; }
    public              int             Y           { get; set; }
    public              int             seedOffset  { get; set; }
    [JsonIgnore] public HashSet<TileId> allowedTiles{ get; set; } = [];
    public              bool            isAggressive   { get; set; }
    public (int r, int g, int b)        colorVariation { get; set; }
    public float                        hunger         { get; set; } = 0f;
    public int                          breedCooldown  { get; set; } = 0;
    public float                        health         { get; set; } = 1.0f;
    public int                          age            { get; set; } = 0;
    public int                          maxAge         { get; set; } = 0;
    public bool                         isMature       { get; set; } = true;
    public int                          HerdId         { get; set; } = -1;
    [JsonIgnore] public float           colorPulse     { get; set; } = 0f;
    [JsonIgnore] public (int r, int g, int b) colorPulseTarget { get; set; } = (255, 0, 0);
    [JsonIgnore] public bool            isHibernating  { get; set; } = false;
    public              int             attackCooldown { get; set; } = 0;
    [JsonIgnore] public abstract EntityId EntityId  { get; }

    protected Random rng { get; private set; }

    protected List<(int x, int y)>? _path;
    protected int                   _pathIdx;

    protected Species(string name, string habitat, int x, int y, int seedOffset)
    {
        Name            = name;
        Habitat         = habitat;
        X               = x;
        Y               = y;
        this.seedOffset = seedOffset;
        rng             = new Random(seedOffset);
    }

    protected Species(string name, string habitat, int seedOffset)
        : this(name, habitat, 0, 0, seedOffset) { }

    public abstract void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx);

    protected static double GetDistance(int x1, int y1, int x2, int y2)
    {
        int dx = x2 - x1, dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    protected bool FollowPath(TileId[,] mapData)
    {
        if (_path == null || _pathIdx >= _path.Count) { _path = null; return false; }
        var (nx, ny) = _path[_pathIdx];
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        if (nx < 0 || nx >= w || ny < 0 || ny >= h) { _path = null; return false; }
        (X, Y) = (nx, ny);
        _pathIdx++;
        if (_pathIdx >= _path.Count) _path = null;
        return true;
    }

    protected void SetPath(AStar astar, TileId[,] mapData, int gx, int gy)
    {
        _path    = astar.FindPath(mapData, X, Y, gx, gy);
        _pathIdx = 0;
    }

    protected void MoveRandomly(TileId[,] mapData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        Span<int> dirs = stackalloc int[4];
        int count = 0;
        if (Y > 0   && allowedTiles.Contains(mapData[X, Y - 1])) dirs[count++] = 0;
        if (Y < h-1 && allowedTiles.Contains(mapData[X, Y + 1])) dirs[count++] = 1;
        if (X > 0   && allowedTiles.Contains(mapData[X - 1, Y])) dirs[count++] = 2;
        if (X < w-1 && allowedTiles.Contains(mapData[X + 1, Y])) dirs[count++] = 3;
        if (count == 0) return;
        switch (dirs[rng.Next(count)])
        {
            case 0: Y--; break;
            case 1: Y++; break;
            case 2: X--; break;
            case 3: X++; break;
        }
    }

    protected void MoveToward(TileId[,] mapData, int tx, int ty)
    {
        int w  = mapData.GetLength(0), h = mapData.GetLength(1);
        int nx = X + (tx > X ? 1 : tx < X ? -1 : 0);
        int ny = Y + (ty > Y ? 1 : ty < Y ? -1 : 0);
        if (nx >= 0 && nx < w && ny >= 0 && ny < h && allowedTiles.Contains(mapData[nx, ny]))
            (X, Y) = (nx, ny);
    }

    protected void MoveRandomlyNear(TileId[,] mapData, int anchorX, int anchorY, double maxDist)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        Span<int> order = stackalloc int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 7; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        ReadOnlySpan<int> dx8 = stackalloc int[8] { -1, 1, 0, 0, -1, -1, 1, 1 };
        ReadOnlySpan<int> dy8 = stackalloc int[8] {  0, 0,-1, 1, -1,  1,-1, 1 };
        for (int k = 0; k < 8; k++)
        {
            int idx = order[k];
            int nx  = X + dx8[idx], ny = Y + dy8[idx];
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
            if (!allowedTiles.Contains(mapData[nx, ny])) continue;
            if (GetDistance(nx, ny, anchorX, anchorY) > maxDist) continue;
            (X, Y) = (nx, ny);
            return;
        }
    }

    protected void MoveInGroups(TileId[,] mapData, EntityId[,] overlayData, EntityId clusterEntity, EntityId fallbackEntity)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int nearestX = -1, nearestY = -1;
        double minDist = double.MaxValue;

        for (int dx = -7; dx <= 7; dx++)
            for (int dy = -7; dy <= 7; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] != clusterEntity || (cx == X && cy == Y)) continue;
                double dist = GetDistance(X, Y, cx, cy);
                if (dist < minDist) { minDist = dist; nearestX = cx; nearestY = cy; }
            }

        if (nearestX != -1)
        {
            if (minDist > 3) MoveToward(mapData, nearestX, nearestY);
            else MoveRandomlyNear(mapData, nearestX, nearestY, 3);
        }
        else MoveTowardNearest(mapData, overlayData, fallbackEntity);
    }

    protected void MoveTowardNearest(TileId[,] mapData, EntityId[,] overlayData, EntityId entityType, int radius = 20)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == entityType && !(cx == X && cy == Y))
                {
                    MoveToward(mapData, cx, cy);
                    return;
                }
            }
    }

    protected void MoveTowardNearestAllowed(TileId[,] mapData, TileId[] targets, int radius = 10)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int bestX = -1, bestY = -1;
        double bestDist = double.MaxValue;
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                foreach (var t in targets)
                    if (mapData[cx, cy] == t)
                    {
                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist < bestDist) { bestDist = dist; bestX = cx; bestY = cy; }
                    }
            }
        if (bestX != -1) MoveToward(mapData, bestX, bestY);
    }

    protected (bool found, int px, int py) CheckForPredatorsInRange(EntityId[,] overlayData, EntityId predatorType, int radius = 10)
    {
        int w = overlayData.GetLength(0), h = overlayData.GetLength(1);
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == predatorType) return (true, cx, cy);
            }
        return (false, -1, -1);
    }

    protected void AvoidPredators(TileId[,] mapData, int px, int py)
    {
        int w     = mapData.GetLength(0), h = mapData.GetLength(1);
        int moveX = X > px ? 1 : X < px ? -1 : 0;
        int moveY = Y > py ? 1 : Y < py ? -1 : 0;
        ReadOnlySpan<(int, int)> escapes = [(moveX, moveY), (moveX, 0), (0, moveY)];
        foreach (var (dx, dy) in escapes)
        {
            int nx = X + dx, ny = Y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h && allowedTiles.Contains(mapData[nx, ny]))
            {
                (X, Y) = (nx, ny);
                return;
            }
        }
        MoveRandomly(mapData);
    }

    protected void FleeFearMap(TileId[,] mapData, float[,] fearMap)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int bestX = X, bestY = Y;
        float lowestFear = fearMap[X, Y];
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = X + dx, ny = Y + dy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (!allowedTiles.Contains(mapData[nx, ny])) continue;
                if (fearMap[nx, ny] < lowestFear) { lowestFear = fearMap[nx, ny]; bestX = nx; bestY = ny; }
            }
        (X, Y) = (bestX, bestY);
    }

    protected virtual void SearchForFood() { }
    protected virtual void Attack() { }

    protected bool ShouldAct(in SimContext ctx)
    {
        var def = EntityRegistry.Get(EntityId);
        if (def.Activity == ActivityPattern.Diurnal   && ctx.IsNight) return false;
        if (def.Activity == ActivityPattern.Nocturnal && !ctx.IsNight) return false;
        float chance = ctx.IsNight ? def.NightMoveChance : def.MoveChance;
        return rng.NextDouble() < chance;
    }

    protected (int r, int g, int b) SeedColorVariation(EntityId eid)
    {
        int range = EntityRegistry.Get(eid).ColorVarianceRange;
        return range > 0
            ? (rng.Next(-range, range + 1), rng.Next(-range, range + 1), rng.Next(-range, range + 1))
            : (0, 0, 0);
    }

    public (int r, int g, int b) MutateColor(Random mutRng, int drift) =>
        (Math.Clamp(colorVariation.r + mutRng.Next(-drift, drift + 1), -255, 255),
         Math.Clamp(colorVariation.g + mutRng.Next(-drift, drift + 1), -255, 255),
         Math.Clamp(colorVariation.b + mutRng.Next(-drift, drift + 1), -255, 255));

    public bool RollChance(float chance) => rng.NextDouble() < chance;

    public void Pulse((int r, int g, int b) target, float strength = 1f)
    {
        colorPulseTarget = target;
        colorPulse       = Math.Clamp(strength, 0f, 1f);
    }

    public void DecayPulse(float rate = 0.80f) =>
        colorPulse = colorPulse < 0.01f ? 0f : colorPulse * rate;

    protected void InitBreedCooldown(int maxCooldown) =>
        breedCooldown = maxCooldown > 0 ? rng.Next(0, maxCooldown) : 0;

    protected void InitAge(EntityDefinition def)
    {
        health   = def.MaxHealth;
        isMature = def.MaturityAge == 0;
        if (def.MaxAge > 0)
        {
            int lo = (int)(def.MaxAge * (1f - def.AgeDeathVariance));
            int hi = (int)(def.MaxAge * (1f + def.AgeDeathVariance));
            maxAge = rng.Next(Math.Max(1, lo), hi + 1);
        }
    }
}

#region species behavior

#region beach species
public class Crab : Species
{
    public override EntityId EntityId => EntityId.Crab;

    public Crab(int x, int y, int seedOffset) : base("Crab", "Beach", x, y, seedOffset)
    {
        allowedTiles   = EntityRegistry.Get(EntityId.Crab).AllowedTiles;
        isAggressive   = rng.NextDouble() > 0.7;
        colorVariation = SeedColorVariation(EntityId.Crab);
        InitBreedCooldown(EntityRegistry.Get(EntityId.Crab).BreedCooldown);
        InitAge(EntityRegistry.Get(EntityId.Crab));
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        if (!ShouldAct(ctx)) return;

        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int nearX = -1, nearY = -1;
        for (int dx = -5; dx <= 5 && nearX == -1; dx++)
            for (int dy = -5; dy <= 5 && nearX == -1; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == EntityId.Crab && !(cx == X && cy == Y))
                { nearX = cx; nearY = cy; }
            }

        if (nearX != -1) MoveRandomlyNear(mapData, nearX, nearY, 5);
        else MoveRandomly(mapData);
    }
}

public class Turtle : Species
{
    private static readonly HashSet<TileId> _tiles      = [TileId.Beach, TileId.BeachDark];
    private static readonly HashSet<TileId> _swimTiles  = [TileId.Beach, TileId.BeachDark,
        TileId.Ocean, TileId.OceanShallow, TileId.Lake, TileId.LakeShallow,
        TileId.River, TileId.RiverShallow];
    private static readonly AStar _astar = new(AStar.CreateAnimalConfig("amphibian"));

    public override EntityId EntityId => EntityId.Turtle;

    private bool _onBeach;
    private int  _restTicks;
    [JsonIgnore] private readonly float _swimChance;
    [JsonIgnore] private readonly int   _restMin, _restMax;

    public Turtle(int x, int y, int seedOffset) : base("Turtle", "Beach", x, y, seedOffset)
    {
        var def        = EntityRegistry.Get(EntityId.Turtle);
        allowedTiles   = _tiles;
        isAggressive   = rng.NextDouble() > 0.7;
        _onBeach       = true;
        _swimChance    = def.SwimChance;
        _restMin       = def.RestMin;
        _restMax       = def.RestMax;
        colorVariation = SeedColorVariation(EntityId.Turtle);
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        // Fear — flee wolf via gradient
        if (ctx.FearMap != null && ctx.FearMap[X, Y] > 0.4f)
        {
            var saved = allowedTiles;
            allowedTiles = _swimTiles;
            FleeFearMap(mapData, ctx.FearMap);
            allowedTiles = saved;
            return;
        }

        if (_onBeach)
        {
            if (_restTicks > 0) { _restTicks--; return; }
            // Occasionally swim out
            if (rng.NextDouble() < _swimChance) { _onBeach = false; _path = null; }
            else MoveRandomly(mapData);
        }
        else
        {
            // Swimming: use extended tile set
            var saved = allowedTiles;
            allowedTiles = _swimTiles;

            if (_path != null) { FollowPath(mapData); allowedTiles = saved; return; }

            // 15% chance head to beach
            if (rng.NextDouble() < 0.15)
            {
                var (bx, by) = FindNearestBeach(mapData);
                if (bx != -1) { SetPath(_astar, mapData, bx, by); _onBeach = false; }
            }
            else MoveRandomly(mapData);

            allowedTiles = saved;

            // If on beach tile, switch to rest mode
            if (TileRegistry.Get(mapData[X, Y]).IsLand && _tiles.Contains(mapData[X, Y]))
            {
                _onBeach    = true;
                _restTicks  = rng.Next(_restMin, _restMax);
                allowedTiles = _tiles;
            }
        }
    }

    private (int x, int y) FindNearestBeach(TileId[,] mapData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        for (int r = 1; r < Math.Max(w, h); r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                    int cx = X + dx, cy = Y + dy;
                    if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                    if (_tiles.Contains(mapData[cx, cy])) return (cx, cy);
                }
        return (-1, -1);
    }
}
#endregion

#region plains species
public class Cow : Species
{
    public override EntityId EntityId => EntityId.Cow;

    private int _anchorX;
    private int _anchorY;
    [JsonIgnore] private readonly float _anchorRadius;

    public Cow(int x, int y, int seedOffset) : base("Cow", "Plains", x, y, seedOffset)
    {
        var def        = EntityRegistry.Get(EntityId.Cow);
        allowedTiles   = def.AllowedTiles;
        isAggressive   = rng.NextDouble() > 0.7;
        _anchorX       = x;
        _anchorY       = y;
        _anchorRadius  = def.AnchorRadius;
        colorVariation = SeedColorVariation(EntityId.Cow);
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        if (ctx.FearMap != null && ctx.FearMap[X, Y] > 0.3f)
        {
            FleeFearMap(mapData, ctx.FearMap);
            return;
        }
        if (!ShouldAct(ctx)) return;

        if (GetDistance(X, Y, _anchorX, _anchorY) > _anchorRadius)
            MoveToward(mapData, _anchorX, _anchorY);
        else
            MoveInGroups(mapData, overlayData, EntityId.Cow, EntityId.Cow);
    }
}

public class Sheep : Species
{
    public override EntityId EntityId => EntityId.Sheep;

    private int _anchorX;
    private int _anchorY;
    [JsonIgnore] private readonly float _anchorRadius;

    public Sheep(int x, int y, int seedOffset) : base("Sheep", "Plains", x, y, seedOffset)
    {
        var def        = EntityRegistry.Get(EntityId.Sheep);
        allowedTiles   = def.AllowedTiles;
        isAggressive   = rng.NextDouble() > 0.7;
        _anchorX       = x;
        _anchorY       = y;
        _anchorRadius  = def.AnchorRadius;
        colorVariation = SeedColorVariation(EntityId.Sheep);
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        if (ctx.FearMap != null && ctx.FearMap[X, Y] > 0.1f)
        {
            FleeFearMap(mapData, ctx.FearMap);
            return;
        }
        if (!ShouldAct(ctx)) return;

        if (GetDistance(X, Y, _anchorX, _anchorY) > _anchorRadius)
            MoveToward(mapData, _anchorX, _anchorY);
        else
            MoveInGroups(mapData, overlayData, EntityId.Sheep, EntityId.Sheep);
    }
}
#endregion

#region forest species
public class Wolf : Species
{
    public override EntityId EntityId => EntityId.Wolf;

    [JsonIgnore] private readonly HunterBehavior _hunter;

    public Wolf(int x, int y, int seedOffset) : base("Wolf", "Forest", x, y, seedOffset)
    {
        var def        = EntityRegistry.Get(EntityId.Wolf);
        allowedTiles   = def.AllowedTiles;
        _hunter        = new(allowedTiles, def.PreyIds, def.HuntAllowedTiles) { ScanRadius = def.HuntScanRadius };
        colorVariation = SeedColorVariation(EntityId.Wolf);
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        bool hunted = ctx.EnableAnimalHunting && _hunter.Execute(this, mapData, overlayData, rng, ctx);
        if (hunted) return;
        if (!allowedTiles.Contains(mapData[X, Y])) { MoveTowardNearestAllowed(mapData, [.. allowedTiles]); return; }
        if (ShouldAct(ctx)) MoveRandomly(mapData);
    }
}

public class Bear : Species
{
    public override EntityId EntityId => EntityId.Bear;

    [JsonIgnore] private readonly HunterBehavior      _hunter;
    [JsonIgnore] private readonly HibernationBehavior _hibernate;

    public Bear(int x, int y, int seedOffset) : base("Bear", "Forest", x, y, seedOffset)
    {
        var def            = EntityRegistry.Get(EntityId.Bear);
        allowedTiles       = def.AllowedTiles;
        _hunter            = new(allowedTiles, def.PreyIds, def.HuntAllowedTiles) { ScanRadius = def.HuntScanRadius };
        _hibernate         = new() { TriggerSeason = def.HibernateSeason, PreferredTiles = [TileId.Forest], HibernateChance = def.HibernateChance };
        colorVariation     = SeedColorVariation(EntityId.Bear);
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        if (_hibernate.ShouldSeekHibernationSpot(ctx, mapData[X, Y]))
        { MoveTowardNearestAllowed(mapData, [TileId.Forest]); isHibernating = false; return; }
        isHibernating = _hibernate.IsHibernating(ctx, mapData[X, Y], rng);
        if (isHibernating) return;
        if (ctx.EnableAnimalHunting && _hunter.Execute(this, mapData, overlayData, rng, ctx)) return;
        if (!allowedTiles.Contains(mapData[X, Y])) { MoveTowardNearestAllowed(mapData, [.. allowedTiles]); return; }
        if (ShouldAct(ctx)) MoveRandomly(mapData);
    }
}
#endregion

#region mountain species
public class Goat : Species
{
    public override EntityId EntityId => EntityId.Goat;

    public Goat(int x, int y, int seedOffset) : base("Goat", "Mountain", x, y, seedOffset)
    {
        var def        = EntityRegistry.Get(EntityId.Goat);
        allowedTiles   = def.AllowedTiles;
        colorVariation = SeedColorVariation(EntityId.Goat);
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        if (ctx.FearMap != null && ctx.FearMap[X, Y] > 0.5f)
        {
            FleeFearMap(mapData, ctx.FearMap);
            return;
        }
        if (!ShouldAct(ctx)) return;

        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        Span<int> prefDirs = stackalloc int[4];
        Span<int> anyDirs  = stackalloc int[4];
        int pc = 0, ac = 0;

        static bool IsMountain(TileId t) =>
            t == TileId.Mountain || t == TileId.MountainDeep || t == TileId.Snow;

        if (Y > 0)   { var t = mapData[X, Y-1]; if (allowedTiles.Contains(t)) { if (IsMountain(t)) prefDirs[pc++] = 0; else anyDirs[ac++] = 0; } }
        if (Y < h-1) { var t = mapData[X, Y+1]; if (allowedTiles.Contains(t)) { if (IsMountain(t)) prefDirs[pc++] = 1; else anyDirs[ac++] = 1; } }
        if (X > 0)   { var t = mapData[X-1, Y]; if (allowedTiles.Contains(t)) { if (IsMountain(t)) prefDirs[pc++] = 2; else anyDirs[ac++] = 2; } }
        if (X < w-1) { var t = mapData[X+1, Y]; if (allowedTiles.Contains(t)) { if (IsMountain(t)) prefDirs[pc++] = 3; else anyDirs[ac++] = 3; } }

        bool goMountain    = pc > 0 && (ac == 0 || rng.NextDouble() < 0.8);
        Span<int> chosen   = goMountain ? prefDirs[..pc] : anyDirs[..ac];
        if (chosen.IsEmpty) return;
        switch (chosen[rng.Next(chosen.Length)])
        {
            case 0: Y--; break;
            case 1: Y++; break;
            case 2: X--; break;
            case 3: X++; break;
        }
    }
}
#endregion

#region water species
public class Fish : Species
{
    public override EntityId EntityId => EntityId.Fish;

    [JsonIgnore] private readonly BoidBehavior _boids;

    public Fish(int x, int y, int seedOffset) : base("Fish", "Water", x, y, seedOffset)
    {
        var def        = EntityRegistry.Get(EntityId.Fish);
        allowedTiles   = def.AllowedTiles;
        colorVariation = SeedColorVariation(EntityId.Fish);
        _boids         = new() { CohesionRadius = def.BoidCohesionRadius, SeparationDist = def.BoidSeparationDist, MaxBoidSize = def.MaxBoidSize };
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        if (!ShouldAct(ctx)) return;
        _boids.Execute(this, mapData, overlayData, EntityId.Fish, rng);
    }
}
#endregion

#region air species
public class Bird : Species
{
    private static readonly HashSet<TileId> _allTiles = [
        TileId.Plains, TileId.Forest, TileId.Beach, TileId.BeachDark,
        TileId.Mountain, TileId.MountainDeep, TileId.Snow,
        TileId.Ocean, TileId.OceanShallow, TileId.River, TileId.RiverShallow,
        TileId.Lake, TileId.LakeShallow, TileId.Stream, TileId.Empty
    ];
    public override EntityId EntityId => EntityId.Bird;

    [JsonIgnore] private readonly BoidBehavior _boids;
    [JsonIgnore] private readonly HunterBehavior _hunter;

    public Bird(int x, int y, int seedOffset) : base("Bird", "Air", x, y, seedOffset)
    {
        var def        = EntityRegistry.Get(EntityId.Bird);
        allowedTiles   = _allTiles;
        colorVariation = SeedColorVariation(EntityId.Bird);
        _boids         = new() { CohesionRadius = def.BoidCohesionRadius, SeparationDist = def.BoidSeparationDist, MaxBoidSize = def.MaxBoidSize };
        _hunter        = new(allowedTiles, def.PreyIds, def.HuntAllowedTiles) { ScanRadius = def.HuntScanRadius };
        InitBreedCooldown(def.BreedCooldown);
        InitAge(def);
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData, in SimContext ctx)
    {
        if (ctx.EnableAnimalHunting && _hunter.Execute(this, mapData, overlayData, rng, ctx)) return;
        if (!ShouldAct(ctx)) return;
        _boids.Execute(this, mapData, overlayData, EntityId.Bird, rng);
    }
}
#endregion
#endregion
