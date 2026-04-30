using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region update funcstions
    public List<Crab> crabs {get; set;} = [];
    public List<Turtle> turtles {get; set;} = [];
    public List<Cow> cows {get; set;} = [];
    public List<Sheep> sheeps {get; set;} = [];

    private Action<SunriseEvent>? _onSunrise;
    private Action<SunsetEvent>? _onSunset;

    public void SubscribeSpeciesEvents()
    {
        if (_onSunrise != null) EventBus.Unsubscribe(_onSunrise);
        if (_onSunset  != null) EventBus.Unsubscribe(_onSunset);
        _onSunrise = _ => SyncNightState(false);
        _onSunset  = _ => SyncNightState(true);
        EventBus.Subscribe(_onSunrise);
        EventBus.Subscribe(_onSunset);
        SyncNightState(dayNight.TimeOfDay < dayNight.SunriseTime || dayNight.TimeOfDay > dayNight.SunsetTime);
    }

    private void SyncNightState(bool isNight)
    {
        double rise = dayNight.SunriseTime, set = dayNight.SunsetTime;
        foreach (Species e in crabs.Cast<Species>().Concat(turtles).Concat(cows).Concat(sheeps))
        {
            e.isNight = isNight;
            e.sunriseTime = rise;
            e.sunsetTime = set;
        }
    }

    public void InitializeSpecies(int minSpecies, int maxSpecies, Species species)
    {
        var allowedTiles = species.allowedTiles;

        int habitatTiles = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (allowedTiles.Contains(mapData[x, y])) habitatTiles++;

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
                    Crab crab = new(x, y, rng.Next());
                    crabs.Add(crab);
                    overlayData[x, y] = EntityId.Crab;
                }
                catch (InvalidOperationException)
                {
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
                    Turtle turtle = new(x, y, rng.Next());
                    turtles.Add(turtle);
                    overlayData[x, y] = EntityId.Turtle;
                }
                catch (InvalidOperationException)
                {
                    outputBuffer.Add("No beach biomes available to place turtles.");
                    break;
                }
            }
        }
    }

    private void InitializeHerd<T>(int minGroups, int maxGroups, int minPerGroup, int maxPerGroup,
        List<T> list, Func<int, int, int, T> factory) where T : Species
    {
        int numberOfGroups = rng.Next(minGroups, maxGroups);
        for (int i = 0; i < numberOfGroups; i++)
        {
            int numberOfAnimals = rng.Next(minPerGroup, maxPerGroup);
            bool validPointFound = false;
            (int startX, int startY) = (0, 0);
            int attempts = 0;

            while (!validPointFound && attempts < 100)
            {
                (startX, startY) = GetRandomPointInAllowedTiles(new HashSet<TileId> { TileId.Plains });
                if (IsAtLeastDistanceFromMountains(startX, startY, 5)) validPointFound = true;
                attempts++;
            }

            if (!validPointFound)
            {
                outputBuffer.Add($"Failed to find a valid starting point for {typeof(T).Name} group.");
                continue;
            }

            for (int j = 0; j < numberOfAnimals; j++)
            {
                try
                {
                    int iterations = 0;
                    (int x, int y) = (0, 0);
                    bool placed = false;
                    while (!placed && iterations < 100)
                    {
                        (x, y) = GetRandomPointInRange(startX, startY, 1, 3);
                        if (mapData[x, y] == TileId.Plains)
                        {
                            T animal = factory(x, y, rng.Next());
                            list.Add(animal);
                            overlayData[x, y] = animal.EntityId;
                            placed = true;
                        }
                        iterations++;
                    }
                    if (iterations >= 100)
                    {
                        outputBuffer.Add($"Failed to place {typeof(T).Name}.");
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    outputBuffer.Add($"No plains biomes available to place {typeof(T).Name}.");
                    break;
                }
            }
        }
    }

    private void UpdateSpecies<T>(List<T> species) where T : Species
    {
        foreach (T animal in species)
        {
            int oldX = animal.X, oldY = animal.Y;
            animal.Behave(mapData, overlayData);
            if (overlayData[animal.X, animal.Y] != EntityId.None)
            {
                animal.X = oldX;
                animal.Y = oldY;
            }
            else
            {
                overlayData[oldX, oldY] = EntityId.None;
                if (!IsTileUnderCloud(animal.X, animal.Y)) overlayData[animal.X, animal.Y] = animal.EntityId;
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
                    if (mapData[checkX, checkY] == TileId.Mountain || mapData[checkX, checkY] == TileId.MountainDeep) return false;
                }
            }
        }
        return true;
    }

    private (int x, int y) GetRandomPointInAllowedTiles(HashSet<TileId> allowedTiles)
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

        if (attempts >= maxAttempts) outputBuffer.Add("Failed to find a valid point.");

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
                if (!IsTileUnderCloud(crab.X, crab.Y)) overlayData[crab.X, crab.Y] = EntityId.Crab;
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
                if (!IsTileUnderCloud(turtle.X, turtle.Y)) overlayData[turtle.X, turtle.Y] = EntityId.Turtle;
            }
        }
    }
    public void UpdateCows()
    {
        foreach (Cow cow in cows)
        {
            // Set time values before behaving
            cow.SetTime(dayNight.TimeOfDay, dayNight.SunriseTime, dayNight.SunsetTime);

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
                if (!IsTileUnderCloud(cow.X, cow.Y)) overlayData[cow.X, cow.Y] = EntityId.Cow;
            }
        }
    }
    public void UpdateSheeps()
    {
        foreach (Sheep sheep in sheeps)
        {
            // Set time values before behaving
            sheep.SetTime(dayNight.TimeOfDay, dayNight.SunriseTime, dayNight.SunsetTime);

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
                if (!IsTileUnderCloud(sheep.X, sheep.Y)) overlayData[sheep.X, sheep.Y] = EntityId.Sheep;
            }
        }
    }

    private void InitializeCows(int minGroups, int maxGroups, int minPerGroup, int maxPerGroup)
    {
        InitializeHerd(minGroups, maxGroups, minPerGroup, maxPerGroup, cows, (x, y, seed) => new Cow(x, y, seed));
    }
    private void InitializeSheeps(int minGroups, int maxGroups, int minPerGroup, int maxPerGroup)
    {
        InitializeHerd(minGroups, maxGroups, minPerGroup, maxPerGroup, sheeps, (x, y, seed) => new Sheep(x, y, seed));
    }
    #endregion
}
