namespace Mountain;

public sealed class EquipmentViewModel : EquipmentModel
{
    public Equipment Equipment { get; set; }

    public bool PlayDeployEffects
    {
        set
        {
            ModelRenderer.Set("b_deploy", value);
            ModelRenderer.Set("b_deploy_skip", !value);
        }
    }

    [Property, Range(0, 1), Feature("View")]
    private readonly float ironSightsFireScale = 0.2f;

    [Property, Feature("View")]
    private readonly float yawInertiaScale = 10f;

    [Property, Feature("View")]
    private readonly float pitchInertiaScale = 10f;

    private bool activateInertia;
    private float lastPitch;
    private float lastYaw;

    private Vector3 lerpedWishMove;
    private float PitchInertia;
    private float YawInertia;

    public void SetFireMode(FireMode currentFireMode)
    {
        var mode = currentFireMode switch
        {
            FireMode.Semi => 1,
            FireMode.Automatic => 3,
            FireMode.Burst => 2,
            _ => 0,
        };

        ModelRenderer.Set("firing_mode", mode);
    }

    protected override void OnStart()
    {
        if (GetComponentInChildren<ShootingBehavior>() is { } shoot)
        {
            SetFireMode(shoot.CurrentFireMode);
        }
    }

    protected override void OnUpdate()
    {
        ViewModel();
        ApplyInertia();
        ApplyVelocity();
    }

    private void ViewModel()
    {
        if (!Equipment.IsValid())
        {
            return;
        }

        ModelRenderer.Set("b_sprint", Equipment.Owner.Mode is RunMovementState);
        ModelRenderer.Set("b_grounded", Equipment.Owner.IsOnGround);

        var isAiming = Equipment.EquipmentFlags.HasFlag(Equipment.EquipmentFlag.Aiming);
        ModelRenderer.Set("ironsights", isAiming ? 1 : 0);
        ModelRenderer.Set("ironsights_fire_scale", isAiming ? ironSightsFireScale : 0f);

        if (Equipment.Aimable != null)
        {
            ModelRenderer.Set("speed_ironsights", Equipment.Aimable.AimSpeed * 0.05f);
        }

        if (Equipment.Reloadable != null)
        {
            ModelRenderer.Set("reload_speed", Equipment.Reloadable.ReloadTime);
        }

        ModelRenderer.Set("b_twohanded", true);
    }

    private void ApplyInertia()
    {
        if (!Client.Local.IsValid())
        {
            return;
        }

        var inRot = Client.Local.Camera.WorldRotation;

        // Need to fetch data from the camera for the first frame
        if (!activateInertia)
        {
            lastPitch = inRot.Pitch();
            lastYaw = inRot.Yaw();
            YawInertia = 0;
            PitchInertia = 0;
            activateInertia = true;
        }

        var newPitch = Client.Local.Camera.WorldRotation.Pitch();
        var newYaw = Client.Local.Camera.WorldRotation.Yaw();

        PitchInertia = Angles.NormalizeAngle(newPitch - lastPitch);
        YawInertia = Angles.NormalizeAngle(lastYaw - newYaw);

        lastPitch = newPitch;
        lastYaw = newYaw;
    }

    private void ApplyVelocity()
    {
        if (!Equipment.IsValid())
        {
            return;
        }

        var moveVel = Equipment.Owner.Velocity;
        var moveLen = moveVel.Length;

        var wishMove = Equipment.Owner.WishVelocity.Normal * 1f;

        lerpedWishMove = lerpedWishMove.LerpTo(wishMove, Time.Delta * 7.0f);
        ModelRenderer.Set("move_bob", moveLen.Remap(0, 300, 0, 1, true));

        YawInertia += lerpedWishMove.y;

        ModelRenderer.Set("aim_yaw_inertia", YawInertia * yawInertiaScale);
        ModelRenderer.Set("aim_pitch_inertia", PitchInertia * pitchInertiaScale);
    }
}