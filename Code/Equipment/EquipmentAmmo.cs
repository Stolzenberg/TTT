namespace Mountain;

public class EquipmentAmmo : Component
{
    [Property, Sync]
    public int Ammo { get; set; } = 0;

    [Property]
    public int MaxAmmo { get; init; } = 30;

    [Property]
    public bool HasAmmo => Ammo > 0;

    public bool IsFull => Ammo == MaxAmmo;
}