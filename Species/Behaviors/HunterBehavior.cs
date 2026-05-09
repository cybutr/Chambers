using System;
using System.Collections.Generic;

public sealed class HunterBehavior
{
    public int ScanRadius  { get; init; } = 15;
    public int ReplanEvery { get; init; } = 5;

    private readonly AStar      _astar;
    private readonly AStar      _huntAstar;
    private readonly EntityId[] _prey;
    private List<(int x, int y)>? _path;
    private int  _pathIdx, _pathSteps;
    private bool _hunting;
    private int  _targetX, _targetY;

    public HunterBehavior(HashSet<TileId> allowedTiles, EntityId[] prey, HashSet<TileId>? huntAllowedTiles = null)
    {
        var config = new AStar.TerrainConfig();
        foreach (TileId t in System.Enum.GetValues<TileId>())
            if (!allowedTiles.Contains(t)) config.BlockTerrain(t);
        config.PathRandomness  = 0.2f;
        config.DiagonalPenalty = 1.3f;
        _astar = new(config);

        if (huntAllowedTiles != null)
        {
            var hconfig = new AStar.TerrainConfig();
            foreach (TileId t in System.Enum.GetValues<TileId>())
                if (!huntAllowedTiles.Contains(t)) hconfig.BlockTerrain(t);
            hconfig.PathRandomness  = 0.2f;
            hconfig.DiagonalPenalty = 1.3f;
            _huntAstar = new(hconfig);
        }
        else _huntAstar = _astar;

        _prey = prey;
    }

    public bool Execute(Species self, TileId[,] mapData, EntityId[,] overlayData, Random rng, in SimContext ctx)
    {
        var def = EntityRegistry.Get(self.EntityId);
        if (def.HuntActivity == ActivityPattern.Diurnal && ctx.IsNight ||
            def.HuntActivity == ActivityPattern.Nocturnal && !ctx.IsNight)
        {
            _hunting = false;
            _path = null;
            return false;
        }

        if (def.HuntAttemptChance > 0f && rng.NextDouble() > def.HuntAttemptChance)
            return false;

        var (found, px, py) = ScanForPrey(self, overlayData, rng, ctx);
        if (found)
        {
            if (!_hunting || _pathSteps >= ReplanEvery || _path == null ||
                Math.Sqrt((_targetX - px) * (_targetX - px) + (_targetY - py) * (_targetY - py)) > 2)
            {
                _targetX  = px; _targetY = py;
                _path     = _huntAstar.FindPath(mapData, self.X, self.Y, px, py);
                _pathIdx  = 0; _pathSteps = 0;
            }
            _hunting = true;
            if (_path != null && _pathIdx < _path.Count)
            {
                var (nx, ny) = _path[_pathIdx];
                int w = mapData.GetLength(0), h = mapData.GetLength(1);
                if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                { self.X = nx; self.Y = ny; _pathIdx++; _pathSteps++; }
                if (_pathIdx >= _path.Count) _path = null;
            }
            return true;
        }
        _hunting = false; _path = null;
        return false;
    }

    private (bool found, int px, int py) ScanForPrey(Species self, EntityId[,] ov, Random rng, in SimContext ctx)
    {
        int w = ov.GetLength(0), h = ov.GetLength(1);
        int bestX = -1, bestY = -1;
        float bestScore = float.MinValue;
        for (int dx = -ScanRadius; dx <= ScanRadius; dx++)
            for (int dy = -ScanRadius; dy <= ScanRadius; dy++)
            {
                int cx = self.X + dx, cy = self.Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                EntityId preyId = ov[cx, cy];
                if (Array.IndexOf(_prey, preyId) < 0) continue;

                Species? prey = ctx.GetSpeciesAt?.Invoke(cx, cy);
                float score = ScorePrey(self, preyId, prey, cx, cy);
                if (score > bestScore || score == bestScore && rng.NextDouble() < 0.5)
                {
                    bestScore = score;
                    bestX = cx;
                    bestY = cy;
                }
            }
        return bestX == -1 ? (false, -1, -1) : (true, bestX, bestY);
    }

    private float ScorePrey(Species self, EntityId preyId, Species? prey, int px, int py)
    {
        var def = EntityRegistry.Get(self.EntityId);
        int index = Array.IndexOf(_prey, preyId);
        if (index < 0) return float.MinValue;

        float distance = MathF.Sqrt((self.X - px) * (self.X - px) + (self.Y - py) * (self.Y - py));
        float distanceScore = 1f - Math.Clamp(distance / Math.Max(1, ScanRadius), 0f, 1f);
        float orderScore = _prey.Length - index;
        float healthScore = 0.5f;
        float ageScore = 0.5f;

        if (prey != null && prey.maxAge > 0)
        {
            float healthRatio = prey.health > 0f ? Math.Clamp(prey.health / Math.Max(0.0001f, EntityRegistry.Get(preyId).MaxHealth), 0f, 1f) : 0f;
            float ageRatio    = Math.Clamp((float)prey.age / prey.maxAge, 0f, 1f);
            healthScore = 1f - Math.Clamp(Math.Abs(healthRatio - def.HuntHealthTarget), 0f, 1f);
            ageScore    = 1f - Math.Clamp(Math.Abs(ageRatio - def.HuntAgeTarget), 0f, 1f);
        }

        return orderScore * def.HuntOrderWeight + healthScore * def.HuntHealthWeight +
               ageScore * def.HuntAgeWeight + distanceScore * def.HuntDistanceWeight;
    }
}
