using System;

namespace Mountain;

public sealed class DroppedEquipment : Component, Component.ICollisionListener
{
    public static DroppedEquipment Create(EquipmentResource resource, Vector3 positon, Rotation rotation,
        Equipment? equipment = null)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("DroppedEquipment can only be created on the host.");
        }
        
        var gameObject = new GameObject
        {
            WorldPosition = positon,
            WorldRotation = rotation,
            Name = resource.ResourceName,
        };

        gameObject.Tags.Add("pickup");

        var worldModel = resource.WorldModel;
        var bounds = worldModel.Bounds;

        var droppedWeapon = gameObject.Components.Create<DroppedEquipment>();
        droppedWeapon.Resource = resource;

        var renderer = gameObject.Components.Create<SkinnedModelRenderer>();
        renderer.Model = worldModel;

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

    public Rigidbody Rigidbody { get; set; }
    public EquipmentResource Resource { get; set; }

    public void OnCollisionStart(Collision collision)
    {
        var player = collision.Other.GameObject.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        if (player.Has(Resource))
        {
            Log.Info($"Player {player.Client.DisplayName} tried to pick up {Resource.ResourceName} but already has one.");
            return;
        }

        player.ServerGive(Resource);
        GameObject.Destroy();
    }
}