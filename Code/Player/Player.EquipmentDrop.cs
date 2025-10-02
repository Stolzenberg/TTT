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
        
        var resource = ActiveEquipment.Resource;
        ServerRemoveEquipment(ActiveEquipment);

        var gameObject = resource.DroppedWorldModelPrefab.Clone(new CloneConfig
        {
            Transform = Scene.WorldTransform,
            Parent = Scene,
        });
        
        gameObject.NetworkSpawn();

        var droppedEquipment = gameObject.GetComponent<DroppedEquipment>();
        droppedEquipment?.Setup(resource);

        gameObject.WorldPosition = Camera.WorldPosition + Camera.WorldRotation.Forward * 100f;

        var rb = gameObject.GetComponent<Rigidbody>();
        rb?.ApplyImpulse(Camera.WorldRotation.Forward * ThrowEquipmentForce);
    }
}