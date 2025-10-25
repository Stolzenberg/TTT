using System;

namespace Mountain;

public abstract class MovementState : Component
{
    /// <summary>
    /// Priority for this movement state when conditions are met.
    /// </summary>
    [Property]
    public int Priority { get; set; } = 100;

    private const float AccelerationTime = 0.01f;
    private const float DeaccelerationTime = 0.01f;

    protected Player Player =>
        player ??= GetComponent<Player>();
    private Player? player;

    private Vector3.SmoothDamped smoothedMovement;

    /// <summary>
    ///     Highest number becomes the new movement state.
    /// </summary>
    public virtual int Score(Player player)
    {
        return 0;
    }

    /// <summary>
    ///     This mode has just started.
    /// </summary>
    public virtual void OnStateBegin()
    {

    }

    /// <summary>
    ///     This mode has stopped. We're swapping to another move mode.
    /// </summary>
    public virtual void OnStateEnd(MovementState? next)
    {

    }

    /// <summary>
    ///     Called before the physics step is run
    /// </summary>
    public virtual void PrePhysicsStep()
    {

    }

    /// <summary>
    ///     Called after the physics step is run
    /// </summary>
    public virtual void PostPhysicsStep()
    {

    }

    public virtual void UpdateRigidBody(Rigidbody body)
    {
        var wantsGravity = !Player.IsOnGround;

        if (Player.Velocity.Length > 1)
        {
            wantsGravity = true;
        }

        if (Player.GroundVelocity.Length > 1)
        {
            wantsGravity = true;
        }

        if (Player.GroundIsDynamic)
        {
            wantsGravity = true;
        }

        body.Gravity = wantsGravity;

        // when we're standing on the still ground and aren't wishing to move we apply a high linear damping to the body
        // this stops whatever momentum it had from dragging it slowly down hills.
        var wantsBrakes = Player is { IsOnGround: true, WishVelocity.Length: < 1, GroundVelocity.Length: < 1 };
        body.LinearDamping = wantsBrakes ? 10.0f * Player.BrakePower : Player.AirFriction;

        body.AngularDamping = 1f;
    }

    public virtual void AddVelocity()
    {
        var body = Player.Body;
        var wish = Player.WishVelocity;
        if (wish.IsNearZeroLength)
        {
            return;
        }

        if (IsCollidingWithWall(wish))
        {
            return;
        }

        // Remove wish when walking against a wall

        var groundFriction = Player.GroundFriction;
        var groundVelocity = Player.GroundVelocity;

        var z = body.Velocity.z;

        var velocity = body.Velocity - Player.GroundVelocity;
        var speed = velocity.Length;

        var maxSpeed = MathF.Max(wish.Length, speed);

        if (Player.IsOnGround)
        {
            velocity = velocity.AddClamped(wish * groundFriction, wish.Length * groundFriction);
        }
        else
        {
            var amount = 0.05f;
            velocity = velocity.AddClamped(wish * amount, wish.Length);
        }

        if (velocity.Length > maxSpeed)
        {
            velocity = velocity.Normal * maxSpeed;
        }

        velocity += groundVelocity;

        if (Player.IsOnGround)
        {
            velocity.z = z;
        }

        body.Velocity = velocity;
    }

    public virtual bool IsStandableSurface(SceneTraceResult tr)
    {
        // Check if the surface is too steep to stand on
        // Surfaces with more than 45 degrees from vertical are not standable
        var angle = Vector3.GetAngle(tr.Normal, Vector3.Up);

        return angle <= 45f;
    }

    public virtual Vector3 UpdateState(Rotation eyes, Vector3 input)
    {
        eyes = eyes.Angles() with { pitch = 0 };

        var direction = (eyes * input).Normal;

        if (direction.IsNearlyZero(0.1f))
        {
            direction = 0;
        }
        else
        {
            // Retain momentum, once we're moving, we're moving. Don't lerp between directions, only between speeds.
            smoothedMovement.Current = direction * smoothedMovement.Current.Length;
        }

        // Smooth the wish velocity
        smoothedMovement.Target = direction * GetSpeed();
        smoothedMovement.SmoothTime = smoothedMovement.Target.Length < smoothedMovement.Current.Length
            ? DeaccelerationTime
            : AccelerationTime;

        smoothedMovement.Update(Time.Delta);

        // If it's near zero, just stop
        if (smoothedMovement.Current.IsNearlyZero(0.01f))
        {
            smoothedMovement.Current = 0;
        }

        return smoothedMovement.Current;
    }

    protected abstract float GetSpeed();

    /// <summary>
    /// Check if the player is colliding with a wall in the direction of movement.
    /// </summary>
    private bool IsCollidingWithWall(Vector3 direction, float distance = 32f)
    {
        if (direction.IsNearlyZero())
        {
            return false;
        }

        var from = Player.WorldPosition + Vector3.Up * (Player.BodyHeight * 0.5f);
        var to = from + direction.Normal * distance;

        var tr = Scene.Trace.Ray(from, to).IgnoreGameObjectHierarchy(Player.GameObject).WithCollisionRules(Player.Tags)
            .Run();

        return tr.Hit;
    }
}