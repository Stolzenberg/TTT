namespace Mountain;

/// <summary>
/// Movement state for climbing ladders and other vertical surfaces.
/// Dynamically scans the game world for climbable tags.
/// </summary>
public sealed class LadderMovementState : MovementState
{
    /// <summary>
    /// Base climbing speed in units per second.
    /// </summary>
    [Property]
    public float BaseClimbSpeed { get; set; } = 250f;

    /// <summary>
    /// A list of tags we can climb up - when they're on triggers or colliders.
    /// </summary>
    [Property]
    public TagSet ClimbableTags { get; set; } = ["ladder", "climbable"];

    /// <summary>
    /// How aggressively to pull the player toward the ladder center.
    /// </summary>
    [Property, Range(0f, 20f)]
    public float LadderSnapStrength { get; set; } = 5f;

    /// <summary>
    /// Maximum distance from ladder center before snapping stops.
    /// </summary>
    [Property]
    public float MaxSnapDistance { get; set; } = 50f;

    /// <summary>
    /// The velocity to apply when jumping off the ladder.
    /// </summary>
    [Property]
    public float DismountVelocity { get; set; } = 200f;

    /// <summary>
    /// If true, inverts climb direction when looking down sharply.
    /// </summary>
    [Property]
    public bool InvertWhenLookingDown { get; set; } = true;

    /// <summary>
    /// Pitch angle threshold for inverting climb direction.
    /// </summary>
    [Property, Range(0f, 90f)]
    public float InvertPitchThreshold { get; set; } = 50f;

    /// <summary>
    /// Maximum distance to check for climbable objects in front of the player.
    /// </summary>
    [Property]
    public float ClimbableCheckDistance { get; set; } = 32f;

    /// <summary>
    /// The GameObject we're currently climbing. This will usually be a ladder trigger.
    /// </summary>
    public Collider? ClimbingCollider { get; private set; }
    public Vector3? ClimbingForward { get; private set; }

    /// <summary>
    /// When climbing, this is the rotation of the wall/ladder you're climbing, where
    /// Forward is the direction to look at the ladder, and Up is the direction to climb.
    /// </summary>
    public Rotation ClimbingRotation { get; private set; }

    /// <summary>
    /// Indicates if the player is actively climbing.
    /// </summary>
    public bool IsClimbing => ClimbingCollider.IsValid();

    public override int Score(Player player)
    {
        ScanForClimbableObjects();

        return IsClimbing ? Priority : 0;
    }

    public override void OnStateBegin()
    {
        // Reset velocity when starting to climb
        Player.Body.Velocity = Vector3.Zero;
        
        Player.Body.WorldRotation = ClimbingRotation;
    }

    public override void OnStateEnd(MovementState? next)
    {
        // Clamp velocity when leaving the ladder to prevent excessive momentum
        var maxSpeed = GetSpeed();
        Player.Body.Velocity = Player.Body.Velocity.ClampLength(maxSpeed * 1.5f);
    }

    public override void UpdateRigidBody(Rigidbody body)
    {
        // Disable gravity and increase damping while climbing
        body.Gravity = false;
        body.LinearDamping = 20f;
        body.AngularDamping = 1f;
    }

    public override void PrePhysicsStep()
    {
        base.PrePhysicsStep();

        // Handle jump dismount
        if (Input.Pressed("Jump"))
        {
            DismountLadder();
        }
    }

    public override void PostPhysicsStep()
    {
        base.PostPhysicsStep();

        // Keep player centered on the ladder
        SnapToLadder();
    }

    public override void AddVelocity()
    {
        var wish = Player.WishVelocity;

        Player.Body.Velocity = wish;
    }

    public override Vector3 UpdateState(Rotation eyes, Vector3 input)
    {
        if (!IsClimbing)
        {
            return Vector3.Zero;
        }

        // Use the X input (forward/backward) for climbing up/down
        var climbInput = input.x;

        // Invert climbing direction when looking down sharply
        if (InvertWhenLookingDown && eyes.Pitch() > InvertPitchThreshold)
        {
            climbInput *= -1f;
        }

        // Calculate climb velocity in the ladder's up direction
        var climbVelocity = ClimbingRotation.Up * climbInput * BaseClimbSpeed;

        return climbVelocity;
    }

    protected override float GetSpeed()
    {
        return BaseClimbSpeed;
    }

    private float GetLookingAwayPercentage()
    {
        if (!IsClimbing)
        {
            return 0f;
        }

        // Use horizontal components only so pitch doesn't affect the measurement
        var eyeFwd = Player.EyeAngles.Forward.WithZ(0);

        if (eyeFwd.IsNearZeroLength)
        {
            return 0f;
        }
        
        if (!ClimbingForward.HasValue)
        {
            return 0f;
        }

        eyeFwd = eyeFwd.Normal;

        // Angle in degrees between 0 and 180
        var angle = Vector3.GetAngle(eyeFwd, ClimbingForward.Value);
        // Convert to percentage where 0° -> 0% (looking at ladder) and 180° -> 100% (looking directly away)
        return angle / 180f * 100f;
    }

    private bool IsLookingAwayOver(float thresholdPercent)
    {
        var percentage = GetLookingAwayPercentage();
        Log.Info("Looking away percentage: " + percentage);
        return percentage >= thresholdPercent;
    }

