using System;
using Sandbox.Events;

namespace Mountain;

public sealed class EquipmentSpawnPoint : Component, IGameEventHandler<BetweenRoundCleanupEvent>
{
    [Property]
    public EquipmentResource Equipment { get; set; }
    
    [Property, FeatureEnabled("Spawn Chance")]
    public bool UseSpawnChance { get; set; } = false;
    
    [Property, Feature("Spawn Chance"), FeatureEnabled("Spawn Chance"), Range(0, 1), Step(0.01f)]
    public float ChancePercentage { get; set; }
    
    [Property, FeatureEnabled("Spawn Force")]
    public bool UseSpawnForce { get; set; } = false;

    [Property, Feature("Spawn Force"), FeatureEnabled("Spawn Force")]
    private float SpawnForce { get; set; } = 1000f;
    
    
    private static readonly Model Model = Model.Load("models/arrow.vmdl");

    protected override void DrawGizmos()
    {
        Gizmo.Hitbox.Model(Model);

        var so = Gizmo.Draw.Model(Model);

        if (so is not null)
        {
            so.Flags.CastShadows = true;
        }
    }

    protected override void OnStart()
    {
        SpawnEquipment();
    }

    public void OnGameEvent(BetweenRoundCleanupEvent eventArgs)
    {
        SpawnEquipment();
    }
    
    private void SpawnEquipment()
    {
        if (UseSpawnChance && Random.Shared.Float(0f, 1f) > ChancePercentage)
        {
            Log.Info($"Did not spawn equipment {Equipment} at {this} due to spawn chance.");
            return;
        }
        
        if (!Equipment.DroppedWorldModelPrefab.IsValid())
        {
            Log.Warning($"Equipment spawn point {this} has an invalid equipment resource {Equipment}.");
            return;
        }

        var gameObject = Equipment.DroppedWorldModelPrefab.Clone(new CloneConfig
        {
            Transform = Transform.World,
            Parent = GameObject,
        });
        
        gameObject.NetworkSpawn();
        gameObject.WorldPosition = WorldPosition + Vector3.Up * 10f;
        gameObject.WorldRotation = gameObject.WorldRotation.RotateAroundAxis(Vector3.Forward, 90f);
        
        if (UseSpawnForce)
        {
            var rb = gameObject.GetComponent<Rigidbody>();
            rb?.ApplyImpulse(Vector3.Up * SpawnForce + Vector3.Random * SpawnForce);
        }
        
        Log.Info($"Spawned equipment {Equipment} at {this}.");
    }
}