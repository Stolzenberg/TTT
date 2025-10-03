using System;

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

        if (ActiveEquipment == null)
        {
            return;
        }

        var equipment = ActiveEquipment;
        var resource = ActiveEquipment.Resource;
        
        ServerRemoveEquipment(ActiveEquipment);

        var droppedEquipment = DroppedEquipment.Create(resource,
            Camera.WorldPosition + Camera.WorldRotation.Forward * 32f, Rotation.Identity, equipment);

        droppedEquipment.Rigidbody.ApplyImpulse(Camera.WorldRotation.Forward * ThrowEquipmentForce);
    }
}