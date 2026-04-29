using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A* Pathfinding with terrain costs, path styling, and obstacle avoidance.
/// 
/// NEW ORGANIC PATH FEATURES:
/// - RequireCardinalConnectivity: Only allow N/S/E/W movement (no diagonals) for connected-looking paths
/// - BiomeChangePenalty: Penalty when transitioning between terrain types
/// - BiomeStickiness: Preference to stay in same biome type
/// - AvoidBacktracking: Prevent paths from moving away from goal
/// - TerrainTransitionSmoothness: Smooth transitions between different terrain costs
/// 
/// OBSTACLE AVOIDANCE:
/// - EnableObstacleAvoidance: Enable/disable obstacle avoidance
/// - HighCostThreshold: Terrain cost to be considered obstacle (default: 3.0)
/// - AvoidanceDistance: Distance in tiles to maintain from obstacles (default: 3.5)
/// - AvoidancePenaltyMultiplier: Strength of avoidance (default: 1.8)
/// </summary>
public class AStar
{
    public class PathNode
    {
        public int X { get; set; }
        public int Y { get; set; }
        public float GCost { get; set; }
        public float HCost { get; set; }
        public float FCost => GCost + HCost;
        public PathNode? Parent { get; set; }
        public TileId TerrainType { get; set; }

        public PathNode(int x, int y, TileId terrainType)
        {
            X = x;
            Y = y;
            TerrainType = terrainType;
            GCost = 0;
            HCost = 0;
            Parent = null;
        }
    }

    public class TerrainConfig
    {
        private Dictionary<TileId, float> _movementCosts;
        private HashSet<TileId> _blockedTerrain;

        // Path Shape & Style
        public float PathRandomness { get; set; } = 0.2f;
        public float DirectionChangePenalty { get; set; } = 0.0f;
        public float DiagonalPenalty { get; set; } = 0.8f;
        public float WanderingBias { get; set; } = 2.8f;
        public float WaveAmplitude { get; set; } = 0.7f;
        public float WaveFrequency { get; set; } = 0.6f;
        public float OrganicNoise { get; set; } = 1.8f;
        public float StraightLineAvoidance { get; set; } = 1.7f;
        public float DirectionInertia { get; set; } = 0.2f;
        public int RandomSeed { get; set; } = 0;
        public float HighCostThreshold { get; set; } = 3.0f;
        public float AvoidanceDistance { get; set; } = 2.0f;
        public float AvoidancePenaltyMultiplier { get; set; } = 2.0f;
        public bool EnableObstacleAvoidance { get; set; } = false;
        public bool RequireCardinalConnectivity { get; set; } = true;
        public float BiomeChangePenalty { get; set; } = 0.5f;
        public float BiomeStickiness { get; set; } = 0.8f;
        public bool PreferWidthExpansion { get; set; } = true;
        public float WidthExpansionRadius { get; set; } = 5.0f;
        public float TerrainTransitionSmoothness { get; set; } = 0.5f;
        public bool AvoidBacktracking { get; set; } = true;
        public float BacktrackingPenalty { get; set; } = 5.0f;

        public TerrainConfig()
        {
            _movementCosts = new Dictionary<TileId, float>();
            _blockedTerrain = new HashSet<TileId>();
            // Default costs come from TileRegistry; add overrides here if needed.
            BlockTerrain(TileId.Border);
        }

        public void SetTerrainCost(TileId terrain, float cost) => _movementCosts[terrain] = cost;
        public void BlockTerrain(TileId terrain) => _blockedTerrain.Add(terrain);
        public void UnblockTerrain(TileId terrain) => _blockedTerrain.Remove(terrain);
        public float GetTerrainCost(TileId terrain)
        {
            if (_movementCosts.TryGetValue(terrain, out float cost)) return cost;
            return TileRegistry.IsValid(terrain) ? TileRegistry.Get(terrain).MovementCost : 1.0f;
        }
        public bool IsTerrainBlocked(TileId terrain) => _blockedTerrain.Contains(terrain) || GetTerrainCost(terrain) >= float.MaxValue;
        public bool IsHighCostTerrain(TileId terrain) => GetTerrainCost(terrain) >= HighCostThreshold;
    }

