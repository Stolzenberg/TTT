using System;

namespace Mountain;

public sealed partial class Player
{
    /// <summary>
    /// Stores the player's ammunition for different ammo types.
    /// Key is AmmoType, Value is the amount of ammo.
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public Dictionary<AmmoType, int> AmmoInventory { get; set; } = new();

    /// <summary>
    /// Maximum ammo a player can carry for each ammo type.
    /// </summary>
    [Property, Feature("Ammo")]
    public Dictionary<AmmoType, int> MaxAmmo { get; set; } = new()
    {
        { AmmoType.Pistol, 120 },
        { AmmoType.Rifle, 180 },
        { AmmoType.Shotgun, 32 },
        { AmmoType.Sniper, 30 },
        { AmmoType.SMG, 150 },
        { AmmoType.Rocket, 6 },
    };

    /// <summary>
    /// Gets the amount of ammo the player has for a specific type.
    /// </summary>
    public int GetAmmo(AmmoType ammoType)
    {
        if (ammoType == AmmoType.None)
        {
            return 0;
        }

        return AmmoInventory.GetValueOrDefault(ammoType, 0);
    }

    /// <summary>
    /// Sets the amount of ammo the player has for a specific type.
    /// </summary>
    public void SetAmmo(AmmoType ammoType, int amount)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("SetAmmo can only be called on the host.");
        }

        if (ammoType == AmmoType.None)
        {
            return;
        }

        var maxAmmo = MaxAmmo.GetValueOrDefault(ammoType, 0);
        AmmoInventory[ammoType] = Math.Clamp(amount, 0, maxAmmo);
    }

    /// <summary>
    /// Adds ammo to the player's inventory.
    /// </summary>
    /// <returns>The actual amount of ammo added (may be less than requested if max capacity reached)</returns>
    public int GiveAmmo(AmmoType ammoType, int amount)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("GiveAmmo can only be called on the host.");
        }

        if (ammoType == AmmoType.None || amount <= 0)
        {
            return 0;
        }

        var currentAmmo = GetAmmo(ammoType);
        var maxAmmo = MaxAmmo.GetValueOrDefault(ammoType, 0);
        var newAmmo = Math.Min(currentAmmo + amount, maxAmmo);
        var actualAmountAdded = newAmmo - currentAmmo;

        AmmoInventory[ammoType] = newAmmo;

        Log.Info($"{Client.DisplayName} received {actualAmountAdded} {ammoType} ammo (now has {newAmmo}/{maxAmmo})");

        return actualAmountAdded;
    }

    /// <summary>
    /// Takes ammo from the player's inventory.
    /// </summary>
    /// <returns>The actual amount of ammo taken (may be less than requested if not enough available)</returns>
    public int TakeAmmo(AmmoType ammoType, int amount)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("TakeAmmo can only be called on the host.");
        }

        if (ammoType == AmmoType.None || amount <= 0)
        {
            return 0;
        }

        var currentAmmo = GetAmmo(ammoType);
        var amountToTake = Math.Min(amount, currentAmmo);

        AmmoInventory[ammoType] = currentAmmo - amountToTake;

        return amountToTake;
    }

    /// <summary>
    /// Checks if the player has at least the specified amount of ammo.
    /// </summary>
    public bool HasAmmo(AmmoType ammoType, int amount = 1)
    {
        return GetAmmo(ammoType) >= amount;
    }
}