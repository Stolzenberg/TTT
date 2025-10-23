# Loot System Documentation

## Overview

The loot system has been refactored to provide a unified approach for spawning and managing pickupable items in the
game, including both equipment (weapons) and ammunition.

## Architecture

### Core Components

#### 1. **IPickupable Interface**

Located in: `Code/Loot/IPickupable.cs`

Defines the contract for any item that can be picked up by players.

```csharp
public interface IPickupable
{
    bool TryPickup(Player player);
    string GetDisplayName();
}
```

#### 2. **DroppedLoot (Base Class)**

Located in: `Code/Loot/DroppedLoot.cs`

Abstract base class for all dropped loot items. Handles:

- Trigger detection for player pickups
- Pickup cooldown to prevent immediate re-pickup
- Common physics and visual setup
- Rigidbody and collider creation

**Key Methods:**

- `TryPickup(Player player)` - Override to implement pickup logic
- `GetDisplayName()` - Override to provide item name
- `CreateVisuals(GameObject)` - Override to create visual representation
- `InitializeDroppedLoot(GameObject, BBox)` - Call to set up common components

#### 3. **DroppedEquipment**

Located in: `Code/Equipment/DroppedEquipment.cs`

Refactored to inherit from `DroppedLoot`. Handles weapon pickups with magazine ammo state transfer.

**Usage:**

```csharp
var dropped = DroppedEquipment.Create(equipmentResource, position, rotation, equipment);
```

#### 4. **DroppedAmmo**

Located in: `Code/Loot/DroppedAmmo.cs`

New class for ammunition pickups. Players can pick up ammo to refill their reserve inventory.

**Properties:**

- `AmmoType` - Type of ammunition (Pistol, Rifle, Shotgun, etc.)
- `Amount` - Quantity of ammo
- `AmmoModel` - Visual model for the ammo pickup

**Usage:**

```csharp
var dropped = DroppedAmmo.Create(AmmoType.Rifle, 30, position, rotation, customModel);
```

**Pickup Behavior:**

- Only picks up if player has inventory space
- Adds ammo to player's reserve ammunition
- Returns false if player's ammo is full (item remains on ground)

### Spawning System

#### 1. **LootSpawnPoint**

Located in: `Code/Loot/LootSpawnPoint.cs`

General-purpose spawn point that can spawn either equipment or ammo.

**Properties:**

- `LootType` - Equipment or Ammo
- `Equipment` - Equipment resource to spawn (if type is Equipment)
- `AmmoType`, `AmmoAmount`, `AmmoModel` - Ammo configuration (if type is Ammo)
- `UseSpawnChance`, `ChancePercentage` - Random spawn chance
- `UseSpawnForce`, `SpawnForce` - Apply physics impulse on spawn

**Features:**

- Spawns loot on game start
- Re-spawns between rounds (responds to `BetweenRoundCleanupEvent`)
- Optional spawn chance for randomization
- Optional spawn force for dynamic spawning

#### 2. **LootSpawnConfig**

Located in: `Code/Loot/LootSpawnConfig.cs`

Configuration class for procedural loot generation in `PrepareMapSpawns`.

**Properties:**

- `LootType` - Equipment or Ammo
- Equipment-specific: `Equipment`
- Ammo-specific: `AmmoType`, `AmmoAmount`, `AmmoModel`
- `SpawnWeight` - Relative probability (higher = spawns more often)
- `UseCustomSpawnChance`, `SpawnChance` - Override default spawn chance
- `UseCustomSpawnForce`, `SpawnForce` - Override default spawn force

#### 3. **PrepareMapSpawns Integration**

Located in: `Code/Game/PrepareMapSpawns.cs`

Enhanced to support automatic loot spawn generation.

**New Properties:**

- `EnableLootSpawns` - Enable general loot spawning system
- `MinLootSpawnPoints` - Target number of loot spawn points
- `MinLootSpawnDistance` - Minimum spacing between loot spawns
- `LootToSpawn` - List of loot items with their configurations
- `DefaultLootSpawnChance` - Default spawn probability

**Behavior:**

1. If `EnableLootSpawns` is true and `LootToSpawn` has items, uses loot system
2. Otherwise, falls back to equipment-only spawning
3. Automatically detects existing `LootSpawnPoint` components and skips generation
4. Uses weighted random selection based on `SpawnWeight`
5. Validates spawn positions (ground, clearance, spacing)

## Usage Examples

### Example 1: Creating a Dropped Ammo Pickup

```csharp
// Drop 30 rifle ammo when player dies
var ammoDropPosition = player.WorldPosition + Vector3.Up * 10;
DroppedAmmo.Create(AmmoType.Rifle, 30, ammoDropPosition, Rotation.Identity);
```

### Example 2: Configuring Loot Spawns in PrepareMapSpawns

In the editor, configure the `PrepareMapSpawns` component:

```csharp
LootToSpawn = new List<LootSpawnConfig>
{
    // Spawn pistols frequently
    new LootSpawnConfig
    {
        LootType = LootType.Equipment,
        Equipment = pistolResource,
        SpawnWeight = 3.0f,
        UseCustomSpawnChance = true,
        SpawnChance = 0.8f
    },
    
    // Spawn pistol ammo very frequently
    new LootSpawnConfig
    {
        LootType = LootType.Ammo,
        AmmoType = AmmoType.Pistol,
        AmmoAmount = 30,
        SpawnWeight = 5.0f,
        SpawnChance = 0.9f
    },
    
    // Spawn sniper rifle rarely
    new LootSpawnConfig
    {
        LootType = LootType.Equipment,
        Equipment = sniperResource,
        SpawnWeight = 0.5f,
        SpawnChance = 0.3f
    }
};
```

### Example 3: Manual Loot Spawn Point

Place a `LootSpawnPoint` component in your scene:

```csharp
// For equipment
var spawnPoint = gameObject.AddComponent<LootSpawnPoint>();
spawnPoint.LootType = LootType.Equipment;
spawnPoint.Equipment = rifleResource;
spawnPoint.UseSpawnChance = true;
spawnPoint.ChancePercentage = 0.7f;

// For ammo
var ammoSpawn = gameObject.AddComponent<LootSpawnPoint>();
ammoSpawn.LootType = LootType.Ammo;
ammoSpawn.AmmoType = AmmoType.Shotgun;
ammoSpawn.AmmoAmount = 8;
ammoSpawn.UseSpawnForce = true;
ammoSpawn.SpawnForce = 500f;
```

## Integration with Ammunition System

The loot system integrates seamlessly with the player ammunition system:

1. **Player picks up ammo** → `Player.GiveAmmo(ammoType, amount)` is called
2. **Player's reserve increases** → Up to the max capacity defined in `Player.MaxAmmo`
3. **Player reloads weapon** → `Player.TakeAmmo(ammoType, amount)` transfers reserve to magazine
4. **Player drops weapon** → Magazine ammo stays with weapon, reserve ammo stays with player

## Benefits of Refactored System

1. **Unified Architecture** - All pickupable items inherit from `DroppedLoot`
2. **Extensibility** - Easy to add new loot types (health packs, armor, etc.)
3. **Consistency** - Same pickup behavior and physics for all loot
4. **Flexible Spawning** - Mix equipment and ammo in the same spawn system
5. **Weighted Randomization** - Control spawn probabilities with weights
6. **Clean Code** - Reduced duplication between equipment and ammo systems

## Future Enhancements

Potential additions to the loot system:

- Health pack pickups
- Armor pickups
- Special item pickups (keys, objectives)
- Custom ammo models per type
- Loot rarity system
- Drop tables for different game modes
- Loot pooling for performance

