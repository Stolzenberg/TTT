using Sandbox.Events;

namespace Mountain;

public sealed class PrepareMapSpawns : Component, IGameEventHandler<EnterStateEvent>
{
    [Property, Category("Dynamic Spawn Generation")]
    public int MinSpawnPointsPerTeam { get; set; } = 8;

    [Property, Category("Dynamic Spawn Generation"), Description("Minimum distance between spawn points")]
    public float MinSpawnDistance { get; set; } = 150f;

    [Property, Category("Dynamic Spawn Generation"),
     Description("Radius to check for free space around spawn point (0-1, percentage of min spawn distance)"),
     Range(0f, 1f)]
    public float FreeSpaceRadius { get; set; } = 0.6f;

    [Property, Category("Dynamic Spawn Generation"),
     Description("How much to shrink the map bounds for spawn generation (to avoid spawning at edges)")]
    public float BoundsShrinkAmount { get; set; } = 200f;

    [Property, Category("Dynamic Spawn Generation"), Description("Maximum attempts to find a valid spawn location")]
    public int MaxSpawnAttempts { get; set; } = 100;

    [Property, Category("Dynamic Spawn Generation"), Description("Height offset above ground for spawn points")]
    public float SpawnHeightOffset { get; set; } = 10f;

    [Property, Category("Dynamic Spawn Generation"), Description("Player body radius for collision checks")]
    public float PlayerBodyRadius { get; set; } = 16f;

    [Property, Category("Dynamic Spawn Generation"), Description("Player body height for collision checks")]
    public float PlayerBodyHeight { get; set; } = 72f;

    [Property, Category("Dynamic Spawn Generation"), Description("Minimum distance from walls")]
    public float MinWallDistance { get; set; } = 32f;

    [Property, Category("Dynamic Spawn Generation"), Description("For what teams should spawn points be generated")]
    public List<Team> Teams { get; set; } = [Team.Unassigned];

    // Loot Spawn Properties
    [Property, Category("Loot Spawn Generation"), Description("Minimum number of loot spawn points to generate")]
    public int MinLootSpawnPoints { get; set; } = 15;

    [Property, Category("Loot Spawn Generation"), Description("Minimum distance between loot spawn points")]
    public float MinLootSpawnDistance { get; set; } = 80f;

    [Property, Category("Loot Spawn Generation"),
     Description("Radius to check for free space around loot spawn point (0-1, percentage of min distance)"),
     Range(0f, 1f)]
    public float LootFreeSpaceRadius { get; set; } = 0.4f;

    [Property, Category("Loot Spawn Generation"), Description("Height offset above ground for loot spawns")]
    public float LootSpawnHeightOffset { get; set; } = 5f;

    [Property, Category("Loot Spawn Generation"), Description("Minimum distance from walls for loot spawns")]
    public float LootMinWallDistance { get; set; } = 24f;

    [Property, Category("Loot Spawn Generation"), Description("Loot collision check radius")]
    public float LootCollisionRadius { get; set; } = 8f;

    [Property, Category("Loot Spawn Generation"), Description("Loot collision check height")]
    public float LootCollisionHeight { get; set; } = 16f;

    [Property, Category("Loot Spawn Generation"), Description("List of loot items to spawn (equipment and ammo)")]
    public List<LootSpawnConfig> LootToSpawn { get; set; } = new();

    [Property, Category("Loot Spawn Generation"), Description("Default spawn chance for loot"), Range(0f, 1f)]
    public float DefaultLootSpawnChance { get; set; } = 0.7f;

    [Property, Category("Loot Spawn Generation"), Description("Default spawn force for loot")]
    public float DefaultLootSpawnForce { get; set; } = 1000f;

    [Property, Category("Loot Spawn Generation"), Description("Enable spawn force by default")]
    public bool DefaultUseSpawnForce { get; set; } = false;

