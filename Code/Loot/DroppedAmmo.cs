using System;

namespace Mountain;

/// <summary>
/// Represents dropped ammunition that can be picked up by players.
/// </summary>
public sealed class DroppedAmmo : DroppedLoot
{
    [Property, Sync]
    public AmmoType AmmoType { get; set; } = AmmoType.None;

    [Property, Sync]
    public int Amount { get; set; } = 0;

    [Property]
    public Model AmmoModel { get; set; } = null!;

    public override bool TryPickup(Player player)
    {
        if (AmmoType == AmmoType.None || Amount <= 0)
        {
            Log.Warning($"Invalid ammo drop: Type={AmmoType}, Amount={Amount}");

            return false;
        }

        if (player.Equipments.All(e => e.Value.Resource.AmmoType != AmmoType))
        {
            Log.Info($"{player.Client.DisplayName} tried to pick up {AmmoType} ammo but has no weapon that uses it.");

            return false;
        }

        var ammoAdded = player.GiveAmmo(AmmoType, Amount);

        if (ammoAdded > 0)
        {
            Log.Info($"{player.Client.DisplayName} picked up {ammoAdded} {AmmoType} ammo");

            return true;
        }

        // Player's ammo is full
        return false;
    }

    public override string GetDisplayName()
    {
        return $"{Amount} {AmmoType} Ammo";
    }

    /// <summary>
    /// Creates a dropped ammo pickup in the world.
    /// </summary>
    public static DroppedAmmo Create(AmmoType ammoType, int amount, Vector3 position, Rotation rotation,
        Model? customModel = null)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Creating dropped ammo can only be done on the host.");
        }

        if (ammoType == AmmoType.None || amount <= 0)
        {
            throw new ArgumentException($"Invalid ammo parameters: Type={ammoType}, Amount={amount}");
        }

        var gameObject = new GameObject
        {
            WorldPosition = position,
            WorldRotation = rotation,
            Name = $"{ammoType}_Ammo_{amount}",
        };

        var droppedAmmo = gameObject.Components.Create<DroppedAmmo>();
        droppedAmmo.AmmoType = ammoType;
        droppedAmmo.Amount = amount;
        droppedAmmo.AmmoModel = customModel ?? GetDefaultModel();

        // Use a simple box bounds for ammo pickups
        var bounds = new BBox(new Vector3(-4, -4, 0), new Vector3(4, 4, 8));
        droppedAmmo.InitializeDroppedLoot(gameObject, bounds);

        gameObject.NetworkSpawn();

        return droppedAmmo;
    }

    protected override void CreateVisuals(GameObject gameObject)
    {
        if (AmmoModel.IsValid())
        {
            var renderer = gameObject.Components.Create<ModelRenderer>();
            renderer.Model = AmmoModel;
        }
    }
}