using System;

namespace Mountain;

public sealed class DroppedEquipment : Component, Component.ITriggerListener
{
    public Rigidbody Rigidbody { get; set; } = null!;
    public EquipmentResource Resource { get; set; } = null!;
    private const float PickupCooldown = 0.1f;
    private TimeSince timeSinceDropped;

    public void OnTriggerEnter(GameObject other)
    {
        if (!Networking.IsHost)
        {
            return;
        }

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
                $"{player.Client.DisplayName} tried to pick up {Resource.ResourceName} but already has equipment on this slot {Resource.Slot}.");

            return;
        }

        var equipment = player.Give(Resource);
        equipment.OwnerPickup(this);

        GameObject.Destroy();

        Log.Info($"{player.Client.DisplayName} picked up {equipment}.");
    }

    public static DroppedEquipment Create(EquipmentResource resource, Vector3 position, Rotation rotation,
        Equipment? equipment = null)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Creating dropped equipment can only be created on the host.");
        }

        var gameObject = new GameObject
        {
            WorldPosition = position,
            WorldRotation = rotation,
            Name = resource.ResourceName,
            Tags = { "pickup" },
        };

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
        droppedWeapon.Rigidbody.MassOverride = 15f; // So throwing feels for all weapons the same.

        gameObject.Components.Create<DestroyBetweenRounds>();

        if (equipment.IsValid())
        {
            foreach (var state in equipment.GetComponents<IDroppedEquipmentState>())
            {
                Log.Info($"Transferring state {droppedWeapon} to dropped equipments component {state}.");
                state.CopyToDropped(droppedWeapon);
            }

            gameObject.Tags.Add("dropped_data");
        }

        gameObject.NetworkSpawn();

        return droppedWeapon;
    }
}