    private MapInstance mapInstance = null!;

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        HandlePlayerSpawns();
        HandleLootSpawns();
    }

    protected override void OnStart()
    {
        mapInstance = GameMode.Instance.Get<MapInstance>();
    }

    private void HandlePlayerSpawns()
    {
        var teamSpawnPoints = mapInstance.GetComponentsInChildren<TeamSpawnPoint>().ToList();
        if (teamSpawnPoints.Any())
        {
            Log.Info($"Team spawn points already exist in map: {teamSpawnPoints.Count}, skipping generation.");

            return;
        }

        var spawns = Scene.GetAllComponents<SpawnPoint>().Where(s => s.Components.Get<TeamSpawnPoint>() == null)
            .ToList();

        if (spawns.Any())
        {
            var index = 0;
            foreach (var spawn in spawns)
            {
                var teamSpawnPoint = spawn.GameObject.AddComponent<TeamSpawnPoint>();
                spawn.GameObject.Name = $"SpawnPoint ({teamSpawnPoint.Team}) {index++}";
                spawn.Destroy();
            }

            Log.Info($"Converted {index} spawn points to team spawn points.");
        }
        else
        {
            Log.Info("No spawn points found in map, generating dynamic spawn points...");
            GenerateDynamicSpawnPoints();
        }
    }

    private void GenerateDynamicSpawnPoints()
    {
        var bounds = mapInstance.Bounds;

        // Shrink bounds to avoid spawning at edges
        var min = bounds.Mins + new Vector3(BoundsShrinkAmount, BoundsShrinkAmount, 0);
        var max = bounds.Maxs - new Vector3(BoundsShrinkAmount, BoundsShrinkAmount, 0);

        Log.Info($"Generating spawn points within bounds: Min={min}, Max={max}");

        var allSpawnPositions = new List<Vector3>();

        foreach (var team in Teams)
        {
            var teamSpawns = GenerateSpawnPointsForTeam(team, min, max, allSpawnPositions);

            if (teamSpawns.Count == 0)
            {
                Log.Warning($"Failed to generate any spawn points for team {team}!");
            }
            else
            {
                Log.Info($"Generated {teamSpawns.Count} spawn points for team {team}");
            }
        }
    }

    private List<Vector3> GenerateSpawnPointsForTeam(Team team, Vector3 min, Vector3 max, List<Vector3> existingSpawns)
    {
        var spawnPositions = new List<Vector3>();
        var attempts = 0;
        var targetCount = MinSpawnPointsPerTeam;

        while (spawnPositions.Count < targetCount && attempts < MaxSpawnAttempts * targetCount)
        {
            attempts++;

            // Generate random position within bounds
            var randomPos = new Vector3(Game.Random.Float(min.x, max.x), Game.Random.Float(min.y, max.y),
                max.z // Start from top of bounds
            );

            // Trace down to find ground
            var groundPos = FindGroundPosition(randomPos, min.z);

            if (!groundPos.HasValue)
            {
                continue;
            }

            var spawnPos = groundPos.Value + Vector3.Up * SpawnHeightOffset;

            // Check if position is valid (not too close to other spawns and has free space)
            if (!IsValidSpawnPosition(spawnPos, spawnPositions, existingSpawns))
            {
                continue;
            }

            // Create spawn point
            CreateTeamSpawnPoint(team, spawnPos, spawnPositions.Count);
            spawnPositions.Add(spawnPos);
            existingSpawns.Add(spawnPos);
        }

        if (attempts >= MaxSpawnAttempts * targetCount && spawnPositions.Count < targetCount)
        {
            Log.Warning(
                $"Reached max attempts ({attempts}) for team {team}, only created {spawnPositions.Count}/{targetCount} spawn points");
        }

        return spawnPositions;
    }

    private Vector3? FindGroundPosition(Vector3 startPos, float minZ)
    {
        // Trace down to find ground
        var trace = Scene.Trace.Ray(startPos, startPos + Vector3.Down * 10000f)
            .WithoutTags("player", "trigger", "pickup").Run();

        if (trace.Hit && trace.EndPosition.z >= minZ)
        {
            return trace.EndPosition;
        }

        return null;
    }

    private bool IsValidSpawnPosition(Vector3 position, List<Vector3> teamSpawns, List<Vector3> allSpawns)
    {
        // Check distance to other spawns of same team
        foreach (var existingSpawn in teamSpawns)
        {
            if (Vector3.DistanceBetween(position, existingSpawn) < MinSpawnDistance)
            {
                return false;
            }
        }

        // Check distance to spawns of other teams (should be at least half the min distance)
        foreach (var existingSpawn in allSpawns)
        {
            if (Vector3.DistanceBetween(position, existingSpawn) < MinSpawnDistance * 0.5f)
            {
                return false;
            }
        }

        // Check if a player can actually stand at this spawn point (collision checks)
        if (!CanPlayerStandAtPosition(position))
        {
            return false;
        }

        // Check if spawn point is too close to walls
        if (!HasMinimumWallClearance(position))
        {
            return false;
        }

        // Check for free space around spawn point
        if (!HasFreeSpace(position))
        {
            return false;
        }

        return true;
    }

    private bool CanPlayerStandAtPosition(Vector3 position)
    {
        // Create a bounding box representing the player's body
        var bodyBox = new BBox(new Vector3(-PlayerBodyRadius, -PlayerBodyRadius, 0),
            new Vector3(PlayerBodyRadius, PlayerBodyRadius, PlayerBodyHeight));

        // Check if player body would collide with anything at this position
        var bodyTrace = Scene.Trace.Box(bodyBox, position, position).WithoutTags("player", "trigger", "pickup").Run();

        if (bodyTrace.Hit)
        {
            return false;
        }

        // Verify there's solid ground beneath the spawn point
        var groundCheckStart = position + Vector3.Down * 5f;
        var groundCheckEnd = position + Vector3.Down * (SpawnHeightOffset + 10f);

        var groundTrace = Scene.Trace.Ray(groundCheckStart, groundCheckEnd).WithoutTags("player", "trigger", "pickup")
            .Run();

        // Must have ground within reasonable distance
        if (!groundTrace.Hit || groundTrace.Distance > SpawnHeightOffset + 5f)
        {
            return false;
        }

        // Check that the ground is relatively flat (not too steep)
        var groundNormal = groundTrace.Normal;
        var angle = Vector3.GetAngle(groundNormal, Vector3.Up);

        // Ground should be relatively flat (less than 45 degrees)
        if (angle > 45f)
        {
            return false;
        }

        return true;
    }

    private bool HasMinimumWallClearance(Vector3 position)
    {
        // Check in cardinal and diagonal directions for nearby walls
        var checkDirections = new[]
        {
            Vector3.Forward,
            Vector3.Backward,
            Vector3.Left,
            Vector3.Right,
            (Vector3.Forward + Vector3.Right).Normal,
            (Vector3.Forward + Vector3.Left).Normal,
            (Vector3.Backward + Vector3.Right).Normal,
            (Vector3.Backward + Vector3.Left).Normal
        };

        // Check at multiple heights (feet, waist, head)
        var checkHeights = new[] { 10f, PlayerBodyHeight * 0.5f, PlayerBodyHeight * 0.9f };

        foreach (var height in checkHeights)
        {
            var checkPos = position + Vector3.Up * height;

            foreach (var direction in checkDirections)
            {
                var endPos = checkPos + direction * MinWallDistance;
                var trace = Scene.Trace.Ray(checkPos, endPos).WithoutTags("player", "trigger", "pickup").Run();

                // If we hit a wall closer than the minimum distance, reject this spawn
                if (trace.Hit && trace.Distance < MinWallDistance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool HasFreeSpace(Vector3 position)
    {
        var checkRadius = MinSpawnDistance * FreeSpaceRadius;
        var directions = new[]
        {
            Vector3.Forward,
            Vector3.Backward,
            Vector3.Left,
            Vector3.Right,
            (Vector3.Forward + Vector3.Right).Normal,
            (Vector3.Forward + Vector3.Left).Normal,
            (Vector3.Backward + Vector3.Right).Normal,
            (Vector3.Backward + Vector3.Left).Normal
        };

        // Check if there's enough free space in multiple directions
        var freeDirections = 0;

        foreach (var direction in directions)
        {
            var endPos = position + direction * checkRadius;
            var trace = Scene.Trace.Ray(position, endPos).WithoutTags("player", "trigger", "pickup").Run();

            if (!trace.Hit || trace.Distance >= checkRadius * 0.8f)
            {
                freeDirections++;
            }
        }

        // Require at least 50% of directions to be free
        return freeDirections >= directions.Length / 2;
    }

    private void CreateTeamSpawnPoint(Team team, Vector3 position, int index)
    {
        var spawnObject = new GameObject
        {
            WorldPosition = position,
            WorldRotation = Rotation.Identity,
            Name = $"SpawnPoint ({team}) {index} [Generated]"
        };

        var teamSpawnPoint = spawnObject.AddComponent<TeamSpawnPoint>();
        teamSpawnPoint.Team = team;

        spawnObject.SetParent(mapInstance.GameObject);
    }

    private void HandleLootSpawns()
    {
        var existingLootSpawns = mapInstance.GetComponentsInChildren<LootSpawnPoint>().ToList();
        if (existingLootSpawns.Any())
        {
            Log.Info($"Loot spawn points found in map: {existingLootSpawns.Count}, skipping dynamic generation.");

            return;
        }

        if (LootToSpawn.Count == 0)
        {
            Log.Warning("No loot configured for spawning. Add loot to LootToSpawn list.");

            return;
        }

        Log.Info("No loot spawn points found in map, generating dynamic loot spawns...");
        GenerateDynamicLootSpawns();
    }

    private void GenerateDynamicLootSpawns()
    {
        var bounds = mapInstance.Bounds;

        // Shrink bounds to avoid spawning at edges
        var min = bounds.Mins + new Vector3(BoundsShrinkAmount, BoundsShrinkAmount, 0);
        var max = bounds.Maxs - new Vector3(BoundsShrinkAmount, BoundsShrinkAmount, 0);

        Log.Info($"Generating loot spawn points within bounds: Min={min}, Max={max}");

        var allLootPositions = new List<Vector3>();
        var attempts = 0;
        var targetCount = MinLootSpawnPoints;
        var spawnedCount = 0;

        // Calculate total weight for weighted random selection
        var totalWeight = LootToSpawn.Sum(l => l.SpawnWeight);

        while (spawnedCount < targetCount && attempts < MaxSpawnAttempts * targetCount)
        {
            attempts++;

            // Generate random position within bounds
            var randomPos = new Vector3(Game.Random.Float(min.x, max.x), Game.Random.Float(min.y, max.y),
                max.z // Start from top of bounds
            );

            // Trace down to find ground
            var groundPos = FindGroundPosition(randomPos, min.z);

            if (!groundPos.HasValue)
            {
                continue;
            }

            var spawnPos = groundPos.Value + Vector3.Up * LootSpawnHeightOffset;

            // Check if position is valid
            if (!IsValidLootSpawnPosition(spawnPos, allLootPositions))
            {
                continue;
            }

            // Pick a random loot from the list using weighted random
            var lootConfig = SelectWeightedRandomLoot(totalWeight);

            if (lootConfig == null)
            {
                continue;
            }

            // Create loot spawn point
            CreateLootSpawnPoint(lootConfig, spawnPos, spawnedCount);
            allLootPositions.Add(spawnPos);
            spawnedCount++;
        }

        if (attempts >= MaxSpawnAttempts * targetCount && spawnedCount < targetCount)
        {
            Log.Warning(
                $"Reached max attempts ({attempts}) for loot spawns, only created {spawnedCount}/{targetCount} spawn points");
        }
        else
        {
            Log.Info($"Generated {spawnedCount} loot spawn points");
        }
    }

    private LootSpawnConfig? SelectWeightedRandomLoot(float totalWeight)
    {
        var randomValue = Game.Random.Float(0f, totalWeight);
        var currentWeight = 0f;

        foreach (var lootConfig in LootToSpawn)
        {
            currentWeight += lootConfig.SpawnWeight;
            if (randomValue <= currentWeight)
            {
                return lootConfig;
            }
        }

        // Fallback to first item if something goes wrong
        return LootToSpawn.FirstOrDefault();
    }

    private bool IsValidLootSpawnPosition(Vector3 position, List<Vector3> existingSpawns)
    {
        // Check distance to other loot spawns
        foreach (var existingSpawn in existingSpawns)
        {
            if (Vector3.DistanceBetween(position, existingSpawn) < MinLootSpawnDistance)
            {
                return false;
            }
        }

        // Check if loot can fit at this position (collision checks)
        if (!CanLootFitAtPosition(position))
        {
            return false;
        }

        // Check if spawn point is too close to walls
        if (!HasLootWallClearance(position))
        {
            return false;
        }

        // Check for free space around spawn point
        if (!HasLootFreeSpace(position))
        {
            return false;
        }

        return true;
    }

    private bool CanLootFitAtPosition(Vector3 position)
    {
        // Create a bounding box representing the loot's collision
        var lootBox = new BBox(new Vector3(-LootCollisionRadius, -LootCollisionRadius, 0),
            new Vector3(LootCollisionRadius, LootCollisionRadius, LootCollisionHeight));

        // Check if loot would collide with anything at this position
        var lootTrace = Scene.Trace.Box(lootBox, position, position).WithoutTags("player", "trigger", "pickup").Run();

        if (lootTrace.Hit)
        {
            return false;
        }

        // Verify there's solid ground beneath the spawn point
        var groundCheckStart = position + Vector3.Down * 2f;
        var groundCheckEnd = position + Vector3.Down * (LootSpawnHeightOffset + 10f);

        var groundTrace = Scene.Trace.Ray(groundCheckStart, groundCheckEnd).WithoutTags("player", "trigger", "pickup")
            .Run();

        // Must have ground within reasonable distance
        if (!groundTrace.Hit || groundTrace.Distance > LootSpawnHeightOffset + 5f)
        {
            return false;
        }

        // Check that the ground is relatively flat (not too steep)
        var groundNormal = groundTrace.Normal;
        var angle = Vector3.GetAngle(groundNormal, Vector3.Up);

        // Ground should be relatively flat (less than 45 degrees)
        if (angle > 45f)
        {
            return false;
        }

        return true;
    }

    private bool HasLootWallClearance(Vector3 position)
    {
        // Check in cardinal and diagonal directions for nearby walls
        var checkDirections = new[]
        {
            Vector3.Forward,
            Vector3.Backward,
            Vector3.Left,
            Vector3.Right,
            (Vector3.Forward + Vector3.Right).Normal,
            (Vector3.Forward + Vector3.Left).Normal,
            (Vector3.Backward + Vector3.Right).Normal,
            (Vector3.Backward + Vector3.Left).Normal
        };

        // Check at loot height
        var checkPos = position + Vector3.Up * (LootCollisionHeight * 0.5f);

        foreach (var direction in checkDirections)
        {
            var endPos = checkPos + direction * LootMinWallDistance;
            var trace = Scene.Trace.Ray(checkPos, endPos).WithoutTags("player", "trigger", "pickup").Run();

            // If we hit a wall closer than the minimum distance, reject this spawn
            if (trace.Hit && trace.Distance < LootMinWallDistance)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasLootFreeSpace(Vector3 position)
    {
        var checkRadius = MinLootSpawnDistance * LootFreeSpaceRadius;
        var directions = new[]
        {
            Vector3.Forward,
            Vector3.Backward,
            Vector3.Left,
            Vector3.Right,
            (Vector3.Forward + Vector3.Right).Normal,
            (Vector3.Forward + Vector3.Left).Normal,
            (Vector3.Backward + Vector3.Right).Normal,
            (Vector3.Backward + Vector3.Left).Normal
        };

        // Check if there's enough free space in multiple directions
        var freeDirections = 0;

        foreach (var direction in directions)
        {
            var endPos = position + direction * checkRadius;
            var trace = Scene.Trace.Ray(position, endPos).WithoutTags("player", "trigger", "pickup").Run();

            if (!trace.Hit || trace.Distance >= checkRadius * 0.7f)
            {
                freeDirections++;
            }
        }

        // Require at least 40% of directions to be free (less strict than player spawns)
        return freeDirections >= (int)(directions.Length * 0.4f);
    }

    private void CreateLootSpawnPoint(LootSpawnConfig config, Vector3 position, int index)
    {
        var lootName = config.LootType == LootType.Equipment
            ? config.Equipment?.ResourceName ?? "Unknown"
            : $"{config.AmmoAmount}x{config.AmmoType}";

        var spawnObject = new GameObject
        {
            WorldPosition = position,
            WorldRotation = Rotation.Identity,
            Name = $"LootSpawn ({lootName}) {index} [Generated]"
        };

        var lootSpawnPoint = spawnObject.AddComponent<LootSpawnPoint>();
        lootSpawnPoint.LootType = config.LootType;

        // Configure based on loot type
        if (config.LootType == LootType.Equipment)
        {
            if (config.Equipment != null)
            {
                lootSpawnPoint.Equipment = config.Equipment;
            }
        }
        else if (config.LootType == LootType.Ammo)
        {
            lootSpawnPoint.AmmoType = config.AmmoType;
            lootSpawnPoint.AmmoAmount = config.AmmoAmount;
            lootSpawnPoint.AmmoModel = config.AmmoModel;
        }

        // Apply configuration settings
        if (config.UseCustomSpawnChance)
        {
            lootSpawnPoint.UseSpawnChance = true;
            lootSpawnPoint.ChancePercentage = config.SpawnChance;
        }
        else if (DefaultLootSpawnChance < 1f)
        {
            lootSpawnPoint.UseSpawnChance = true;
            lootSpawnPoint.ChancePercentage = DefaultLootSpawnChance;
        }

        if (config.UseCustomSpawnForce)
        {
            lootSpawnPoint.UseSpawnForce = true;
            lootSpawnPoint.SpawnForce = config.SpawnForce;
        }
        else if (DefaultUseSpawnForce)
        {
            lootSpawnPoint.UseSpawnForce = true;
            lootSpawnPoint.SpawnForce = DefaultLootSpawnForce;
        }

        spawnObject.SetParent(mapInstance.GameObject);

        Log.Info($"Created loot spawn for {lootName} at {position}");
    }
}