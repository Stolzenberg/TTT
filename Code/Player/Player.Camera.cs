namespace Mountain;

public sealed partial class Player
{
    public CameraComponent Camera { get; private set; }

    [Sync(SyncFlags.FromHost)]
    public Angles EyeAngles { get; set; }

    [Sync(SyncFlags.FromHost)]
    public Vector3 EyePosition { get; set; }

    [Property, Feature("Camera")]
    private readonly Vector3 cameraOffset = new(5, 11, 0);
    [Property, Feature("Camera")]
    private readonly float velocityModifier = 0.05f;
    [Property, Feature("Camera")]
    private readonly float defaultFovChangeSpeed = 25f;
    [Property, Feature("Camera")]
    private readonly GameObject head = null!;

    [Property, Feature("Camera"), Range(0, 2)]
    private readonly float lookSensitivity = 1;
    [Property, Feature("Camera"), Range(0, 180)]
    private readonly float pitchClamp = 85;

    private float fieldOfViewOffset;
    private float targetFieldOfView = Preferences.FieldOfView;

    public void AddFieldOfViewOffset(float degrees)
    {
        fieldOfViewOffset -= degrees;
    }

    private void SetupCamera()
    {
        if (!IsPossessed)
        {
            return;
        }

        Camera = Scene.Camera;
        Camera.FieldOfView = Preferences.FieldOfView;
    }

    private void UpdateEyeAngles()
    {
        if (Client.IsBot)
        {
            return;
        }

        var input = Input.AnalogLook;

        input *= lookSensitivity;

        var angle = EyeAngles;
        angle += input;
        angle.roll = 0;

        if (pitchClamp > 0)
        {
            angle.pitch = angle.pitch.Clamp(-pitchClamp, pitchClamp);
        }

        EyeAngles = angle;

        ApplyRecoil();
    }

    private void ApplyRecoil()
    {
        if (!ActiveEquipment.IsValid())
        {
            return;
        }

        if (ActiveEquipment.GetComponentInChildren<Recoil>() is { } fn)
        {
            EyeAngles += fn.Current;
        }
    }

    private void UpdateCameraPosition()
    {
        if (!IsPossessed)
        {
            return;
        }

        Camera.WorldRotation = Current!.EyeAngles;

        var position = head.WorldPosition;

        var offset = new Vector3
        {
            x = cameraOffset.x,
            y = cameraOffset.y + Velocity.Length * velocityModifier,
            z = cameraOffset.z,
        };

        var worldOffset = head.WorldRotation * offset;
        position += worldOffset;

        EyePosition = position;

        Camera.WorldPosition = Current.EyePosition;

        ScreenShaker.Main.Apply(Camera);
    }

    private void UpdateFov()
    {
        fieldOfViewOffset = 0;
        var speed = defaultFovChangeSpeed;

        if (ActiveEquipment.IsValid())
        {
            if (ActiveEquipment.EquipmentFlags.HasFlag(Equipment.EquipmentFlag.Aiming) &&
                ActiveEquipment.Aimable != null)
            {
                fieldOfViewOffset -= ActiveEquipment.Aimable.AimFieldOfView;
                speed = ActiveEquipment.Aimable.AimSpeed;
            }
        }

        targetFieldOfView = targetFieldOfView.LerpTo(Preferences.FieldOfView + fieldOfViewOffset, Time.Delta * speed);
        Camera.FieldOfView = targetFieldOfView;
    }
}