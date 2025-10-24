namespace Mountain;

/// <summary>
/// Configuration for loot spawn generation (equipment or ammo).
/// </summary>
[Title("Loot Spawn Config")]
public class LootSpawnConfig
{
    [Property, Description("Type of loot to spawn")]
    public LootType LootType { get; set; } = LootType.Equipment;

    [Property, Description("Equipment to spawn"), ShowIf(nameof(LootType), LootType.Equipment)]
    public EquipmentResource? Equipment { get; set; }

    [Property, Description("Ammo type to spawn"), ShowIf(nameof(LootType), LootType.Ammo)]
    public AmmoType AmmoType { get; set; } = AmmoType.Pistol;

    [Property, Description("Custom model for ammo pickup"), ShowIf(nameof(LootType), LootType.Ammo)]
    public Model? AmmoModel { get; set; }

    [Property, Description("Amount to spawn"), Range(1, 100)]
    public int Amount { get; set; } = 30;

    [Property, Description("Use custom spawn chance for this loot")]
    public bool UseCustomSpawnChance { get; set; } = false;

    [Property, Description("Spawn chance percentage"), Range(0f, 1f), ShowIf(nameof(UseCustomSpawnChance), true)]
    public float SpawnChance { get; set; } = 0.7f;

    [Property, Description("Use custom spawn force for this loot")]
    public bool UseCustomSpawnForce { get; set; } = false;

    [Property, Description("Spawn force to apply"), ShowIf(nameof(UseCustomSpawnForce), true)]
    public float SpawnForce { get; set; } = 1000f;

    [Property, Description("Relative spawn weight (higher = more likely to spawn this item)"), Range(0.1f, 10f)]
    public float SpawnWeight { get; set; } = 1f;
}