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
    
    private bool previousDeployedState;

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
        
        Log.Info($"Deployed {this}");
    }

    [Rpc.Owner]
    public void Holster()
    {
        if (!IsDeployed)
        {
            return;
        }

        IsDeployed = false;
        
        Log.Info($"Holstered {this}");
    }

    private void OnIsDeployedPropertyChanged(bool oldValue, bool newValue)
    {
        UpdateDeployedState();
    }

    private void UpdateDeployedState()
    {
        Log.Info($"{Owner.Client.DisplayName}'s {this} deployed state changed: {previousDeployedState} -> {IsDeployed}");
        
        // No state change, nothing to do
        if (IsDeployed == previousDeployedState)
        {
            return;
        }

        switch (IsDeployed)
        {
            // Handle state transitions
            case true when !previousDeployedState:
                // Transitioning from holstered to deployed
                OnDeployed();

                break;
            case false when previousDeployedState:
                // Transitioning from deployed to holstered
                OnHolstered();

                break;
        }

        // Update our tracking of the previous state
        previousDeployedState = IsDeployed;
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
        
        Log.Info($"{Owner.Client.DisplayName} is going to deploy {this}");
    }

    private void OnHolstered()
    {
        UpdateRenderMode();
        DestroyWorldModel();
        DestroyViewModel();

        HasCreatedViewModel = false;

        GameObject.Root.Dispatch(new EquipmentHolsteredEvent(this));
        
        Log.Info($"{Owner.Client.DisplayName} is going to holster {this}");
    }
}
