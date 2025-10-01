using System;

namespace Mountain;

public sealed class DroppedEquipment : Component, Component.ITriggerListener
{
    private EquipmentResource equipment;

    public void Setup(EquipmentResource equipment)
    {
        this.equipment = equipment;
    }
    
    public void OnTriggerEnter(Collider other)
    {
        var player = other.GameObject.GetComponent<Player>();
        if (player == null) return;
        if (player.Has(equipment)) return;

        player.ServerGive(equipment); 
        GameObject.Destroy();
    }
}