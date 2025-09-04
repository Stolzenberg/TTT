namespace Mountain;

public class EquipmentAmmo : Component
{
    /// <summary>
    ///     How much ammo are we holding?
    /// </summary>
    [Property, Sync]
    public int Ammo { get; set; } = 0;

    [Property]
    public int MaxAmmo { get; init; } = 30;

    /// <summary>
    ///     Do we have any ammo?
    /// </summary>
    [Property]
    public bool HasAmmo => Ammo > 0;

    /// <summary>
    ///     Is this container full?
    /// </summary>
    public bool IsFull => Ammo == MaxAmmo;
}