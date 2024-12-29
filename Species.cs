using System;
using System.Collections.Generic;
public abstract class Species
{
    public string Name { get; set; }
    public string Habitat { get; set; }

    protected Species(string name, string habitat)
    {
        Name = name;
        Habitat = habitat;
    }

    public abstract void Behave();
}
#region species behavior
public class Boids
{
    public List<Species> Species { get; set; }

    public Boids()
    {
        Species = new List<Species>();
    }

    public void AddSpecies(Species species)
    {
        Species.Add(species);
    }

    public void RemoveSpecies(Species species)
    {
        Species.Remove(species);
    }

    public void Behave()
    {
        foreach (var species in Species)
        {
            species.Behave();
        }
    }
}

#endregion
#region types of species
#region beach species
public class Crab : Species
{
    private Random random;
    public int X { get; set; }
    public int Y { get; set; }
    private int beachWidth;
    private int beachHeight;
    private bool isAggressive;
    private bool predatorNearby;
    private bool isHunted;
    private char[,] mapData;
    private int predatorX;
    private int predatorY;
    private List<char> allowedTiles = new List<char> { 'B', 'b' }; // Add your selected tiles here
    public Crab(int initialX, int initialY, int beachWidth, int beachHeight, char[,] mapData)
        : base("Crab", "Beach")
    {
        random = new Random();
        X = initialX;
        Y = initialY;
        this.beachWidth = beachWidth;
        this.beachHeight = beachHeight;
        this.mapData = mapData;
        isAggressive = random.NextDouble() > 0.005;
    }
    public override void Behave()
    {
        if (random.NextDouble() > 0.7 && !isHunted)
        {
            MoveRandomly();
        }
        SearchForFood();
        if (isAggressive)
        {
            Attack();
        }
        CheckForPredatorsInRange();
        AvoidPredators();
    }
    private void Attack()
    {
        // Simulate attacking with a 10% chance
        bool attack = random.NextDouble() > 0.9;
        if (attack)
        {
            // Logic to attack nearby species
            // e.g., Map.Attack(X, Y);
        }
    }
    private void MoveRandomly()
    {
        List<int> possibleDirections = new List<int>();

        // Check each direction and add to possibleDirections if the tile is allowed
        if (Y > 0 && allowedTiles.Contains(mapData[X, Y - 1])) possibleDirections.Add(0); // Up
        if (Y < beachHeight - 1 && allowedTiles.Contains(mapData[X, Y + 1])) possibleDirections.Add(1); // Down
        if (X > 0 && allowedTiles.Contains(mapData[X - 1, Y])) possibleDirections.Add(2); // Left
        if (X < beachWidth - 1 && allowedTiles.Contains(mapData[X + 1, Y])) possibleDirections.Add(3); // Right

        if (possibleDirections.Count > 0)
        {
            int direction = possibleDirections[random.Next(possibleDirections.Count)];
            switch (direction)
            {
                case 0:
                    Y -= 1; // Move up
                    break;
                case 1:
                    Y += 1; // Move down
                    break;
                case 2:
                    X -= 1; // Move left
                    break;
                case 3:
                    X += 1; // Move right
                    break;
            }
        }
    }
    private void SearchForFood()
    {
        // Simulate searching for food with a 30% chance
        bool foundFood = random.NextDouble() > 0.7;
        if (foundFood)
        {
            // Logic to consume food at (X, Y)
            // e.g., Map.ConsumeFood(X, Y);
        }
    }
    private void AvoidPredators()
    {
        // Simulate predator detection with a 20% chance
        if (predatorNearby)
        {
            isHunted = true;
            // Move away from predator

            // Assuming predatorX and predatorY are the predator's coordinates
            int deltaX = X - predatorX;
            int deltaY = Y - predatorY;

            // Determine move direction
            int moveX = deltaX > 0 ? 1 : deltaX < 0 ? -1 : 0;
            int moveY = deltaY > 0 ? 1 : deltaY < 0 ? -1 : 0;

            // Possible directions sorted by preference
            List<(int, int)> directions = new List<(int, int)>
            {
                (moveX, moveY),     // Diagonal away
                (moveX, 0),         // Horizontal away
                (0, moveY)          // Vertical away
            };

            bool moved = false;

            foreach (var dir in directions)
            {
                int newX = X + dir.Item1;
                int newY = Y + dir.Item2;
                if (newX >= 0 && newX < beachWidth && newY >= 0 && newY < beachHeight && allowedTiles.Contains(mapData[newX, newY]))
                {
                    X = newX;
                    Y = newY;
                    moved = true;
                    break;
                }
            }

            if (!moved)
            {
                // Could not move away, move randomly
                MoveRandomly();
            }
            // Move to a random allowed tile away from the predator
            MoveRandomly();
        }
        // Return to beach and restore normal behavior
        isHunted = false;
        MoveToBeach();
    }
    private void CheckForPredatorsInRange()
    {
        // Check in a 10 tile radius for wolves
        for (int dx = -10; dx <= 10; dx++)
        {
            for (int dy = -10; dy <= 10; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < beachWidth && checkY >= 0 && checkY < beachHeight)
                {
                    // Check if there's a wolf ('W') at this position
                    if (mapData[checkX, checkY] == 'W')
                    {
                        predatorNearby = true;
                        predatorX = checkX;
                        predatorY = checkY;
                        return;
                    }
                    else
                    {
                        predatorNearby = false;
                    }
                }
            }
        }
    }
    private void MoveToBeach()
    {
        // Move towards the nearest beach tile
        var (nearestX, nearestY) = FindNearestBeachTile();
        if (nearestX != -1 && nearestY != -1)
        {
            X = nearestX;
            Y = nearestY;
        }
    }
    private (int, int) FindNearestBeachTile()
    {
        int nearestX = -1;
        int nearestY = -1;
        double nearestDistance = double.MaxValue;

        for (int x = 0; x < beachWidth; x++)
        {
            for (int y = 0; y < beachHeight; y++)
            {
                if (allowedTiles.Contains(mapData[x, y]))
                {
                    double distance = GetDistance(X, Y, x, y);
                    if (distance < nearestDistance)
                    {
                        nearestX = x;
                        nearestY = y;
                        nearestDistance = distance;
                    }
                }
            }
        }

        return (nearestX, nearestY);
    }
    private double GetDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }
}
public class Turtle : Species
{
    private Random random;
    public int X { get; set; }
    public int Y { get; set; }
    private int beachWidth;
    private int beachHeight;
    private bool isAggressive;
    private bool predatorNearby;
    private bool isHunted;
    private char[,] mapData;
    private int mapWidth;
    private int mapHeight;
    private char[,] overlayData;
    private int predatorX;
    private int predatorY;
    private List<char> allowedTiles = new List<char> { 'B', 'b' }; // Add your selected tiles here

