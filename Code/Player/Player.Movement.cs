namespace Mountain;

public sealed partial class Player : IScenePhysicsEvents
{
    [Sync]
    public Vector3 WishVelocity { get; set; }
    public MovementState? Mode { get; private set; }
    public Rigidbody Body =>
        body ??= GetComponent<Rigidbody>();

    /// <summary>
    ///     Our actual physical velocity minus our ground velocity
    /// </summary>
    public Vector3 Velocity { get; private set; }
    public bool IsSprinting { get; private set; }

    /// <summary>
    ///     We will apply extra friction when we're on the ground and our desired velocity is
    ///     lower than our current velocity, so we will slow down.
    /// </summary>
    [Property, Group("Movement"), Range(0, 1)]
    public float BrakePower { get; init; } = 1;

    /// <summary>
    ///     How much friction to add when we're in the air. This will slow you down unless you have a wish
    ///     velocity.
    /// </summary>
    [Property, Group("Movement"), Range(0, 1)]
    public float AirFriction { get; init; } = 0.1f;
    
    /// <summary>
    /// If true, we're not allowed to move.
    /// </summary>
    [Sync( SyncFlags.FromHost )] public bool IsFrozen { get; set; }
    
    private Rigidbody? body;

    void IScenePhysicsEvents.PrePhysicsStep()
    {
        UpdateBody();

        if (!Client.IsLocalClient)
        {
            return;
        }

        Mode?.AddVelocity();
        Mode?.AddJump();
        Mode?.PrePhysicsStep();
    }

    void IScenePhysicsEvents.PostPhysicsStep()
    {
        Velocity = Body.Velocity - GroundVelocity;
        UpdateGroundVelocity();

        Mode?.PostPhysicsStep();
        CheckGround();

        HandleImpactDamage();

        ChooseBestMovementState();
    }

    private void InputMove(Vector3 input)
    {
        var rot = EyeAngles.ToRotation();
        
        if (IsFrozen)
        {
            WishVelocity = Vector3.Zero;
            return;
        }
        
        WishVelocity = Mode!.UpdateState(rot, input);
    }

    private void ToggleSprinting(bool value)
    {
        IsSprinting = value;
    }

    private void ChooseBestMovementState()
    {
        var best = GetComponents<MovementState>().MaxBy(x => x.Score(this));
        if (Mode == best)
        {
            return;
        }

        Mode?.OnStateEnd(best);

        Mode = best;

        Body.PhysicsBody.Sleeping = false;

        Mode?.OnStateBegin();
    }

    private void UpdateBody()
    {
        var locking = Body.Locking;
        locking.Pitch = true;
        locking.Yaw = true;
        locking.Roll = true;
        Body.Locking = locking;

        // When trying to move, we move the mass center up to the waist so the player can "step" over smaller shit
        // When not moving we drop it to the foot position.
        var massCenter = IsOnGround ? WishVelocity.Length.Clamp(0, BodyHeight * 0.5f) : BodyHeight * 0.5f;
        Body.MassCenterOverride = new(0, 0, massCenter);
        Body.OverrideMassCenter = true;
        
        Mode?.UpdateRigidBody(Body);
    }
}