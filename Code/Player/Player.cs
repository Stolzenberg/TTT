namespace Mountain;

public sealed partial class Player : Component
{
    [Sync(SyncFlags.FromHost)]
    public Client Client { get; set; }

    private static Player? local;

    protected override void OnStart()
    {
        ChooseBestMovementState();
        ApplyClothing();
        SetupCamera();
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

        if (!Client.IsLocalPlayer)
        {
            return;
        }

        UpdateInput();
        UpdateLookAt();
        UpdateEyeAngles();
        UpdateCameraPosition();
        UpdateFov();
        UpdateEquipmentChange();
        UpdateEquipmentDrop();
    }
}