    private TerrainConfig _terrainConfig;
    private Random _random;
    private Dictionary<(int, int), float>? _obstacleDistanceMap;

    public AStar()
    {
        _terrainConfig = new TerrainConfig();
        _random = new Random();
    }

    public AStar(TerrainConfig customConfig)
    {
        _terrainConfig = customConfig;
        _random = customConfig.RandomSeed != 0 ? new Random(customConfig.RandomSeed) : new Random();
    }

    public List<(int x, int y)>? FindPath(TileId[,] mapData, int startX, int startY, int goalX, int goalY)
    {
        int width = mapData.GetLength(0);
        int height = mapData.GetLength(1);

        if (!IsValidCoordinate(startX, startY, width, height) || !IsValidCoordinate(goalX, goalY, width, height)) return null;

        if (_terrainConfig.IsTerrainBlocked(mapData[startX, startY]) || _terrainConfig.IsTerrainBlocked(mapData[goalX, goalY])) return null;

        // Pre-compute obstacle distance map for avoidance
        if (_terrainConfig.EnableObstacleAvoidance) _obstacleDistanceMap = ComputeObstacleDistanceMap(mapData, width, height);

        PathNode[,] nodeGrid = new PathNode[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                nodeGrid[x, y] = new PathNode(x, y, mapData[x, y]);

        List<PathNode> openList = [];
        HashSet<PathNode> closedSet = [];
        PathNode startNode = nodeGrid[startX, startY];
        PathNode goalNode = nodeGrid[goalX, goalY];
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
                if (openList[i].FCost < currentNode.FCost || (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost)) currentNode = openList[i];

            openList.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == goalNode) return RetracePath(startNode, goalNode);

            foreach (PathNode neighbor in GetNeighbors(currentNode, nodeGrid, width, height))
            {
                if (_terrainConfig.IsTerrainBlocked(neighbor.TerrainType) || closedSet.Contains(neighbor)) continue;

                bool isDiagonal = (currentNode.X != neighbor.X) && (currentNode.Y != neighbor.Y);
                
                // Skip diagonals if cardinal connectivity required
                if (isDiagonal && _terrainConfig.RequireCardinalConnectivity) continue;

                float moveCost = isDiagonal ? 1.414f : 1.0f;
                
                if (isDiagonal && _terrainConfig.DiagonalPenalty > 1.0f) moveCost *= _terrainConfig.DiagonalPenalty;
                
                float terrainCost = _terrainConfig.GetTerrainCost(neighbor.TerrainType);
                float randomnessFactor = 1.0f + (_terrainConfig.PathRandomness > 0 ? 
                    ((float)_random.NextDouble() * 2.0f - 1.0f) * _terrainConfig.PathRandomness : 0f);
                
                float totalPenalty = CalculatePathPenalties(currentNode, neighbor, startNode, goalNode, nodeGrid);
                float obstaclePenalty = CalculateObstacleAvoidancePenalty(neighbor);
                
                float newMovementCostToNeighbor = currentNode.GCost + 
                    (moveCost * terrainCost * randomnessFactor) + 
                    totalPenalty +
                    obstaclePenalty;

                if (newMovementCostToNeighbor < neighbor.GCost || !openList.Contains(neighbor))
                {
                    neighbor.GCost = newMovementCostToNeighbor;
                    neighbor.HCost = GetDistance(neighbor, goalNode);
                    neighbor.Parent = currentNode;

                    if (!openList.Contains(neighbor)) openList.Add(neighbor);
                }
            }
        }

