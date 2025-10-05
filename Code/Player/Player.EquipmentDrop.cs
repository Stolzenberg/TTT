namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("Equipment")]
    public float ThrowEquipmentForce { get; set; } = 2500f;

    private void UpdateEquipmentDrop()
    {
        if (!Input.Pressed("Drop"))
        {
            return;
        }

        RequestDropActiveEquipment();
    }
    
    [Rpc.Host]
    private void RequestDropActiveEquipment()
    {
        if (!ActiveEquipment.IsValid())
        {
            return;
        }

        if (ActiveEquipment.Resource.Slot == EquipmentSlot.Fists)
        {
            return;
        }
        
        DropEquipment(ActiveEquipment);
    }
    
    [Rpc.Host]
    private void RequestDropAllEquipment()
    {
        foreach (var equipment in Equipments.Values.Where(e => e.Resource.Slot != EquipmentSlot.Fists))
        {
            DropEquipment(equipment);
        }
    }

    private void DropEquipment(Equipment equipment)
    {
        var worldPosition = equipment.WorldPosition;
        var worldRotation = equipment.WorldRotation;
        var direction = worldPosition + worldRotation.Forward;
        
        var droppedEquipment = DroppedEquipment.Create(equipment.Resource,
            direction * 32f, Rotation.Identity, equipment);
        
        ServerRemoveEquipment(equipment);

        droppedEquipment.Rigidbody.ApplyImpulse(direction * ThrowEquipmentForce);
    }
}