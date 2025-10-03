namespace Mountain;

public interface IDroppedEquipmentState
{
    void CopyToDropped(DroppedEquipment dropped);
    void CopyFromDropped(DroppedEquipment dropped);
}

public interface IDroppedEquipmentState<T> : IDroppedEquipmentState where T : Component, IDroppedEquipmentState<T>, new()
{
    void IDroppedEquipmentState.CopyToDropped(DroppedEquipment dropped)
    {
        var state = dropped.GetOrAddComponent<T>();

        ((T)this).CopyPropertiesTo(state);
    }

    void IDroppedEquipmentState.CopyFromDropped(DroppedEquipment dropped)
    {
        if (dropped.GetComponent<T>() is { } state)
        {
            state.CopyPropertiesTo((T)this);
        }
    }
}