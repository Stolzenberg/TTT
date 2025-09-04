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
    private readonly float yawInertiaScale = 2f;

    [Property, Feature("View")]
    private readonly float pitchInertiaScale = 2f;

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
        if (GetComponentInChildren<Shootable>() is { } shoot)
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
        if (!Client.Local.Player.IsValid())
        {
            return;
        }
        
        ModelRenderer.Set("b_sprint", Client.Local.Player.IsSprinting);
        ModelRenderer.Set("b_grounded", Client.Local.Player.IsOnGround);

        if (!Client.Local.Player.ActiveEquipment.IsValid())
        {
            return;
        }

        var isAiming = Client.Local.Player.ActiveEquipment.EquipmentFlags.HasFlag(Equipment.EquipmentFlag.Aiming);
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
        if (!Client.Local.Player.IsValid())
        {
            return;
        }
        
        var inRot = Client.Local.Player!.Camera.WorldRotation;

        // Need to fetch data from the camera for the first frame
        if (!activateInertia)
        {
            lastPitch = inRot.Pitch();
            lastYaw = inRot.Yaw();
            YawInertia = 0;
            PitchInertia = 0;
            activateInertia = true;
        }

        var newPitch = Client.Local.Player.Camera.WorldRotation.Pitch();
        var newYaw = Client.Local.Player.Camera.WorldRotation.Yaw();

        PitchInertia = Angles.NormalizeAngle(newPitch - lastPitch);
        YawInertia = Angles.NormalizeAngle(lastYaw - newYaw);

        lastPitch = newPitch;
        lastYaw = newYaw;
    }

    private void ApplyVelocity()
    {
        if (!Client.Local.Player.IsValid())
        {
            return;
        }
        
        var moveVel = Client.Local.Player.Velocity;
        var moveLen = moveVel.Length;

        var wishMove = Client.Local.Player.WishVelocity.Normal * 1f;

        lerpedWishMove = lerpedWishMove.LerpTo(wishMove, Time.Delta * 7.0f);
        ModelRenderer?.Set("move_bob", moveLen.Remap(0, 300, 0, 1, true));

        YawInertia += lerpedWishMove.y * 10f;

        ModelRenderer?.Set("aim_yaw_inertia", YawInertia * yawInertiaScale);
        ModelRenderer?.Set("aim_pitch_inertia", PitchInertia * pitchInertiaScale);
    }
}