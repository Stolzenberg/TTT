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

        if (!Player.Client.IsLocalPlayer)
        {
            return;
        }

        if (Input.Pressed("Voice"))
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
        var rot = eyes.Angles() with { pitch = Player.Camera.WorldRotation.Pitch() };

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
        return isNoclip ? 10000 : -10000;
    }

    protected override float GetSpeed()
    {
        return speed;
    }
}