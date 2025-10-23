using System;

namespace Mountain;

public sealed class DroppedEquipment : DroppedLoot
{
    public EquipmentResource Resource { get; set; } = null!;

    public override bool TryPickup(Player player)
    {
        if (player.Has(Resource))
        {
            Log.Info(
                $"{player.Client.DisplayName} tried to pick up {Resource.ResourceName} but already has equipment on this slot {Resource.Slot}.");

            return false;
        }

        var equipment = player.Give(Resource);
        equipment.OwnerPickup(this);

        return true;
    }

    public override string GetDisplayName()
    {
        return Resource?.ResourceName ?? "Unknown Equipment";
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
        };

        var droppedWeapon = gameObject.Components.Create<DroppedEquipment>();
        droppedWeapon.Resource = resource;

        var worldModel = resource.WorldModel;
        var bounds = worldModel.Bounds;

        droppedWeapon.InitializeDroppedLoot(gameObject, bounds);

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

    protected override void CreateVisuals(GameObject gameObject)
    {
        var renderer = gameObject.Components.Create<SkinnedModelRenderer>();
        renderer.Model = Resource.WorldModel;
    }
}