    /// <summary>
    /// Scans the player's touching colliders for climbable objects.
    /// </summary>
    private void ScanForClimbableObjects()
    {
        // If we're already climbing and not looking away, keep climbing
        if (ClimbingCollider.IsValid() && !IsLookingAwayOver(80f))
        {
            return;
        }

        var bestCandidate = BestCandidate();
        if (!bestCandidate.IsValid())
        {
            ClimbingCollider = null;

            return;
        }

        // Check if we need to update the climbing rotation
        // (either new collider or the hit normal was updated for current collider)
        var needsRotationUpdate = bestCandidate != ClimbingCollider;
        
        ClimbingCollider = bestCandidate;

        if (IsClimbing && needsRotationUpdate)
        {
            UpdateClimbingRotation();
        }
    }

    private Collider? BestCandidate()
    {
        var worldTransform = Player.WorldTransform;
        var playerPosition = worldTransform.Position;
        var playerHeight = Player.BodyHeight;

        Collider? bestCandidate = null;

        // Trace forward from the player at multiple heights to detect climbable surfaces
        var eyeRotation = Player.EyeAngles.ToRotation();
        var forwardDirection = eyeRotation.Forward.WithZ(0).Normal;

        // If the player isn't moving forward, use the body rotation instead
        if (forwardDirection.IsNearZeroLength)
        {
            forwardDirection = worldTransform.Rotation.Forward.WithZ(0).Normal;
        }

        // Check at multiple heights along the player's body
        var checkHeights = new[] { 0.25f, 0.5f, 0.75f };

        foreach (var heightRatio in checkHeights)
        {
            var checkHeight = playerHeight * heightRatio;
            var traceStart = playerPosition + Vector3.Up * checkHeight;
            var traceEnd = traceStart + forwardDirection * ClimbableCheckDistance;

            var trace = Scene.Trace.Ray(traceStart, traceEnd).WithAnyTags(ClimbableTags).HitTriggers()
                .IgnoreGameObjectHierarchy(Player.GameObject).Radius(2f).Run();

            // Debug visualization (optional)
            DebugOverlay.Line(traceStart, traceEnd, trace.Hit ? Color.Green : Color.Red);

            if (!trace.Hit)
            {
                continue;
            }

            if (!trace.Component.IsValid())
            {
                continue;
            }

            var collider = trace.Component.GetComponent<Collider>();

            // Prioritize the current climbing object to avoid flickering
            if (ClimbingCollider == collider)
            {
                bestCandidate = collider;

                break;
            }

            // Check if the surface is relatively vertical (ladder-like)
            var surfaceAngle = Vector3.GetAngle(trace.Normal, Vector3.Up);
            if (surfaceAngle is <= 60f or >= 120f) // Between 60-120 degrees means it's roughly vertical
            {
                continue;
            }

            bestCandidate = collider;

            break;
        }

        return bestCandidate;
    }

    /// <summary>
    /// Updates the climbing rotation based on the current climbing object's orientation.
    /// </summary>
    private void UpdateClimbingRotation()
    {
        if (!IsClimbing)
        {
            return;
        }

        // Calculate direction from player to ladder
        var toLadder = ClimbingCollider!.WorldPosition - Player.WorldPosition;
        ClimbingForward = Player.EyeAngles.Forward.WithZ(0);
        ClimbingRotation = ClimbingCollider.WorldRotation;
        
        Log.Info("Climbing forward set to: " + ClimbingForward.Value);

        // If the player is behind the ladder, flip the rotation 180 degrees
        if (toLadder.Dot(ClimbingRotation.Forward) < 0)
        {
            ClimbingRotation *= Rotation.FromYaw(180f);
        }
    }

    /// <summary>
    /// Keeps the player snapped to the center of the ladder.
    /// </summary>
    private void SnapToLadder()
    {
        if (!IsClimbing)
        {
            return;
        }

        var playerPos = Player.WorldPosition;
        var ladderPos = ClimbingCollider!.WorldPosition;
        var ladderUp = ClimbingCollider.WorldRotation.Up;

        // Find the closest point on the ladder's vertical line
        var ladderLine = new Line(ladderPos - ladderUp * 1000f, ladderPos + ladderUp * 1000f);
        var closestPointOnLadder = ladderLine.ClosestPoint(playerPos);

        // Calculate offset from ladder center (but keep vertical component)
        var offset = closestPointOnLadder - playerPos;
        offset = offset.SubtractDirection(ClimbingCollider.WorldRotation.Forward);

        // Only snap if within range
        if (offset.Length > MaxSnapDistance || offset.Length <= 0.01f)
        {
            return;
        }

        // Apply snapping force
        var snapForce = offset * LadderSnapStrength;
        Player.Body.Velocity = Player.Body.Velocity.AddClamped(snapForce, offset.Length * 10f);
    }

    /// <summary>
    /// Dismounts from the ladder with a velocity away from it.
    /// </summary>
    private void DismountLadder()
    {
        if (!IsClimbing)
        {
            return;
        }

        // Apply backward velocity to push away from the ladder
        var dismountDirection = ClimbingRotation.Backward;
        Player.Body.Velocity += dismountDirection * DismountVelocity;

        // Clear the climbing object to immediately exit the state
        ClimbingCollider = null;
    }
}
