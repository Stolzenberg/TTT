namespace Mountain;

public class EquipmentAmmo : Component, IDroppedEquipmentState<EquipmentAmmo>
{
    [Property, Sync]
    public int Ammo { get; set; } = 0;

    [Property]
    public int MaxAmmo { get; set; } = 30;

    [Property]
    public bool HasAmmo => Ammo > 0;

    public bool IsFull => Ammo == MaxAmmo;
}