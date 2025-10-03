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

        var droppedEquipment = DroppedEquipment.Create(ActiveEquipment.Resource,
            Camera.WorldPosition + Camera.WorldRotation.Forward * 32f, Rotation.Identity, ActiveEquipment);
        
        ServerRemoveEquipment(ActiveEquipment);

        droppedEquipment.Rigidbody.ApplyImpulse(Camera.WorldRotation.Forward * ThrowEquipmentForce);
    }
}