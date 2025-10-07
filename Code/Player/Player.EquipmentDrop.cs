using System.Numerics;

namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("Equipment")]
    public float ThrowEquipmentForce { get; set; } = 25f;

    private void UpdateEquipmentDrop()
    {
        if (!Input.Pressed("Drop"))
        {
            return;
        }

        RequestDropActiveEquipment(Client.Local.Camera.WorldPosition, Client.Local.Camera.WorldRotation);
    }
    
    [Rpc.Host]
    private void RequestDropActiveEquipment(Vector3 position, Rotation rotation)
    {
        if (!ActiveEquipment.IsValid())
        {
            return;
        }

        if (ActiveEquipment.Resource.Slot == EquipmentSlot.Fists)
        {
            return;
        }
        
        DropEquipment(ActiveEquipment, position, rotation);
    }
    
    [Rpc.Host]
    private void RequestDropAllEquipment()
    {
        foreach (var equipment in Equipments.Values.Where(e => e.Resource.Slot != EquipmentSlot.Fists))
        {
            DropEquipment(equipment, WorldPosition, WorldRotation);
        }
    }

    private void DropEquipment(Equipment equipment, Vector3 position, Rotation rotation)
    {
        var droppedEquipment = DroppedEquipment.Create(equipment.Resource,
            position, Rotation.Identity, equipment);
        
        ServerRemoveEquipment(equipment);

        droppedEquipment.Rigidbody.ApplyImpulse(position + rotation.Forward * ThrowEquipmentForce);
    }
}