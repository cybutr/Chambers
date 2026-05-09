using System;
using System.Collections.Generic;

public sealed class BoidBehavior
{
    public int   CohesionRadius { get; init; } = 6;
    public float SeparationDist { get; init; } = 2f;
    public float MoveChance     { get; init; } = 1f;
    public int   MaxBoidSize    { get; init; } = 10;

    public void Execute(Species self, TileId[,] mapData, EntityId[,] overlayData, EntityId entityType, Random rng)
    {
        if (rng.NextDouble() > MoveChance) return;
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        List<(int x, int y, float dist)> candidates = [];

        for (int dx = -CohesionRadius; dx <= CohesionRadius; dx++)
        for (int dy = -CohesionRadius; dy <= CohesionRadius; dy++)
        {
            int cx = self.X + dx, cy = self.Y + dy;
            if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
            if (overlayData[cx, cy] != entityType || (cx == self.X && cy == self.Y)) continue;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            candidates.Add((cx, cy, dist));
        }

        if (candidates.Count == 0) { RandomWalk(self, mapData, rng); return; }

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
        if (candidates.Count > MaxBoidSize)
            candidates.RemoveRange(MaxBoidSize, candidates.Count - MaxBoidSize);

        int count = candidates.Count;
        int sumX = 0, sumY = 0;
        int nearX = candidates[0].x, nearY = candidates[0].y;
        float minDist = candidates[0].dist;
        foreach (var candidate in candidates)
        {
            sumX += candidate.x;
            sumY += candidate.y;
        }

        int targetX, targetY;
        if (minDist < SeparationDist)
        {
            targetX = self.X + (self.X - nearX);
            targetY = self.Y + (self.Y - nearY);
        }
        else
        {
            targetX = sumX / count;
            targetY = sumY / count;
        }

        int mx = targetX > self.X ? 1 : targetX < self.X ? -1 : 0;
        int my = targetY > self.Y ? 1 : targetY < self.Y ? -1 : 0;
        int nx = self.X + mx, ny = self.Y + my;
        if (nx >= 0 && nx < w && ny >= 0 && ny < h && self.allowedTiles.Contains(mapData[nx, ny]))
        { self.X = nx; self.Y = ny; }
        else RandomWalk(self, mapData, rng);
    }

    private static void RandomWalk(Species self, TileId[,] mapData, Random rng)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        Span<int> dirs = stackalloc int[4];
        int count = 0;
        if (self.Y > 0   && self.allowedTiles.Contains(mapData[self.X, self.Y - 1])) dirs[count++] = 0;
        if (self.Y < h-1 && self.allowedTiles.Contains(mapData[self.X, self.Y + 1])) dirs[count++] = 1;
        if (self.X > 0   && self.allowedTiles.Contains(mapData[self.X - 1, self.Y])) dirs[count++] = 2;
        if (self.X < w-1 && self.allowedTiles.Contains(mapData[self.X + 1, self.Y])) dirs[count++] = 3;
        if (count == 0) return;
        switch (dirs[rng.Next(count)])
        {
            case 0: self.Y--; break;
            case 1: self.Y++; break;
            case 2: self.X--; break;
            case 3: self.X++; break;
        }
    }
}
