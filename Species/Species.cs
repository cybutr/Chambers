using System;
using System.Collections.Generic;
using System.Linq;

public abstract class Species
{
    public string Name { get; set; }
    public string Habitat { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int seedOffset { get; set; }
    protected Random rng { get; set; }
    public List<TileId> allowedTiles { get; set; } = new();
    public bool isAggressive { get; set; }
    public bool predatorNearby { get; set; }
    public bool isHunted { get; set; }
    public int predatorX { get; set; }
    public int predatorY { get; set; }

    protected Species(string name, string habitat, int x, int y, int seedOffset)
    {
        Name = name;
        Habitat = habitat;
        X = x;
        Y = y;
        this.seedOffset = seedOffset;
        rng = new Random(seedOffset);
    }

    // Minimal constructor for stub species (Bear, Wolf, Goat, Fish, Bird)
    protected Species(string name, string habitat, int seedOffset)
        : this(name, habitat, 0, 0, seedOffset) { }

    public abstract void Behave(TileId[,] mapData, EntityId[,] overlayData);

    protected static double GetDistance(int x1, int y1, int x2, int y2)
    {
        int dx = x2 - x1, dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    protected void MoveRandomly(TileId[,] mapData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        var dirs = new List<int>();
        if (Y > 0 && allowedTiles.Contains(mapData[X, Y - 1])) dirs.Add(0);
        if (Y < h - 1 && allowedTiles.Contains(mapData[X, Y + 1])) dirs.Add(1);
        if (X > 0 && allowedTiles.Contains(mapData[X - 1, Y])) dirs.Add(2);
        if (X < w - 1 && allowedTiles.Contains(mapData[X + 1, Y])) dirs.Add(3);
        if (dirs.Count == 0) return;
        switch (dirs[rng.Next(dirs.Count)])
        {
            case 0: Y--; break;
            case 1: Y++; break;
            case 2: X--; break;
            case 3: X++; break;
        }
    }

    protected void CheckForPredatorsInRange(EntityId[,] overlayData, EntityId predatorType, int radius = 10)
    {
        int w = overlayData.GetLength(0), h = overlayData.GetLength(1);
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == predatorType)
                {
                    predatorNearby = true;
                    predatorX = cx;
                    predatorY = cy;
                    return;
                }
            }
        }
        predatorNearby = false;
    }

    protected void AvoidPredators(TileId[,] mapData)
    {
        if (!predatorNearby) return;
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        isHunted = true;
        int moveX = X > predatorX ? 1 : X < predatorX ? -1 : 0;
        int moveY = Y > predatorY ? 1 : Y < predatorY ? -1 : 0;
        var dirs = new List<(int, int)> { (moveX, moveY), (moveX, 0), (0, moveY) };
        bool moved = false;
        foreach (var (dx, dy) in dirs)
        {
            int nx = X + dx, ny = Y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h && allowedTiles.Contains(mapData[nx, ny]))
            {
                X = nx; Y = ny;
                moved = true;
                break;
            }
        }
        if (!moved) MoveRandomly(mapData);
        else MoveRandomly(mapData);
        isHunted = false;
    }

    protected virtual void SearchForFood() { }
    protected virtual void Attack() { }
}

#region species behavior
public class Boids
{
    public List<Species> Species { get; set; } = new();

    public void AddSpecies(Species species) => Species.Add(species);
    public void RemoveSpecies(Species species) => Species.Remove(species);

    public void Behave(TileId[,] mapData, EntityId[,] overlayData)
    {
        foreach (var species in Species)
            species.Behave(mapData, overlayData);
    }
}
#endregion

#region types of species
#region beach species
public class Crab : Species
{
    public Crab(int x, int y, int seedOffset)
        : base("Crab", "Beach", x, y, seedOffset)
    {
        allowedTiles = new List<TileId> { TileId.Beach, TileId.BeachDark };
        isAggressive = rng.NextDouble() > 0.005;
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData)
    {
        if (rng.NextDouble() > 0.7 && !isHunted)
            MoveRandomly(mapData);
        SearchForFood();
        if (isAggressive) Attack();
        // No overlayData-based predator detection for crabs yet
        AvoidPredators(mapData);
        MoveToBeach(mapData);
    }

