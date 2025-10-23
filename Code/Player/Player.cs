namespace Mountain;

public sealed partial class Player : Component
{
    [Sync(SyncFlags.FromHost)]
    public Client Client { get; set; }

    protected override void OnStart()
    {
        ChooseBestMovementState();
        ApplyClothing();
        SetHeadPosition(clothing.Height);
        InitializeKarma();

        SetAmmo(AmmoType.Shotgun, 32);
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