using System;
using Sandbox.Events;

namespace Mountain;

/// <summary>
/// Defines the type of loot that can be spawned.
/// </summary>
public enum LootType
{
    Equipment,
    Ammo,
    Currency,
}

/// <summary>
/// General-purpose loot spawn point that can spawn equipment or ammo.
/// </summary>
public sealed class LootSpawnPoint : Component, IGameEventHandler<BetweenRoundCleanupEvent>
{
    [Property]
    public LootType LootType { get; set; } = LootType.Equipment;

    [Property, ShowIf(nameof(LootType), LootType.Equipment)]
    public EquipmentResource Equipment { get; set; }

    [Property, ShowIf(nameof(LootType), LootType.Ammo)]
    public AmmoType AmmoType { get; set; } = AmmoType.Pistol;

    [Property, ShowIf(nameof(LootType), LootType.Ammo)]
    public Model AmmoModel { get; set; }

    [Property, ShowIf(nameof(LootType), LootType.Currency)]
    public Model CurrencyModel { get; set; }

    [Property]
    public int Amount { get; set; } = 30;

    [Property, FeatureEnabled("Spawn Chance")]
    public bool UseSpawnChance { get; set; } = false;

    [Property, Feature("Spawn Chance"), FeatureEnabled("Spawn Chance"), Range(0, 1), Step(0.01f)]
    public float ChancePercentage { get; set; } = 0.7f;

    [Property, FeatureEnabled("Spawn Force")]
    public bool UseSpawnForce { get; set; } = false;

    [Property, Feature("Spawn Force"), FeatureEnabled("Spawn Force")]
    public float SpawnForce { get; set; } = 1000f;

    private static readonly Model GizmoModel = Model.Load("models/arrow.vmdl");

    public void OnGameEvent(BetweenRoundCleanupEvent eventArgs)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        SpawnLoot();
    }

    protected override void DrawGizmos()
    {
        Gizmo.Hitbox.Model(GizmoModel);

        var so = Gizmo.Draw.Model(GizmoModel);

        if (so is not null)
        {
            so.Flags.CastShadows = true;
        }
    }

    protected override void OnStart()
    {
        if (!Networking.IsHost)
        {
            return;
        }

        SpawnLoot();
    }

    private void SpawnLoot()
    {
        if (UseSpawnChance && Random.Shared.Float(0f, 1f) > ChancePercentage)
        {
            Log.Info($"Did not spawn loot at {WorldPosition} due to spawn chance.");

            return;
        }

        var spawnPosition = WorldPosition + Vector3.Up * 10f;
        var spawnRotation = Rotation.FromYaw(90);

        DroppedLoot? droppedLoot = null;

        switch (LootType)
        {
            case LootType.Equipment:
                if (Equipment != null && Equipment.IsValid())
                {
                    droppedLoot = DroppedEquipment.Create(Equipment, spawnPosition, spawnRotation);
                    Log.Info($"Spawned equipment {Equipment.ResourceName} at {WorldPosition}.");
                }
                else
                {
                    Log.Warning($"Cannot spawn equipment at {WorldPosition}: Equipment resource is not set.");
                }

                break;

            case LootType.Ammo:
                if (AmmoType != AmmoType.None && Amount > 0)
                {
                    droppedLoot = DroppedAmmo.Create(AmmoType, Amount, spawnPosition, spawnRotation, AmmoModel);
                    Log.Info($"Spawned {Amount} {AmmoType} ammo at {WorldPosition}.");
                }
                else
                {
                    Log.Warning($"Cannot spawn ammo at {WorldPosition}: Invalid ammo type or amount.");
                }

                break;
            case LootType.Currency:
                if (Amount > 0)
                {
                    droppedLoot = DroppedCurrency.Create(Amount, spawnPosition, spawnRotation, CurrencyModel);
                    Log.Info($"Spawned currency amount {Amount} at {WorldPosition}.");
                }
                else
                {
                    Log.Warning($"Cannot spawn currency at {WorldPosition}: Invalid amount.");
                }

                break;
        }

        if (droppedLoot != null && UseSpawnForce)
        {
            droppedLoot.Rigidbody.ApplyImpulse(Vector3.Up * SpawnForce + Vector3.Random * SpawnForce);
        }
    }
}