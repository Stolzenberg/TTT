using System;
using Sandbox.Citizen;
using Sandbox.Events;

namespace Mountain;

public record EquipmentDeployedEvent(Equipment Equipment) : IGameEvent;

public record EquipmentHolsteredEvent(Equipment Equipment) : IGameEvent;

public sealed partial class Equipment : Component
{
    [Property, Group("Resources")]
    public EquipmentResource Resource { get; set; }

    [RequireComponent]
    public TagBinder TagBinder { get; init; }

    /// <summary>
    ///     What flags do we have?
    /// </summary>
    [Sync]
    public EquipmentFlag EquipmentFlags { get; set; }

    [Sync(SyncFlags.FromHost)]
    public Player Owner { get; set; }

    [Property, Group("Animation")]
    public CitizenAnimationHelper.Hand Handedness { get; init; } = CitizenAnimationHelper.Hand.Right;

    [Property, Group("Animation")]
    public CitizenAnimationHelper.HoldTypes HoldType { get; init; } = CitizenAnimationHelper.HoldTypes.Rifle;

    public Reloadable? Reloadable =>
        reloadable ??= GetComponent<Reloadable>();
    private Reloadable? reloadable;
    
    public Aimable? Aimable =>
        aimable ??= GetComponent<Aimable>();
    private Aimable? aimable;
    
    [Flags]
    public enum EquipmentFlag
    {
        None = 0,
        Aiming = 1 << 2,
        Reloading = 1 << 3,
    }

    private void UpdateRenderMode()
    {
        if (WorldModel.IsValid())
        {
            WorldModel.ModelRenderer.RenderType = Owner.IsPossessed
                ? ModelRenderer.ShadowRenderType.ShadowsOnly
                : ModelRenderer.ShadowRenderType.On;
        }
    }

    protected override void OnStart()
    {
        previousDeployedState = IsDeployed;

        if (IsDeployed)
        {
            OnDeployed();
        }
        else
        {
            OnHolstered();
        }
    }

    public override string ToString()
    {
        return $"Equipment(Resource={Resource.NameKey}, Owner={Owner.Client.DisplayName}, IsDeployed={IsDeployed})";
    }
}