    public Turtle(int initialX, int initialY, int beachWidth, int beachHeight, char[,] mapData, char[,] overlayData, int mapWidth, int mapHeight)
        : base("Turtle", "Beach")
    {
        random = new Random();
        X = initialX;
        Y = initialY;
        this.beachWidth = beachWidth;
        this.beachHeight = beachHeight;
        this.mapData = mapData;
        this.mapWidth = mapWidth;
        this.mapHeight = mapHeight;
        this.overlayData = overlayData;
        isAggressive = random.NextDouble() > 0.005;
    }
    public override void Behave()
    {
        if (random.NextDouble() > 0.7 && !isHunted)
        {
            MoveRandomly();
        }
        SearchForFood();
        if (isAggressive)
        {
            Attack();
        }
        CheckForPredatorsInRange();
        if (predatorNearby)
        {
            AvoidPredators();
        }
    }
    private void Attack()
    {
        // Simulate attacking with a 10% chance
        bool attack = random.NextDouble() > 0.9;
        if (attack)
        {
            // Logic to attack nearby species
            // e.g., Map.Attack(X, Y);
        }
    }
    private void MoveRandomly()
    {
        List<int> possibleDirections = new List<int>();
        List<char> currentAllowedTiles = new List<char>(allowedTiles);

        // Allow turtles to move into water tiles
        currentAllowedTiles.AddRange(new[] { 'O', 'o', 'L', 'l', 'R', 'r' });

        // Check each direction and add to possibleDirections if the tile is allowed
        if (Y > 0 && IsValidTile(X, Y - 1, currentAllowedTiles)) possibleDirections.Add(0); // Up
        if (Y < mapHeight - 1 && IsValidTile(X, Y + 1, currentAllowedTiles)) possibleDirections.Add(1); // Down
        if (X > 0 && IsValidTile(X - 1, Y, currentAllowedTiles)) possibleDirections.Add(2); // Left
        if (X < mapWidth - 1 && IsValidTile(X + 1, Y, currentAllowedTiles)) possibleDirections.Add(3); // Right

        if (possibleDirections.Count > 0)
        {
            int direction = possibleDirections[random.Next(possibleDirections.Count)];
            switch (direction)
            {
                case 0: Y -= 1; break; // Move up
                case 1: Y += 1; break; // Move down
                case 2: X -= 1; break; // Move left
                case 3: X += 1; break; // Move right
            }

            // If in water and far from beach, occasionally return to beach
            if (IsWaterTile(mapData[X, Y]) && !IsNearBeach(5) && random.NextDouble() < 0.2)
            {
                var (nearestX, nearestY) = FindNearestBeachTile();
                WalkToBeach(nearestX, nearestY);
            }
        }
    }
    private bool IsValidTile(int x, int y, List<char> allowedTiles)
    {
        char tile = mapData[x, y];
        return allowedTiles.Contains(tile);
    }
    private bool IsWaterTile(char tile)
    {
        return tile == 'O' || tile == 'o' || tile == 'L' || tile == 'l' || tile == 'R' || tile == 'r';
    }
    private bool IsNearBeach(int range)
    {
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                if (checkX >= 0 && checkX < mapWidth &&
                    checkY >= 0 && checkY < mapHeight &&
                    (mapData[checkX, checkY] == 'B' || mapData[checkX, checkY] == 'b'))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private void SearchForFood()
    {
        // Simulate searching for food with a 30% chance
        bool foundFood = random.NextDouble() > 0.7;
        if (foundFood)
        {
            // Logic to consume food at (X, Y)
            // e.g., Map.ConsumeFood(X, Y);
        }
    }
    private void AvoidPredators()
    {
        // Simulate predator detection with a 20% chance
        if (predatorNearby)
        {
            isHunted = true;
            // Move away from predator

            // Assuming predatorX and predatorY are the predator's coordinates
            int deltaX = X - predatorX;
            int deltaY = Y - predatorY;

            // Determine move direction
            int moveX = deltaX > 0 ? 1 : deltaX < 0 ? -1 : 0;
            int moveY = deltaY > 0 ? 1 : deltaY < 0 ? -1 : 0;

            // Possible directions sorted by preference
            List<(int, int)> directions = new List<(int, int)>
            {
                (moveX, moveY),     // Diagonal away
                (moveX, 0),         // Horizontal away
                (0, moveY)          // Vertical away
            };

            bool moved = false;

            foreach (var dir in directions)
            {
                int newX = X + dir.Item1;
                int newY = Y + dir.Item2;
                if (newX >= 0 && newX < beachWidth && newY >= 0 && newY < beachHeight && allowedTiles.Contains(mapData[newX, newY]))
                {
                    X = newX;
                    Y = newY;
                    moved = true;
                    break;
                }
            }

            if (!moved)
            {
                // Could not move away, move randomly
                MoveRandomly();
            }
            // Move to a random allowed tile away from the predator
            MoveRandomly();
        }
        // Return to beach and restore normal behavior
        isHunted = false;
        MoveToBeach();
    }
    private void CheckForPredatorsInRange()
    {
        // Check in a 10 tile radius for wolves
        for (int dx = -10; dx <= 10; dx++)
        {
            for (int dy = -10; dy <= 10; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < beachWidth && checkY >= 0 && checkY < beachHeight)
                {
                    // Check if there's a wolf ('W') at this position
                    if (overlayData[checkX, checkY] == 'W')
                    {
                        predatorNearby = true;
                        predatorX = checkX;
                        predatorY = checkY;
                        return;
                    }
                    else
                    {
                        predatorNearby = false;
                    }
                }
            }
        }
    }
    private void MoveToBeach()
    {
        // Scan in increasing radius for beach tiles
        int maxRadius = Math.Max(mapWidth, mapHeight);

        for (int radius = 1; radius < maxRadius; radius++)
        {
            // Check tiles in a square pattern around current position
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int checkX = X + dx;
                    int checkY = Y + dy;

                    // Check if coordinates are within bounds
                    if (checkX >= 0 && checkX < mapWidth &&
                        checkY >= 0 && checkY < mapHeight)
                    {
                        // Check if it's a beach tile
                        if (mapData[checkX, checkY] == 'B' || mapData[checkX, checkY] == 'b')
                        {
                            // Move towards this beach tile
                            WalkToBeach(checkX, checkY);
                            return;
                        }
                    }
                }
            }
        }
    }
    private void WalkToBeach(int targetX, int targetY)
    {
        // Check if we're not already at the target
        if (X == targetX && Y == targetY) return;

        // Calculate direction to move
        int deltaX = targetX - X;
        int deltaY = targetY - Y;

        // Create a list of possible moves, prioritizing diagonal movement
        List<(int dx, int dy)> possibleMoves = new List<(int dx, int dy)>();

        // Add diagonal moves first
        if (deltaX > 0 && deltaY > 0) possibleMoves.Add((1, 1));
        if (deltaX > 0 && deltaY < 0) possibleMoves.Add((1, -1));
        if (deltaX < 0 && deltaY > 0) possibleMoves.Add((-1, 1));
        if (deltaX < 0 && deltaY < 0) possibleMoves.Add((-1, -1));

        // Add cardinal moves
        if (deltaX > 0) possibleMoves.Add((1, 0));
        if (deltaX < 0) possibleMoves.Add((-1, 0));
        if (deltaY > 0) possibleMoves.Add((0, 1));
        if (deltaY < 0) possibleMoves.Add((0, -1));

        // Try each possible move in order of priority
        foreach (var move in possibleMoves)
        {
            int newX = X + move.dx;
            int newY = Y + move.dy;

            // Check if the new position is valid
            if (newX >= 0 && newX < beachWidth &&
                newY >= 0 && newY < beachHeight &&
                (allowedTiles.Contains(mapData[newX, newY]) || IsWaterTile(mapData[newX, newY])))
            {
                X = newX;
                Y = newY;
                break;
            }
        }
    }
    private (int, int) FindNearestBeachTile()
    {
        int nearestX = -1;
        int nearestY = -1;
        double nearestDistance = double.MaxValue;

        for (int x = 0; x < beachWidth; x++)
        {
            for (int y = 0; y < beachHeight; y++)
            {
                if (allowedTiles.Contains(mapData[x, y]))
                {
                    double distance = GetDistance(X, Y, x, y);
                    if (distance < nearestDistance)
                    {
                        nearestX = x;
                        nearestY = y;
                        nearestDistance = distance;
                    }
                }
            }
        }

        return (nearestX, nearestY);
    }
    private double GetDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }
}
#endregion
#region plains species
public class Sheep : Species
{
    private Random random;
    public int X { get; set; }
    public int Y { get; set; }
    private bool isAggressive;
    private bool predatorNearby;
    private bool isHunted;
    private char[,] mapData;
    private char[,] overlayData;
    private int predatorX;
    private int predatorY;
    private bool isNight;
    private double time;
    private double sunriseTime;
    private double sunsetTime;
    private int mapWidth;
    private int mapHeight;
    private List<char> allowedTiles = new List<char> {'P'}; // Add your selected tiles here
    public Sheep(int initialX, int initialY, char[,] mapData, char[,] overlayData)
        : base("Sheep", "Plains")
    {
        random = new Random();
        X = initialX;
        Y = initialY;
        this.mapData = mapData;
        this.overlayData = overlayData;
        this.mapWidth = mapData.GetLength(0);
        this.mapHeight = mapData.GetLength(1);
        isAggressive = random.NextDouble() > 0.005;
    }
    public void SetTime(double currentTime, double sunrise, double sunset)
    {
        time = currentTime;
        sunriseTime = sunrise;
        sunsetTime = sunset;
    }
    public override void Behave()
    {
        if (random.NextDouble() > 0.8 && !isHunted)
        {
            isNight = time > sunsetTime || time < sunriseTime;
        }
        if (random.NextDouble() > 0.7 && !isHunted && !isNight)
        {
            MoveInGroups();
            //MoveRandomly();
        }
        SearchForFood();
        if (isAggressive)
        {
            Attack();
        }
        CheckForPredatorsInRange();
        AvoidPredators();
    }
    private void Attack()
    {
        // Simulate attacking with a 10% chance
        bool attack = random.NextDouble() > 0.9;
        if (attack)
        {
            // Logic to attack nearby species
            // e.g., Map.Attack(X, Y);
        }
    }
    private void MoveInGroups()
    {
        // Find the nearest cow within 3 tiles
        (int x, int y) nearestCow = (X, Y);
        bool cowNearby = false;
        double minDistance = double.MaxValue;

        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dy = -3; dy <= 3; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < mapWidth && checkY >= 0 && checkY < mapHeight)
                {
                    // Check if there's a cow at this position
                    if (overlayData[checkX, checkY] == 'C' && !(checkX == X && checkY == Y))
                    {
                        double distance = GetDistance(X, Y, checkX, checkY);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestCow = (checkX, checkY);
                            cowNearby = true;
                        }
                    }
                }
            }
        }

        // If another cow is found and distance exceeds 3, move closer
        if (cowNearby)
        {
            if (minDistance > 3)
            {
                int deltaX = nearestCow.x - X;
                int deltaY = nearestCow.y - Y;

                // Determine move direction
                int moveX = deltaX > 0 ? 1 : deltaX < 0 ? -1 : 0;
                int moveY = deltaY > 0 ? 1 : deltaY < 0 ? -1 : 0;

                // Possible directions sorted by preference
                List<(int, int)> directions = new List<(int, int)>
                {
                    (moveX, moveY),     // Diagonal
                    (moveX, 0),         // Horizontal
                    (0, moveY)          // Vertical
                };

                bool moved = false;

                foreach (var dir in directions)
                {
                    int newX = X + dir.Item1;
                    int newY = Y + dir.Item2;
                    if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
                    {
                        X = newX;
                        Y = newY;
                        moved = true;
                        break;
                    }
                }

                if (!moved)
                {
                    MoveRandomly();
                }
            }
            else
            {
                // Cows are within 3 tiles, restrict random movement to stay within range
                List<(int, int)> possibleDirections = new List<(int, int)>
                {
                    (-1, 0), (1, 0), (0, -1), (0, 1),
                    (-1, -1), (-1, 1), (1, -1), (1, 1)
                };

                foreach (var dir in possibleDirections.OrderBy(x => Guid.NewGuid()))
                {
                    int newX = X + dir.Item1;
                    int newY = Y + dir.Item2;
                    if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
                    {
                        double distance = GetDistance(newX, newY, nearestCow.x, nearestCow.y);
                        if (distance <= 3)
                        {
                            X = newX;
                            Y = newY;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            MoveToTheNearestSheep();
        }
    }
    private void MoveToTheNearestSheep()
    {
        (int x, int y) nearestSheep = (X, Y);
        for (int dx = -20; dx <= 20; dx++)
        {
            for (int dy = -20; dy <= 20; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < mapWidth && checkY >= 0 && checkY < mapHeight)
                {
                    // Check if there's a cow at this position
                    if (overlayData[checkX, checkY] == 'S' && !(checkX == X && checkY == Y))
                    {
                        nearestSheep = (checkX, checkY);
                        break;
                    }
                }
            }
        }
        // Move towards the nearest cow
        int deltaX = nearestSheep.x - X;
        int deltaY = nearestSheep.y - Y;
        int moveX = deltaX > 0 ? 1 : (deltaX < 0 ? -1 : 0);
        int moveY = deltaY > 0 ? 1 : (deltaY < 0 ? -1 : 0);

        // Determine new position
        int newX = X + moveX;
        int newY = Y + moveY;

        // Check if the new position is within bounds and allowed
        if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
        {
            X = newX;
            Y = newY;
        }
    }
    private void MoveRandomly()
    {
        List<int> possibleDirections = new List<int>();

        // Check each direction and add to possibleDirections if the tile is allowed
        if (Y > 0 && allowedTiles.Contains(mapData[X, Y - 1])) possibleDirections.Add(0); // Up
        if (Y < mapHeight - 1 && allowedTiles.Contains(mapData[X, Y + 1])) possibleDirections.Add(1); // Down
        if (X > 0 && allowedTiles.Contains(mapData[X - 1, Y])) possibleDirections.Add(2); // Left
        if (X < mapWidth - 1 && allowedTiles.Contains(mapData[X + 1, Y])) possibleDirections.Add(3); // Right

        if (possibleDirections.Count > 0)
        {
            int direction = possibleDirections[random.Next(possibleDirections.Count)];
            switch (direction)
            {
                case 0:
                    Y -= 1; // Move up
                    break;
                case 1:
                    Y += 1; // Move down
                    break;
                case 2:
                    X -= 1; // Move left
                    break;
                case 3:
                    X += 1; // Move right
                    break;
            }
        }
    }
    private void SearchForFood()
    {
        // Simulate searching for food with a 30% chance
        bool foundFood = random.NextDouble() > 0.7;
        if (foundFood)
        {
            // Logic to consume food at (X, Y)
            // e.g., Map.ConsumeFood(X, Y);
        }
    }
    private void AvoidPredators()
    {
        // Simulate predator detection with a 20% chance
        if (predatorNearby)
        {
            isHunted = true;
            // Move away from predator

            // Assuming predatorX and predatorY are the predator's coordinates
            int deltaX = X - predatorX;
            int deltaY = Y - predatorY;

            // Determine move direction
            int moveX = deltaX > 0 ? 1 : deltaX < 0 ? -1 : 0;
            int moveY = deltaY > 0 ? 1 : deltaY < 0 ? -1 : 0;

            // Possible directions sorted by preference
            List<(int, int)> directions = new List<(int, int)>
            {
                (moveX, moveY),     // Diagonal away
                (moveX, 0),         // Horizontal away
                (0, moveY)          // Vertical away
            };

            bool moved = false;

            foreach (var dir in directions)
            {
                int newX = X + dir.Item1;
                int newY = Y + dir.Item2;
                if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
                {
                    X = newX;
                    Y = newY;
                    moved = true;
                    break;
                }
            }

            if (!moved)
            {
                // Could not move away, move randomly
                MoveRandomly();
            }
            // Move to a random allowed tile away from the predator
            MoveRandomly();
        }
        // Return to beach and restore normal behavior
        isHunted = false;
    }
    private void CheckForPredatorsInRange()
    {
        // Check in a 10 tile radius for wolves
        for (int dx = -10; dx <= 10; dx++)
        {
            for (int dy = -10; dy <= 10; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < mapWidth && checkY >= 0 && checkY < mapHeight)
                {
                    // Check if there's a wolf ('W') at this position
                    if (overlayData[checkX, checkY] == 'W')
                    {
                        predatorNearby = true;
                        predatorX = checkX;
                        predatorY = checkY;
                        return;
                    }
                    else
                    {
                        predatorNearby = false;
                    }
                }
            }
        }
    }
    private double GetDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }
}
public class Cow : Species
{
    private Random random;
    public int X { get; set; }
    public int Y { get; set; }
    private bool isAggressive;
    private bool predatorNearby;
    private bool isHunted;
    private char[,] mapData;
    private char[,] overlayData;
    private int predatorX;
    private int predatorY;
    private bool isNight;
    private double time;
    private double sunriseTime;
    private double sunsetTime;
    private int mapWidth;
    private int mapHeight;
    private List<char> allowedTiles = new List<char> {'P'}; // Add your selected tiles here
    public Cow(int initialX, int initialY, char[,] mapData, char[,] overlayData)
        : base("Cow", "Plains")
    {
        random = new Random();
        X = initialX;
        Y = initialY;
        this.mapData = mapData;
        this.overlayData = overlayData;
        this.mapWidth = mapData.GetLength(0);
        this.mapHeight = mapData.GetLength(1);
        isAggressive = random.NextDouble() > 0.005;
    }
    public void SetTime(double currentTime, double sunrise, double sunset)
    {
        time = currentTime;
        sunriseTime = sunrise;
        sunsetTime = sunset;
    }
    public override void Behave()
    {
        if (random.NextDouble() > 0.8 && !isHunted)
        {
            isNight = time > sunsetTime || time < sunriseTime;
        }
        if (random.NextDouble() > 0.7 && !isHunted && !isNight)
        {
            MoveInGroups();
            //MoveRandomly();
        }
        SearchForFood();
        if (isAggressive)
        {
            Attack();
        }
        CheckForPredatorsInRange();
        AvoidPredators();
    }
    private void Attack()
    {
        // Simulate attacking with a 10% chance
        bool attack = random.NextDouble() > 0.9;
        if (attack)
        {
            // Logic to attack nearby species
            // e.g., Map.Attack(X, Y);
        }
    }
    private void MoveInGroups()
    {
        // Find the nearest cow within 3 tiles
        (int x, int y) nearestCow = (X, Y);
        bool cowNearby = false;
        double minDistance = double.MaxValue;

        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dy = -3; dy <= 3; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < mapWidth && checkY >= 0 && checkY < mapHeight)
                {
                    // Check if there's a cow at this position
                    if (overlayData[checkX, checkY] == 'C' && !(checkX == X && checkY == Y))
                    {
                        double distance = GetDistance(X, Y, checkX, checkY);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestCow = (checkX, checkY);
                            cowNearby = true;
                        }
                    }
                }
            }
        }

        // If another cow is found and distance exceeds 3, move closer
        if (cowNearby)
        {
            if (minDistance > 3)
            {
                int deltaX = nearestCow.x - X;
                int deltaY = nearestCow.y - Y;

                // Determine move direction
                int moveX = deltaX > 0 ? 1 : deltaX < 0 ? -1 : 0;
                int moveY = deltaY > 0 ? 1 : deltaY < 0 ? -1 : 0;

                // Possible directions sorted by preference
                List<(int, int)> directions = new List<(int, int)>
                {
                    (moveX, moveY),     // Diagonal
                    (moveX, 0),         // Horizontal
                    (0, moveY)          // Vertical
                };

                bool moved = false;

                foreach (var dir in directions)
                {
                    int newX = X + dir.Item1;
                    int newY = Y + dir.Item2;
                    if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
                    {
                        X = newX;
                        Y = newY;
                        moved = true;
                        break;
                    }
                }

                if (!moved)
                {
                    MoveRandomly();
                }
            }
            else
            {
                // Cows are within 3 tiles, restrict random movement to stay within range
                List<(int, int)> possibleDirections = new List<(int, int)>
                {
                    (-1, 0), (1, 0), (0, -1), (0, 1),
                    (-1, -1), (-1, 1), (1, -1), (1, 1)
                };

                foreach (var dir in possibleDirections.OrderBy(x => Guid.NewGuid()))
                {
                    int newX = X + dir.Item1;
                    int newY = Y + dir.Item2;
                    if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
                    {
                        double distance = GetDistance(newX, newY, nearestCow.x, nearestCow.y);
                        if (distance <= 3)
                        {
                            X = newX;
                            Y = newY;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            MoveToTheNearestCow();
        }
    }
    private void MoveToTheNearestCow()
    {
        (int x, int y) nearestCow = (X, Y);
        for (int dx = -20; dx <= 20; dx++)
        {
            for (int dy = -20; dy <= 20; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < mapWidth && checkY >= 0 && checkY < mapHeight)
                {
                    // Check if there's a cow at this position
                    if (overlayData[checkX, checkY] == 'C' && !(checkX == X && checkY == Y))
                    {
                        nearestCow = (checkX, checkY);
                        break;
                    }
                }
            }
        }
        // Move towards the nearest cow
        int deltaX = nearestCow.x - X;
        int deltaY = nearestCow.y - Y;
        int moveX = deltaX > 0 ? 1 : (deltaX < 0 ? -1 : 0);
        int moveY = deltaY > 0 ? 1 : (deltaY < 0 ? -1 : 0);

        // Determine new position
        int newX = X + moveX;
        int newY = Y + moveY;

        // Check if the new position is within bounds and allowed
        if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
        {
            X = newX;
            Y = newY;
        }
    }
    private void MoveRandomly()
    {
        List<int> possibleDirections = new List<int>();

        // Check each direction and add to possibleDirections if the tile is allowed
        if (Y > 0 && allowedTiles.Contains(mapData[X, Y - 1])) possibleDirections.Add(0); // Up
        if (Y < mapHeight - 1 && allowedTiles.Contains(mapData[X, Y + 1])) possibleDirections.Add(1); // Down
        if (X > 0 && allowedTiles.Contains(mapData[X - 1, Y])) possibleDirections.Add(2); // Left
        if (X < mapWidth - 1 && allowedTiles.Contains(mapData[X + 1, Y])) possibleDirections.Add(3); // Right

        if (possibleDirections.Count > 0)
        {
            int direction = possibleDirections[random.Next(possibleDirections.Count)];
            switch (direction)
            {
                case 0:
                    Y -= 1; // Move up
                    break;
                case 1:
                    Y += 1; // Move down
                    break;
                case 2:
                    X -= 1; // Move left
                    break;
                case 3:
                    X += 1; // Move right
                    break;
            }
        }
    }
    private void SearchForFood()
    {
        // Simulate searching for food with a 30% chance
        bool foundFood = random.NextDouble() > 0.7;
        if (foundFood)
        {
            // Logic to consume food at (X, Y)
            // e.g., Map.ConsumeFood(X, Y);
        }
    }
    private void AvoidPredators()
    {
        // Simulate predator detection with a 20% chance
        if (predatorNearby)
        {
            isHunted = true;
            // Move away from predator

            // Assuming predatorX and predatorY are the predator's coordinates
            int deltaX = X - predatorX;
            int deltaY = Y - predatorY;

            // Determine move direction
            int moveX = deltaX > 0 ? 1 : deltaX < 0 ? -1 : 0;
            int moveY = deltaY > 0 ? 1 : deltaY < 0 ? -1 : 0;

            // Possible directions sorted by preference
            List<(int, int)> directions = new List<(int, int)>
            {
                (moveX, moveY),     // Diagonal away
                (moveX, 0),         // Horizontal away
                (0, moveY)          // Vertical away
            };

            bool moved = false;

            foreach (var dir in directions)
            {
                int newX = X + dir.Item1;
                int newY = Y + dir.Item2;
                if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight && allowedTiles.Contains(mapData[newX, newY]))
                {
                    X = newX;
                    Y = newY;
                    moved = true;
                    break;
                }
            }

            if (!moved)
            {
                // Could not move away, move randomly
                MoveRandomly();
            }
            // Move to a random allowed tile away from the predator
            MoveRandomly();
        }
        // Return to beach and restore normal behavior
        isHunted = false;
    }
    private void CheckForPredatorsInRange()
    {
        // Check in a 10 tile radius for wolves
        for (int dx = -10; dx <= 10; dx++)
        {
            for (int dy = -10; dy <= 10; dy++)
            {
                int checkX = X + dx;
                int checkY = Y + dy;

                // Ensure coordinates are within map bounds
                if (checkX >= 0 && checkX < mapWidth && checkY >= 0 && checkY < mapHeight)
                {
                    // Check if there's a wolf ('W') at this position
                    if (overlayData[checkX, checkY] == 'W')
                    {
                        predatorNearby = true;
                        predatorX = checkX;
                        predatorY = checkY;
                        return;
                    }
                    else
                    {
                        predatorNearby = false;
                    }
                }
            }
        }
    }
    private double GetDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }
}
#endregion
#region forest species
public class Bear : Species
{
    public Bear() : base("Bear", "Forest") { }

    public override void Behave()
    {
        // Implement bear behavior
    }
}

public class Wolf : Species
{
    public Wolf() : base("Wolf", "Forest") { }

    public override void Behave()
    {
        // Implement wolf behavior
    }
}
#endregion
#region mountain species
public class Goat : Species
{
    public Goat() : base("Goat", "Mountain") { }

    public override void Behave()
    {
        // Implement goat behavior
    }
}
#endregion
#region water species
public class Fish : Species
{
    public Fish() : base("Fish", "Water") { }

    public override void Behave()
    {
        // Implement fish behavior using Boids
    }
}
#endregion
#region air species
public class Bird : Species
{
    public Bird() : base("Bird", "Air") { }

    public override void Behave()
    {
        // Implement bird behavior using Boids
    }
}
#endregion
#endregion