    private void MoveToBeach(TileId[,] mapData)
    {
        var (nx, ny) = FindNearestBeachTile(mapData);
        if (nx != -1) { X = nx; Y = ny; }
    }

    private (int, int) FindNearestBeachTile(TileId[,] mapData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int nearestX = -1, nearestY = -1;
        double nearestDist = double.MaxValue;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (!allowedTiles.Contains(mapData[x, y])) continue;
                double dist = GetDistance(X, Y, x, y);
                if (dist < nearestDist) { nearestX = x; nearestY = y; nearestDist = dist; }
            }
        }
        return (nearestX, nearestY);
    }
}

public class Turtle : Species
{
    public Turtle(int x, int y, int seedOffset)
        : base("Turtle", "Beach", x, y, seedOffset)
    {
        allowedTiles = new List<TileId> { TileId.Beach, TileId.BeachDark };
        isAggressive = rng.NextDouble() > 0.005;
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData)
    {
        if (rng.NextDouble() > 0.7 && !isHunted)
            MoveInWater(mapData);
        SearchForFood();
        if (isAggressive) Attack();
        CheckForPredatorsInRange(overlayData, EntityId.Wolf);
        AvoidPredators(mapData);
    }

    private void MoveInWater(TileId[,] mapData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        var extended = new List<TileId>(allowedTiles)
        {
            TileId.Ocean, TileId.OceanShallow, TileId.Lake, TileId.LakeShallow,
            TileId.River, TileId.RiverShallow
        };

        var dirs = new List<int>();
        if (Y > 0 && extended.Contains(mapData[X, Y - 1])) dirs.Add(0);
        if (Y < h - 1 && extended.Contains(mapData[X, Y + 1])) dirs.Add(1);
        if (X > 0 && extended.Contains(mapData[X - 1, Y])) dirs.Add(2);
        if (X < w - 1 && extended.Contains(mapData[X + 1, Y])) dirs.Add(3);
        if (dirs.Count == 0) return;

        switch (dirs[rng.Next(dirs.Count)])
        {
            case 0: Y--; break;
            case 1: Y++; break;
            case 2: X--; break;
            case 3: X++; break;
        }

        if (TileRegistry.Get(mapData[X, Y]).IsWater && !IsNearBeach(mapData, 5) && rng.NextDouble() < 0.2)
            WalkToBeach(mapData, FindNearestBeachTile(mapData));
    }

    private bool IsNearBeach(TileId[,] mapData, int range)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx >= 0 && cx < w && cy >= 0 && cy < h &&
                    (mapData[cx, cy] == TileId.Beach || mapData[cx, cy] == TileId.BeachDark))
                    return true;
            }
        return false;
    }

    private void WalkToBeach(TileId[,] mapData, (int tx, int ty) target)
    {
        if (target.tx == -1 || (X == target.tx && Y == target.ty)) return;
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int dX = target.tx - X, dY = target.ty - Y;
        var moves = new List<(int dx, int dy)>();
        if (dX > 0 && dY > 0) moves.Add((1, 1));
        if (dX > 0 && dY < 0) moves.Add((1, -1));
        if (dX < 0 && dY > 0) moves.Add((-1, 1));
        if (dX < 0 && dY < 0) moves.Add((-1, -1));
        if (dX > 0) moves.Add((1, 0));
        if (dX < 0) moves.Add((-1, 0));
        if (dY > 0) moves.Add((0, 1));
        if (dY < 0) moves.Add((0, -1));
        foreach (var (dx, dy) in moves)
        {
            int nx = X + dx, ny = Y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h &&
                (allowedTiles.Contains(mapData[nx, ny]) || TileRegistry.Get(mapData[nx, ny]).IsWater))
            {
                X = nx; Y = ny;
                break;
            }
        }
    }

    private (int, int) FindNearestBeachTile(TileId[,] mapData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int nearestX = -1, nearestY = -1;
        double nearestDist = double.MaxValue;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (!allowedTiles.Contains(mapData[x, y])) continue;
                double dist = GetDistance(X, Y, x, y);
                if (dist < nearestDist) { nearestX = x; nearestY = y; nearestDist = dist; }
            }
        return (nearestX, nearestY);
    }
}
#endregion