        // No path found
        return null;
    }

    /// <summary>
    /// Get all valid neighboring nodes (8-directional movement - includes diagonals)
    /// </summary>
    private List<PathNode> GetNeighbors(PathNode node, PathNode[,] nodeGrid, int width, int height)
    {
        List<PathNode> neighbors = [];

        // Check 8 directions: up, down, left, right, and 4 diagonals
        // Order: N, S, W, E, NW, NE, SW, SE
        int[] dx = { 0, 0, -1, 1, -1, 1, -1, 1 };
        int[] dy = { -1, 1, 0, 0, -1, -1, 1, 1 };

        for (int i = 0; i < 8; i++)
        {
            int checkX = node.X + dx[i];
            int checkY = node.Y + dy[i];

            if (!IsValidCoordinate(checkX, checkY, width, height)) continue;

            PathNode neighbor = nodeGrid[checkX, checkY];

            // Skip if the neighbor itself is blocked
            if (_terrainConfig.IsTerrainBlocked(neighbor.TerrainType)) continue;

            // For diagonal moves (indices 4-7), check if adjacent tiles are passable
            // This prevents "cutting corners" through impassable terrain
            if (i >= 4)
            {
                int adjacentX1 = node.X + dx[i];
                int adjacentY1 = node.Y;
                int adjacentX2 = node.X;
                int adjacentY2 = node.Y + dy[i];

                if (_terrainConfig.IsTerrainBlocked(nodeGrid[adjacentX1, adjacentY1].TerrainType) ||
                    _terrainConfig.IsTerrainBlocked(nodeGrid[adjacentX2, adjacentY2].TerrainType)) continue;
            }

            neighbors.Add(neighbor);
        }

        return neighbors;
    }

    private Dictionary<(int, int), float> ComputeObstacleDistanceMap(TileId[,] mapData, int width, int height)
    {
        var distanceMap = new Dictionary<(int, int), float>();
        var queue = new Queue<(int x, int y, float dist)>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_terrainConfig.IsHighCostTerrain(mapData[x, y]))
                {
                    queue.Enqueue((x, y, 0f));
                    distanceMap[(x, y)] = 0f;
                }
            }
        }

        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };

        while (queue.Count > 0)
        {
            var (x, y, dist) = queue.Dequeue();
            
            if (dist >= _terrainConfig.AvoidanceDistance) continue;

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                
                if (IsValidCoordinate(nx, ny, width, height))
                {
                    float newDist = dist + 1f;
                    if (!distanceMap.ContainsKey((nx, ny)) || distanceMap[(nx, ny)] > newDist)
                    {
                        distanceMap[(nx, ny)] = newDist;
                        queue.Enqueue((nx, ny, newDist));
                    }
                }
            }
        }

        return distanceMap;
    }

    private float CalculateObstacleAvoidancePenalty(PathNode node)
    {
        if (!_terrainConfig.EnableObstacleAvoidance || _obstacleDistanceMap == null) return 0f;

        if (_obstacleDistanceMap.TryGetValue((node.X, node.Y), out float distance))
        {
            // If within avoidance distance, apply exponential penalty
            if (distance < _terrainConfig.AvoidanceDistance)
            {
                // Normalize distance (0 = at obstacle, 1 = at avoidance edge)
                float normalizedDistance = distance / _terrainConfig.AvoidanceDistance;
                
                // Exponential falloff: closer to obstacle = much higher penalty
                // Use quadratic falloff for strong but smooth penalty
                float proximityFactor = 1.0f - normalizedDistance;
                float exponentialPenalty = proximityFactor * proximityFactor;
                
                // Scale by multiplier and base terrain cost to make it significant
                float basePenalty = _terrainConfig.AvoidancePenaltyMultiplier * 10.0f;
                return exponentialPenalty * basePenalty;
            }
        }

        return 0f;
    }

    private float CalculatePathPenalties(PathNode currentNode, PathNode neighbor, PathNode startNode, PathNode goalNode, PathNode[,] nodeGrid)
    {
        float totalPenalty = 0f;

        // Direction change penalty
        if (_terrainConfig.DirectionChangePenalty > 0 && currentNode.Parent != null)
        {
            int prevDx = currentNode.X - currentNode.Parent.X;
            int prevDy = currentNode.Y - currentNode.Parent.Y;
            int currDx = neighbor.X - currentNode.X;
            int currDy = neighbor.Y - currentNode.Y;
            
            if (prevDx != currDx || prevDy != currDy) totalPenalty += _terrainConfig.DirectionChangePenalty;
        }

        // NEW: Biome change penalty
        if (_terrainConfig.BiomeChangePenalty > 0 && currentNode.TerrainType != neighbor.TerrainType) totalPenalty += _terrainConfig.BiomeChangePenalty;

        // NEW: Biome stickiness (prefer staying in same biome, reward being surrounded by low-cost terrain)
        if (_terrainConfig.BiomeStickiness > 0)
        {
            TileId neighborBiome = neighbor.TerrainType;
            int sameBiomeNeighbors = CountNeighborsOfType(neighbor, neighborBiome, nodeGrid);
            
            // If surrounded by same biome (3-4 neighbors), calculate reward/penalty based on terrain cost
            if (sameBiomeNeighbors >= 3)
            {
                float terrainCost = _terrainConfig.GetTerrainCost(neighborBiome);
                // Reward low-cost biomes (negative penalty), penalize high-cost ones
                // Cost 1.0 = -0.4 reward, Cost 3.0 = +0.4 penalty
                float surroundedModifier = (terrainCost - 2.0f) * 0.2f * _terrainConfig.BiomeStickiness;
                totalPenalty += surroundedModifier;
            }
            
            // Additional penalty for changing biomes frequently
            if (currentNode.Parent != null && neighbor.TerrainType != currentNode.TerrainType) totalPenalty += _terrainConfig.BiomeStickiness * 0.5f;
        }

        // NEW: Backtracking avoidance
        if (_terrainConfig.AvoidBacktracking && currentNode.Parent != null && currentNode.Parent.Parent != null)
        {
            float distToGoal = GetDistance(neighbor, goalNode);
            float parentDistToGoal = GetDistance(currentNode.Parent, goalNode);
            if (distToGoal > parentDistToGoal) totalPenalty += _terrainConfig.BacktrackingPenalty;
        }

        // Wandering bias
        if (_terrainConfig.WanderingBias > 0)
        {
            float lineDistance = DistanceFromLine(startNode, goalNode, neighbor);
            totalPenalty -= lineDistance * _terrainConfig.WanderingBias * 0.1f;
        }

        // Wave pattern
        if (_terrainConfig.WaveAmplitude > 0)
        {
            float totalDistance = GetDistance(startNode, goalNode);
            float currentDistance = GetDistance(startNode, neighbor);
            float progress = totalDistance > 0 ? currentDistance / totalDistance : 0;
            float wavePhase = progress * _terrainConfig.WaveFrequency * (float)Math.PI * 2.0f;
            float waveOffset = (float)Math.Sin(wavePhase) * _terrainConfig.WaveAmplitude;
            float perpendicularDistance = DistanceFromLine(startNode, goalNode, neighbor);
            float distanceFromIdeal = Math.Abs(perpendicularDistance - waveOffset);
            totalPenalty -= Math.Max(0, _terrainConfig.WaveAmplitude - distanceFromIdeal) * 0.15f;
        }

        // Organic noise
        if (_terrainConfig.OrganicNoise > 0)
        {
            float noise = GeneratePositionNoise(neighbor.X, neighbor.Y) * _terrainConfig.OrganicNoise;
            totalPenalty += noise * 0.5f;
        }

        // Straight line avoidance
        if (_terrainConfig.StraightLineAvoidance > 0 && currentNode.Parent != null && currentNode.Parent.Parent != null)
        {
            int dx1 = currentNode.Parent.X - currentNode.Parent.Parent.X;
            int dy1 = currentNode.Parent.Y - currentNode.Parent.Parent.Y;
            int dx2 = currentNode.X - currentNode.Parent.X;
            int dy2 = currentNode.Y - currentNode.Parent.Y;
            int dx3 = neighbor.X - currentNode.X;
            int dy3 = neighbor.Y - currentNode.Y;
            
            if (dx1 == dx2 && dy1 == dy2 && dx2 == dx3 && dy2 == dy3) totalPenalty += _terrainConfig.StraightLineAvoidance * 0.5f;
        }

        // Direction inertia
        if (_terrainConfig.DirectionInertia > 0 && currentNode.Parent != null)
        {
            int prevDx = currentNode.X - currentNode.Parent.X;
            int prevDy = currentNode.Y - currentNode.Parent.Y;
            int currDx = neighbor.X - currentNode.X;
            int currDy = neighbor.Y - currentNode.Y;
            
            float directionSimilarity = (prevDx * currDx + prevDy * currDy) / 
                (float)(Math.Sqrt(prevDx * prevDx + prevDy * prevDy) * Math.Sqrt(currDx * currDx + currDy * currDy) + 0.001f);
            
            totalPenalty -= directionSimilarity * _terrainConfig.DirectionInertia * 0.3f;
        }

        // NEW: Terrain transition smoothness
        if (_terrainConfig.TerrainTransitionSmoothness > 0 && currentNode.Parent != null)
        {
            float transitionPenalty = CalculateTransitionPenalty(currentNode, neighbor, nodeGrid);
            totalPenalty += transitionPenalty * _terrainConfig.TerrainTransitionSmoothness;
        }

        return totalPenalty;
    }

    private int CountNeighborsOfType(PathNode node, TileId terrainType, PathNode[,] nodeGrid)
    {
        int count = 0;
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };
        
        for (int i = 0; i < 4; i++)
        {
            int nx = node.X + dx[i];
            int ny = node.Y + dy[i];
            
            if (IsValidCoordinate(nx, ny, nodeGrid.GetLength(0), nodeGrid.GetLength(1)) && nodeGrid[nx, ny].TerrainType == terrainType) count++;
        }
        
        return count;
    }

    private float CalculateTransitionPenalty(PathNode currentNode, PathNode neighbor, PathNode[,] nodeGrid)
    {
        if (currentNode.TerrainType == neighbor.TerrainType) return 0f;

        float costDiff = Math.Abs(_terrainConfig.GetTerrainCost(currentNode.TerrainType) - 
                                    _terrainConfig.GetTerrainCost(neighbor.TerrainType));
        
        return costDiff * 0.5f;
    }

    private float GetDistance(PathNode nodeA, PathNode nodeB)
    {
        int distanceX = Math.Abs(nodeA.X - nodeB.X);
        int distanceY = Math.Abs(nodeA.Y - nodeB.Y);
        int straight = Math.Abs(distanceX - distanceY);
        int diagonal = Math.Min(distanceX, distanceY);
        return straight + 1.414f * diagonal;
    }

    private float DistanceFromLine(PathNode lineStart, PathNode lineEnd, PathNode point)
    {
        float dx = lineEnd.X - lineStart.X;
        float dy = lineEnd.Y - lineStart.Y;
        
        if (dx == 0 && dy == 0) return GetDistance(point, lineStart);
        
        float numerator = Math.Abs(dy * point.X - dx * point.Y + lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X);
        float denominator = (float)Math.Sqrt(dx * dx + dy * dy);
        return numerator / denominator;
    }

    private float GeneratePositionNoise(int x, int y)
    {
        int hash = x * 374761393 + y * 668265263;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash = hash ^ (hash >> 16);
        return ((hash & 0x7FFFFFFF) / (float)0x7FFFFFFF) * 2.0f - 1.0f;
    }

    private List<(int x, int y)> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<(int x, int y)> path = [];
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add((currentNode.X, currentNode.Y));
            currentNode = currentNode.Parent!;
        }

        path.Add((startNode.X, startNode.Y));
        path.Reverse();
        return path;
    }

    private bool IsValidCoordinate(int x, int y, int width, int height) => x >= 0 && x < width && y >= 0 && y < height;

    public TerrainConfig GetTerrainConfig() => _terrainConfig;

    public static TerrainConfig CreateAnimalConfig(string animalType)
    {
        TerrainConfig config = new TerrainConfig();

        switch (animalType.ToLower())
        {
            case "bird":
                config.SetTerrainCost(TileId.Plains, 1.0f);
                config.SetTerrainCost(TileId.Forest, 1.0f);
                config.SetTerrainCost(TileId.Mountain, 1.2f);
                config.SetTerrainCost(TileId.Ocean, 1.0f);
                config.DirectionChangePenalty = 0.3f;
                config.PathRandomness = 0.15f;
                break;

            case "fish":
                config.SetTerrainCost(TileId.Ocean, 1.0f);
                config.BlockTerrain(TileId.Plains);
                config.BlockTerrain(TileId.Forest);
                config.BlockTerrain(TileId.Mountain);
                config.DirectionChangePenalty = 0.5f;
                config.WanderingBias = 0.2f;
                config.PathRandomness = 0.1f;
                break;

            case "land_animal":
                config.SetTerrainCost(TileId.Plains, 1.0f);
                config.SetTerrainCost(TileId.Forest, 1.8f);
                config.SetTerrainCost(TileId.Mountain, 4.0f);
                config.BlockTerrain(TileId.Ocean);
                config.PathRandomness = 0.2f;
                config.DiagonalPenalty = 1.3f;
                break;

            case "amphibian":
                config.SetTerrainCost(TileId.Plains, 1.2f);
                config.SetTerrainCost(TileId.Forest, 1.5f);
                config.SetTerrainCost(TileId.Mountain, 3.0f);
                config.SetTerrainCost(TileId.Ocean, 0.8f);
                config.PathRandomness = 0.25f;
                config.WanderingBias = 0.15f;
                break;
        }

        return config;
    }

    public static TerrainConfig CreatePathStyle(string styleName)
    {
        TerrainConfig config = new TerrainConfig();

        switch (styleName.ToLower())
        {
            case "straight":
                config.PathRandomness = 0.0f;
                config.DirectionChangePenalty = 0.0f;
                config.DiagonalPenalty = 1.0f;
                config.WanderingBias = 0.0f;
                break;

            case "curvy":
                config.PathRandomness = 0.1f;
                config.DirectionChangePenalty = 0.5f;
                config.DiagonalPenalty = 1.0f;
                config.WanderingBias = 0.2f;
                break;

            case "meandering":
                config.PathRandomness = 0.3f;
                config.DirectionChangePenalty = 0.2f;
                config.DiagonalPenalty = 1.4f;
                config.WanderingBias = 0.4f;
                break;

            case "serpentine":
                config.PathRandomness = 0.15f;
                config.DirectionChangePenalty = 0.7f;
                config.DiagonalPenalty = 1.0f;
                config.WanderingBias = 0.35f;
                break;

            case "jagged":
                config.PathRandomness = 0.25f;
                config.DirectionChangePenalty = 0.0f;
                config.DiagonalPenalty = 0.8f;
                config.WanderingBias = 0.1f;
                break;

            case "organic":
                config.PathRandomness = 0.35f;
                config.DirectionChangePenalty = 0.25f;
                config.DiagonalPenalty = 1.3f;
                config.WanderingBias = 0.3f;
                break;

            case "flowing":
                config.PathRandomness = 0.08f;
                config.DirectionChangePenalty = 0.6f;
                config.DiagonalPenalty = 1.2f;
                config.WanderingBias = 0.25f;
                break;

            case "stream":
                config.PathRandomness = 0.15f;
                config.DirectionChangePenalty = 0.4f;
                config.DiagonalPenalty = 1.3f;
                config.WanderingBias = 0.3f;
                config.WaveAmplitude = 2.0f;
                config.WaveFrequency = 0.4f;
                config.OrganicNoise = 0.8f;
                config.StraightLineAvoidance = 0.3f;
                break;

            case "river":
                config.PathRandomness = 0.12f;
                config.DirectionChangePenalty = 0.7f;
                config.DiagonalPenalty = 1.2f;
                config.WanderingBias = 0.4f;
                config.WaveAmplitude = 3.5f;
                config.WaveFrequency = 0.25f;
                config.OrganicNoise = 1.2f;
                config.DirectionInertia = 0.8f;
                break;

            case "creek":
                config.PathRandomness = 0.25f;
                config.DirectionChangePenalty = 0.3f;
                config.DiagonalPenalty = 1.4f;
                config.WanderingBias = 0.35f;
                config.WaveAmplitude = 1.5f;
                config.WaveFrequency = 0.7f;
                config.OrganicNoise = 1.5f;
                config.StraightLineAvoidance = 0.5f;
                break;

            case "path":
                config.PathRandomness = 0.18f;
                config.DirectionChangePenalty = 0.4f;
                config.DiagonalPenalty = 1.25f;
                config.WanderingBias = 0.2f;
                config.OrganicNoise = 1.0f;
                config.StraightLineAvoidance = 0.25f;
                config.DirectionInertia = 0.5f;
                break;

            case "canyon":
                config.PathRandomness = 0.2f;
                config.DirectionChangePenalty = 0.5f;
                config.DiagonalPenalty = 1.15f;
                config.WanderingBias = 0.5f;
                config.WaveAmplitude = 4.0f;
                config.WaveFrequency = 0.3f;
                config.OrganicNoise = 2.0f;
                config.DirectionInertia = 0.6f;
                break;

            case "lightning":
                config.PathRandomness = 0.4f;
                config.DirectionChangePenalty = 0.0f;
                config.DiagonalPenalty = 0.7f;
                config.WanderingBias = 0.15f;
                config.OrganicNoise = 2.5f;
                config.StraightLineAvoidance = 0.6f;
                break;

            case "root":
                config.PathRandomness = 0.3f;
                config.DirectionChangePenalty = 0.35f;
                config.DiagonalPenalty = 1.1f;
                config.WanderingBias = 0.4f;
                config.OrganicNoise = 1.8f;
                config.StraightLineAvoidance = 0.4f;
                config.DirectionInertia = 0.3f;
                break;

            case "tendril":
                config.PathRandomness = 0.2f;
                config.DirectionChangePenalty = 0.8f;
                config.DiagonalPenalty = 1.0f;
                config.WanderingBias = 0.6f;
                config.WaveAmplitude = 2.5f;
                config.WaveFrequency = 0.5f;
                config.OrganicNoise = 1.0f;
                config.DirectionInertia = 0.9f;
                break;

            case "road":
                config.PathRandomness = 0.05f;
                config.RequireCardinalConnectivity = true;
                config.BiomeChangePenalty = 0.3f;
                config.BiomeStickiness = 0.4f;
                config.TerrainTransitionSmoothness = 0.5f;
                config.AvoidBacktracking = true;
                config.StraightLineAvoidance = 0.2f;
                config.EnableObstacleAvoidance = true;
                break;

            case "tunnel":
                config.PathRandomness = 0.1f;
                config.RequireCardinalConnectivity = true;
                config.DiagonalPenalty = 2.0f;
                config.StraightLineAvoidance = 0.3f;
                config.BiomeStickiness = 0.6f;
                config.AvoidBacktracking = true;
                break;

            case "wall":
                config.PathRandomness = 0.15f;
                config.RequireCardinalConnectivity = true;
                config.BiomeChangePenalty = 0.5f;
                config.StraightLineAvoidance = 0.4f;
                config.OrganicNoise = 1.2f;
                break;

            case "biome_path":
                config.PathRandomness = 0.2f;
                config.BiomeChangePenalty = 1.5f;
                config.BiomeStickiness = 1.2f;
                config.TerrainTransitionSmoothness = 0.8f;
                config.WanderingBias = 0.3f;
                config.OrganicNoise = 1.0f;
                break;

            case "efficient":
                config.PathRandomness = 0.0f;
                config.RequireCardinalConnectivity = false;
                config.AvoidBacktracking = true;
                config.BacktrackingPenalty = 3.0f;
                config.WanderingBias = 0.0f;
                config.EnableObstacleAvoidance = false;
                break;
        }

        return config;
    }
}