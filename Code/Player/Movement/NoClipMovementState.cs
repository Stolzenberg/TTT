namespace Mountain;

public sealed class NoClipMovementState : MovementState
{
    [Sync]
    private bool isNoclip { get; set; }

    [Property]
    private readonly float speed = 220f;

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (!Player.IsLocallyControlled)
        {
            return;
        }

        if (Player.Health.State == LifeState.Dead)
        {
            return;
        }

        if (Input.Pressed("Voice") && Input.Down("Jump"))
        {
            isNoclip = !isNoclip;
        }
    }

    public override void UpdateRigidBody(Rigidbody body)
    {
        body.Gravity = false;
        body.LinearDamping = 10f;
        body.AngularDamping = 10f;
    }

    public override void OnStateBegin()
    {
        Player.Collider.Enabled = false;
    }

    public override void OnStateEnd(MovementState? next)
    {
        Player.Collider.Enabled = true;
    }

    public override Vector3 UpdateState(Rotation eyes, Vector3 input)
    {
        var rot = eyes.Angles() with { pitch = Client.Local.Camera.WorldRotation.Pitch() };

        if (Input.Down("Jump"))
        {
            input += Vector3.Up;
        }

        if (Input.Down("Duck"))
        {
            input += Vector3.Down;
        }

        return rot.ToRotation() * input * 4000f;
    }

    public override int Score(Player playerMovement)
    {
        return isNoclip ? Priority : 0;
    }

    protected override float GetSpeed()
    {
        return speed;
    }
}