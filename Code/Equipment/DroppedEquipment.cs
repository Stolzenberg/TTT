using System;

namespace Mountain;

public sealed class DroppedEquipment : Component, Component.ITriggerListener
{
    private const float PickupCooldown = 0.1f;
    private TimeSince timeSinceDropped;

    public static DroppedEquipment Create(EquipmentResource resource, Vector3 position, Rotation rotation,
        Equipment? equipment = null)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("DroppedEquipment can only be created on the host.");
        }

        var gameObject = new GameObject
        {
            WorldPosition = position,
            WorldRotation = rotation,
            Name = resource.ResourceName,
        };

        gameObject.Tags.Add("pickup");

        var worldModel = resource.WorldModel;
        var bounds = worldModel.Bounds;

        var droppedWeapon = gameObject.Components.Create<DroppedEquipment>();
        droppedWeapon.Resource = resource;
        droppedWeapon.timeSinceDropped = 0;

        var renderer = gameObject.Components.Create<SkinnedModelRenderer>();
        renderer.Model = worldModel;

        var trigger = gameObject.Components.Create<SphereCollider>();
        trigger.IsTrigger = true;
        trigger.Radius = 32f;

        var min = bounds.Mins;
        var max = bounds.Maxs;

        var collider = gameObject.Components.Create<BoxCollider>();
        collider.Scale = new(max.x - min.x, max.y - min.y, max.z - min.z);
        collider.Center = new(0, 0, (max.z - min.z) / 2);

        droppedWeapon.Rigidbody = gameObject.Components.Create<Rigidbody>();

        gameObject.Components.Create<DestroyBetweenRounds>();

        if (equipment is not null)
        {
            foreach (var state in equipment.GetComponents<IDroppedEquipmentState>())
            {
                state.CopyToDropped(droppedWeapon);
            }
        }

        gameObject.NetworkSpawn();

        return droppedWeapon;
    }

    public Rigidbody Rigidbody { get; set; } = null!;
    public EquipmentResource Resource { get; set; } = null!;

    public void OnTriggerEnter(GameObject other)
    {
        var player = other.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        if (timeSinceDropped < PickupCooldown)
        {
            return;
        }

        if (player.Has(Resource))
        {
            Log.Info(
                $"Player {player.Client.DisplayName} tried to pick up {Resource.ResourceName} but already has one.");

            return;
        }

        var equipment = player.ServerGive(Resource);
        
        Log.Info($"Player {player.Client.DisplayName} added {equipment}.");
        
        foreach (var state in equipment.GetComponents<IDroppedEquipmentState>())
        {
            state.CopyFromDropped(this);
        }

        GameObject.Destroy();

        Log.Info($"Player {player.Client.DisplayName} picked up {Resource.ResourceName}.");
    }
}