#region plains species
public class Sheep : Species
{
    public bool isNight { get; set; }
    public double time { get; set; }
    public double sunriseTime { get; set; }
    public double sunsetTime { get; set; }

    public Sheep(int x, int y, int seedOffset)
        : base("Sheep", "Plains", x, y, seedOffset)
    {
        allowedTiles = new List<TileId> { TileId.Plains };
        isAggressive = rng.NextDouble() > 0.005;
    }

    public void SetTime(double currentTime, double sunrise, double sunset)
    {
        time = currentTime; sunriseTime = sunrise; sunsetTime = sunset;
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData)
    {
        if (rng.NextDouble() > 0.8 && !isHunted)
            isNight = time > sunsetTime || time < sunriseTime;
        if (rng.NextDouble() > 0.7 && !isHunted && !isNight)
            MoveInGroups(mapData, overlayData);
        SearchForFood();
        if (isAggressive) Attack();
        CheckForPredatorsInRange(overlayData, EntityId.Wolf);
        AvoidPredators(mapData);
    }

    private void MoveInGroups(TileId[,] mapData, EntityId[,] overlayData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        (int x, int y) nearestCow = (-1, -1);
        double minDist = double.MaxValue;

        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -3; dy <= 3; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == EntityId.Cow && !(cx == X && cy == Y))
                {
                    double dist = GetDistance(X, Y, cx, cy);
                    if (dist < minDist) { minDist = dist; nearestCow = (cx, cy); }
                }
            }

        if (nearestCow.x != -1)
        {
            if (minDist > 3)
                MoveToward(mapData, nearestCow.x, nearestCow.y);
            else
                MoveRandomlyNear(mapData, nearestCow.x, nearestCow.y, 3);
        }
        else
        {
            MoveTowardNearestSheep(mapData, overlayData);
        }
    }

    private void MoveTowardNearestSheep(TileId[,] mapData, EntityId[,] overlayData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        for (int dx = -20; dx <= 20; dx++)
            for (int dy = -20; dy <= 20; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == EntityId.Sheep && !(cx == X && cy == Y))
                {
                    MoveToward(mapData, cx, cy);
                    return;
                }
            }
    }

    private void MoveToward(TileId[,] mapData, int tx, int ty)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int nx = X + (tx > X ? 1 : tx < X ? -1 : 0);
        int ny = Y + (ty > Y ? 1 : ty < Y ? -1 : 0);
        if (nx >= 0 && nx < w && ny >= 0 && ny < h && allowedTiles.Contains(mapData[nx, ny]))
        {
            X = nx; Y = ny;
        }
    }

    private void MoveRandomlyNear(TileId[,] mapData, int anchorX, int anchorY, double maxDist)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        var dirs = new List<(int, int)> { (-1,0),(1,0),(0,-1),(0,1),(-1,-1),(-1,1),(1,-1),(1,1) };
        foreach (var (dx, dy) in dirs.OrderBy(_ => rng.Next()))
        {
            int nx = X + dx, ny = Y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h &&
                allowedTiles.Contains(mapData[nx, ny]) &&
                GetDistance(nx, ny, anchorX, anchorY) <= maxDist)
            {
                X = nx; Y = ny;
                return;
            }
        }
    }
}

public class Cow : Species
{
    public bool isNight { get; set; }
    public double time { get; set; }
    public double sunriseTime { get; set; }
    public double sunsetTime { get; set; }

    public Cow(int x, int y, int seedOffset)
        : base("Cow", "Plains", x, y, seedOffset)
    {
        allowedTiles = new List<TileId> { TileId.Plains };
        isAggressive = rng.NextDouble() > 0.005;
    }

    public void SetTime(double currentTime, double sunrise, double sunset)
    {
        time = currentTime; sunriseTime = sunrise; sunsetTime = sunset;
    }

