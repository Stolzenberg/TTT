using System;

namespace Mountain;

public abstract class EquipmentComponent : Component
{
    public required Equipment Equipment { get; set; }
    protected Player Player => Equipment.Owner;

    protected void BindTag(string tag, Func<bool> predicate)
    {
        Equipment.TagBinder.BindTag(tag, predicate);
    }

    protected override void OnAwake()
    {
        Equipment = GetComponentInParent<Equipment>();

        base.OnAwake();
    }
}