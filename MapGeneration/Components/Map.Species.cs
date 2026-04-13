using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region update funcstions
    public List<Crab> crabs {get; set;} = new List<Crab>();
    public List<Turtle> turtles {get; set;} = new List<Turtle>();
    public List<Cow> cows {get; set;} = new List<Cow>();
    public List<Sheep> sheeps {get; set;} = new List<Sheep>();
    public void InitializeSpecies(int minSpecies, int maxSpecies, Species species)
    {
        List<TileId> allowedTiles = new List<TileId> { };

        if (species is Crab or Turtle)
        {
            allowedTiles.Add(TileId.Beach);
            allowedTiles.Add(TileId.BeachDark);
        }
        else if (species is Wolf or Bear)
        {
            allowedTiles.Add(TileId.Forest);
        }
        else if (species is Sheep or Cow)
        {
            allowedTiles.Add(TileId.Plains);
        }
        else if (species is Goat)
        {
            allowedTiles.Add(TileId.Mountain);
            allowedTiles.Add(TileId.MountainDeep);
        }
        else if (species is Fish)
        {
            allowedTiles.Add(TileId.Ocean);
            allowedTiles.Add(TileId.OceanShallow);
            allowedTiles.Add(TileId.Lake);
            allowedTiles.Add(TileId.LakeShallow);
            allowedTiles.Add(TileId.River);
            allowedTiles.Add(TileId.RiverShallow);
        }
        else if (species is Bird)
        {
            allowedTiles.Add(TileId.Forest);
            allowedTiles.Add(TileId.Plains);
            allowedTiles.Add(TileId.Beach);
            allowedTiles.Add(TileId.BeachDark);
            allowedTiles.Add(TileId.Ocean);
            allowedTiles.Add(TileId.OceanShallow);
            allowedTiles.Add(TileId.Lake);
            allowedTiles.Add(TileId.LakeShallow);
            allowedTiles.Add(TileId.River);
            allowedTiles.Add(TileId.RiverShallow);
            allowedTiles.Add(TileId.Mountain);
            allowedTiles.Add(TileId.MountainDeep);
            allowedTiles.Add(TileId.Snow);
        }

        int habitatTiles = 0;
        foreach (TileId tile in allowedTiles)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (mapData[x, y] == tile)
                    {
                        habitatTiles++;
                    }
                }
            }
        }
        if (habitatTiles < maxSpecies)
        {
            maxSpecies = (int)Math.Round((double)habitatTiles / rng.Next(1, 3));
            minSpecies = 0;
        }
        int numberOfSpecies = 0;
        if (habitatTiles > 0) numberOfSpecies = rng.Next(minSpecies, maxSpecies);
        if (species is Crab)
        {
            for (int i = 0; i < numberOfSpecies; i++)
            {
                try
                {
                    (int x, int y) = GetRandomPointInAllowedTiles(allowedTiles);
                    Crab crab = new Crab(x, y, rng.Next());
                    crabs.Add(crab);
                    overlayData[x, y] = EntityId.Crab;
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no beach biomes are available
                    outputBuffer.Add("No beach biomes available to place crabs.");
                    break;
                }
            }
        }
        else if (species is Turtle)
        {
            for (int i = 0; i < numberOfSpecies; i++)
            {
                try
                {
                    (int x, int y) = GetRandomPointInAllowedTiles(allowedTiles);
                    Turtle turtle = new Turtle(x, y, rng.Next());
                    turtles.Add(turtle);
                    overlayData[x, y] = EntityId.Turtle;
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no beach biomes are available
                    outputBuffer.Add("No beach biomes available to place turtles.");
                    break;
                }
            }
        }
    }
    private void InitializeCows(int minGroups, int maxGroups, int minPerGroup, int maxPerGroup)
    {
        int numberOfGroups = rng.Next(minGroups, maxGroups);
        for (int i = 0; i < numberOfGroups; i++)
        {
            int numberOfCows = rng.Next(minPerGroup, maxPerGroup);
            bool validPointFound = false;
            (int startX, int startY) = (0, 0);
            int attempts = 0;

            while (!validPointFound && attempts < 100)
            {
                (startX, startY) = GetRandomPointInAllowedTiles(new List<TileId> { TileId.Plains });
                if (IsAtLeastDistanceFromMountains(startX, startY, 5))
                {
                    validPointFound = true;
                }
                attempts++;
            }

            if (!validPointFound)
            {
                outputBuffer.Add("Failed to find a valid starting point for cow group.");
                continue;
            }

            for (int j = 0; j < numberOfCows; j++)
            {
                try
                {
                    int iterations = 0;
                    (int x, int y) = (0, 0);
                    bool placedCows = false;
                    while (!placedCows && iterations < 100)
                    {
                        (x, y) = GetRandomPointInRange(startX, startY, 1, 3);
                        if (mapData[x, y] == TileId.Plains)
                        {
                            Cow cow = new Cow(x, y, rng.Next());
                            cows.Add(cow);
                            overlayData[x, y] = EntityId.Cow;
                            placedCows = true;
                        }
                        iterations++;
                    }
                    if (iterations >= 100)
                    {
                        outputBuffer.Add("Failed to place cows.");
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no plains biomes are available
                    outputBuffer.Add("No plains biomes available to place cows.");
                    break;
                }
            }
        }
    }
    private void InitializeSheeps(int minGroups, int maxGroups, int minPerGroup, int maxPerGroup)
    {
        int numberOfGroups = rng.Next(minGroups, maxGroups);
        for (int i = 0; i < numberOfGroups; i++)
        {
            int numberOfSheeps = rng.Next(minPerGroup, maxPerGroup);
            bool validPointFound = false;
            (int startX, int startY) = (0, 0);
            int attempts = 0;

            while (!validPointFound && attempts < 100)
            {
                (startX, startY) = GetRandomPointInAllowedTiles(new List<TileId> { TileId.Plains });
                if (IsAtLeastDistanceFromMountains(startX, startY, 5))
                {
                    validPointFound = true;
                }
                attempts++;
            }

            if (!validPointFound)
            {
                outputBuffer.Add("Failed to find a valid starting point for sheep group.");
                continue;
            }

            for (int j = 0; j < numberOfSheeps; j++)
            {
                try
                {
                    int iterations = 0;
                    (int x, int y) = (0, 0);
                    bool placedCows = false;
                    while (!placedCows && iterations < 100)
                    {
                        (x, y) = GetRandomPointInRange(startX, startY, 1, 3);
                        if (mapData[x, y] == TileId.Plains)
                        {
                            Sheep sheep = new Sheep(x, y, rng.Next());
                            sheeps.Add(sheep);
                            overlayData[x, y] = EntityId.Sheep;
                            placedCows = true;
                        }
                        iterations++;
                    }
                    if (iterations >= 100)
                    {
                        outputBuffer.Add("Failed to place sheeps.");
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no plains biomes are available
                    outputBuffer.Add("No plains biomes available to place sheeps.");
                    break;
                }
            }
        }
    }
    private bool IsAtLeastDistanceFromMountains(int x, int y, int minDistance)
    {
        for (int dx = -minDistance; dx <= minDistance; dx++)
        {
            for (int dy = -minDistance; dy <= minDistance; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;
                if (checkX >= 0 && checkX < conf.Width && checkY >= 0 && checkY < conf.Height)
                {
                    if (mapData[checkX, checkY] == TileId.Mountain || mapData[checkX, checkY] == TileId.MountainDeep)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    private (int x, int y) GetRandomPointInAllowedTiles(List<TileId> allowedTiles)
    {
        int maxAttempts = 1000;
        int attempts = 0;
        int x, y;
        do
        {
            x = rng.Next(0, width);
            y = rng.Next(0, height);
            attempts++;
        } while (!allowedTiles.Contains(mapData[x, y]) && attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            outputBuffer.Add("Failed to find a valid point.");
        }

        return (x, y);
    }
    public void UpdateCrabs()
    {
        foreach (Crab crab in crabs)
        {
            // Store old position
            int oldX = crab.X;
            int oldY = crab.Y;

            // Update crab behavior
            crab.Behave(mapData, overlayData);

            // Check if new position is already occupied
            if (overlayData[crab.X, crab.Y] != EntityId.None)
            {
                // Assign crab back to old position
                crab.X = oldX;
                crab.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = EntityId.None;

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(crab.X, crab.Y))
                {
                    overlayData[crab.X, crab.Y] = EntityId.Crab;
                }
            }
        }
    }
    public void UpdateTurtles()
    {
        foreach (Turtle turtle in turtles)
        {
            // Store old position
            int oldX = turtle.X;
            int oldY = turtle.Y;

            // Update turtle behavior
            turtle.Behave(mapData, overlayData);

            // Check if new position is already occupied
            if (overlayData[turtle.X, turtle.Y] != EntityId.None)
            {
                // Assign turtle back to old position
                turtle.X = oldX;
                turtle.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = EntityId.None;

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(turtle.X, turtle.Y))
                {
                    overlayData[turtle.X, turtle.Y] = EntityId.Turtle;
                }
            }
        }
    }
    public void UpdateCows()
    {
        foreach (Cow cow in cows)
        {
            // Set time values before behaving
            cow.SetTime(weather.TimeOfDay, sunriseTime, sunsetTime);
            
            // Store old position
            int oldX = cow.X;
            int oldY = cow.Y;

            // Update cow behavior
            cow.Behave(mapData, overlayData);

            // Check if new position is already occupied
            if (overlayData[cow.X, cow.Y] != EntityId.None)
            {
                // Assign cow back to old position
                cow.X = oldX;
                cow.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = EntityId.None;

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(cow.X, cow.Y))
                {
                    overlayData[cow.X, cow.Y] = EntityId.Cow;
                }
            }
        }
    }
    public void UpdateSheeps()
    {
        foreach (Sheep sheep in sheeps)
        {
            // Set time values before behaving
            sheep.SetTime(weather.TimeOfDay, sunriseTime, sunsetTime);
            
            // Store old position
            int oldX = sheep.X;
            int oldY = sheep.Y;

            // Update sheep behavior
            sheep.Behave(mapData, overlayData);

            // Check if new position is occupied
            if (overlayData[sheep.X, sheep.Y] != EntityId.None)
            {
                // Assign sheep back to old position
                sheep.X = oldX;
                sheep.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = EntityId.None;

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(sheep.X, sheep.Y))
                {
                    overlayData[sheep.X, sheep.Y] = EntityId.Sheep;
                }
            }
        }
    }
    #endregion
}