    public override void Behave(TileId[,] mapData, EntityId[,] overlayData)
    {
        if (rng.NextDouble() > 0.8 && !isHunted)
            isNight = time > sunsetTime || time < sunriseTime;
        if (rng.NextDouble() > 0.7 && !isHunted && !isNight)
            MoveInGroups(mapData, overlayData);
        SearchForFood();
        if (isAggressive) Attack();
        CheckForPredatorsInRange(overlayData, EntityId.Wolf);
        AvoidPredators(mapData);
    }

    private void MoveInGroups(TileId[,] mapData, EntityId[,] overlayData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        (int x, int y) nearestCow = (-1, -1);
        double minDist = double.MaxValue;

        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -3; dy <= 3; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == EntityId.Cow && !(cx == X && cy == Y))
                {
                    double dist = GetDistance(X, Y, cx, cy);
                    if (dist < minDist) { minDist = dist; nearestCow = (cx, cy); }
                }
            }

        if (nearestCow.x != -1)
        {
            if (minDist > 3)
                MoveToward(mapData, nearestCow.x, nearestCow.y);
            else
                MoveRandomlyNear(mapData, nearestCow.x, nearestCow.y, 3);
        }
        else
        {
            MoveTowardNearestCow(mapData, overlayData);
        }
    }

    private void MoveTowardNearestCow(TileId[,] mapData, EntityId[,] overlayData)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        for (int dx = -20; dx <= 20; dx++)
            for (int dy = -20; dy <= 20; dy++)
            {
                int cx = X + dx, cy = Y + dy;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                if (overlayData[cx, cy] == EntityId.Cow && !(cx == X && cy == Y))
                {
                    MoveToward(mapData, cx, cy);
                    return;
                }
            }
    }

    private void MoveToward(TileId[,] mapData, int tx, int ty)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        int nx = X + (tx > X ? 1 : tx < X ? -1 : 0);
        int ny = Y + (ty > Y ? 1 : ty < Y ? -1 : 0);
        if (nx >= 0 && nx < w && ny >= 0 && ny < h && allowedTiles.Contains(mapData[nx, ny]))
        {
            X = nx; Y = ny;
        }
    }

    private void MoveRandomlyNear(TileId[,] mapData, int anchorX, int anchorY, double maxDist)
    {
        int w = mapData.GetLength(0), h = mapData.GetLength(1);
        var dirs = new List<(int, int)> { (-1,0),(1,0),(0,-1),(0,1),(-1,-1),(-1,1),(1,-1),(1,1) };
        foreach (var (dx, dy) in dirs.OrderBy(_ => rng.Next()))
        {
            int nx = X + dx, ny = Y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h &&
                allowedTiles.Contains(mapData[nx, ny]) &&
                GetDistance(nx, ny, anchorX, anchorY) <= maxDist)
            {
                X = nx; Y = ny;
                return;
            }
        }
    }
}
#endregion

#region forest species
public class Bear : Species
{
    public Bear(int seedOffset) : base("Bear", "Forest", seedOffset) { }
    public override void Behave(TileId[,] mapData, EntityId[,] overlayData) { }
}

public class Wolf : Species
{
    public Wolf(int seedOffset) : base("Wolf", "Forest", seedOffset) { }
    public override void Behave(TileId[,] mapData, EntityId[,] overlayData) { }
}
#endregion

#region mountain species
public class Goat : Species
{
    public Goat(int seedOffset) : base("Goat", "Mountain", seedOffset) { }
    public override void Behave(TileId[,] mapData, EntityId[,] overlayData) { }
}
#endregion

#region water species
public class Fish : Species
{
    public Fish(int seedOffset) : base("Fish", "Water", seedOffset) { }
    public override void Behave(TileId[,] mapData, EntityId[,] overlayData) { }
}
#endregion

#region air species
public class Bird : Species
{
    public Bird(int seedOffset) : base("Bird", "Air", seedOffset) { }
    public override void Behave(TileId[,] mapData, EntityId[,] overlayData) { }
}
#endregion
#endregion
