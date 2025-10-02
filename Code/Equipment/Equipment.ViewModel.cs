namespace Mountain;

public sealed partial class Equipment
{
    public EquipmentViewModel ViewModel
    {
        get => viewModel;
        private set
        {
            viewModel = value;

            if (viewModel.IsValid())
            {
                viewModel.Equipment = this;
            }
        }
    }
    [Sync]
    private bool HasCreatedViewModel { get; set; }

    private EquipmentViewModel viewModel;

    private void DestroyViewModel()
    {
        if (ViewModel.IsValid())
        {
            ViewModel.GameObject.Destroy();
        }
    }

    /// <summary>
    ///     Creates a viewmodel for the player to use.
    /// </summary>
    private void CreateViewModel(bool playDeployEffects = true)
    {
        DestroyViewModel();
        UpdateRenderMode();

        if (Resource.ViewModelPrefab.IsValid())
        {
            // We want to use Scene.Camera directly here to avoid issues with the player not created, or client local not being set yet.
            var viewModelGameObject = Resource.ViewModelPrefab.Clone(Scene.Camera.GameObject, Vector3.Zero, Rotation.Identity, Vector3.One);
            
            viewModelGameObject.BreakFromPrefab();

            var viewModelComponent = viewModelGameObject.GetComponent<EquipmentViewModel>();
            viewModelComponent.PlayDeployEffects = playDeployEffects;

            ViewModel = viewModelComponent;
        }

        if (!playDeployEffects)
        {
            return;
        }

        var snd = Sound.Play(DeploySound, WorldPosition);
        if (!snd.IsValid())
        {
            return;
        }

        snd.SpacialBlend = Owner.IsProxy ? snd.SpacialBlend : 0;
    }
}