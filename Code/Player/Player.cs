namespace Mountain;

public sealed partial class Player : Component
{
    [Sync(SyncFlags.FromHost)]
    public Client Client { get; set; }

    [Property, Group("Debug"), Description("Enable debug visualization and logging.")]
    public bool EnableDebug { get; set; } = false;

    protected override void OnStart()
    {
        ChooseBestMovementState();
        ApplyClothing();
        SetHeadPosition(clothing.Height);
        InitializeKarma();
    }

    protected override void OnUpdate()
    {
        if (!Game.IsPlaying)
        {
            return;
        }

        ApplyAnimationParameters();
        UpdateVelocity();
        UpdateRotation();
        UpdateEyes();
        UpdateHeadPosition();
        UpdateCameraPosition();
        UpdateFov();

        if (Health.State == LifeState.Dead)
        {
            return;
        }

        if (IsLocallyControlled)
        {
            UpdateEyeAngles();
            UpdateInput();

            UpdateLookAt();
            UpdateEquipmentChange();
            UpdateEquipmentDrop();
        }
    }
}