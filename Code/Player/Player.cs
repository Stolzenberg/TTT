namespace Mountain;

public sealed partial class Player : Component
{
    [Sync(SyncFlags.FromHost)]
    public Client Client { get; set; }

    protected override void OnStart()
    {
        ChooseBestMovementState();
        ApplyClothing();
    }

    private TimeSince timeSinceBotRandomAngle = 0;

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
        UpdateCameraPosition();
        UpdateFov();

        // Just some random bot logic to debugg shit.
        if (Networking.IsHost && Client.IsBot)
        {
            if (timeSinceBotRandomAngle < 1f)
            {
                return;
            }

            timeSinceBotRandomAngle = 0;

            var angle = EyeAngles;
            angle += Vector3.Random * 100f;
            angle.roll = 0;

            if (pitchClamp > 0)
            {
                angle.pitch = angle.pitch.Clamp(-pitchClamp, pitchClamp);
            }

            EyeAngles = angle;

            var equipment = Equipments.Shuffle().First();
            if (!equipment.Value.IsValid())
            {
                return;
            }

            Switch(equipment.Value);
        }

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