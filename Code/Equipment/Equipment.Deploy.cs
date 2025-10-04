using Sandbox.Events;

namespace Mountain;

public sealed partial class Equipment
{
    /// <summary>
    ///     Is this equipment currently deployed by the player?
    /// </summary>
    [Sync, Change(nameof(OnIsDeployedPropertyChanged))]
    public bool IsDeployed { get; private set; }

    /// <summary>
    ///     What sound should we play when taking this gun out?
    /// </summary>
    [Property, Group("Sounds")]
    public SoundEvent DeploySound { get; set; }
    private bool hasStarted;

    private bool wasDeployed;

    [Rpc.Owner]
    public void Deploy()
    {
        if (IsDeployed)
        {
            return;
        }

        // We must first holster all other equipment items.
        if (Owner.IsValid())
        {
            var equipment = Owner.Equipments.Values.ToList();

            foreach (var item in equipment)
            {
                item.Holster();
            }
        }

        IsDeployed = true;
    }

    [Rpc.Owner]
    public void Holster()
    {
        if (!IsDeployed)
        {
            return;
        }

        IsDeployed = false;
    }

    private void OnIsDeployedPropertyChanged(bool oldValue, bool newValue)
    {
        if (!hasStarted)
        {
            return;
        }

        UpdateDeployedState();
    }

    private void UpdateDeployedState()
    {
        if (IsDeployed == wasDeployed)
        {
            return;
        }

        switch (wasDeployed)
        {
            case false when IsDeployed:
                OnDeployed();

                break;
            case true when !IsDeployed:
                OnHolstered();

                break;
        }

        wasDeployed = IsDeployed;
    }

    private void OnDeployed()
    {
        if (Owner.IsValid() && Owner.IsPossessed)
        {
            CreateViewModel(!HasCreatedViewModel);
        }

        if (!IsProxy)
        {
            HasCreatedViewModel = true;
        }

        CreateWorldModel();
        UpdateRenderMode();

        GameObject.Root.Dispatch(new EquipmentDeployedEvent(this));
    }

    private void OnHolstered()
    {
        UpdateRenderMode();
        DestroyWorldModel();
        DestroyViewModel();

        HasCreatedViewModel = false;

        GameObject.Root.Dispatch(new EquipmentDeployedEvent(this));
    }
}