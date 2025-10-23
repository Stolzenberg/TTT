using System;

namespace Mountain;

/// <summary>The character is swimming</summary>
public sealed class SwimMovementState : MovementState
{
    [Property]
    public new int Priority { get; set; } = 10;

    [Property, Range(0f, 1f)]
    public float SwimLevel { get; set; } = 0.7f;

    [Property]
    public float SwimSpeed { get; set; } = 100f;

    /// <summary>
    /// Will update this based on how much you're in a "water" tagged trigger
    /// </summary>
    public float WaterLevel { get; private set; }

    [Sync]
    public bool IsSwimming { get; private set; }

    public override void UpdateRigidBody(Rigidbody body)
    {
        body.Gravity = false;
        body.LinearDamping = 3.3f;
        body.AngularDamping = 1f;
    }

    public override int Score(Player player)
    {
        return WaterLevel > SwimLevel ? Priority : -100;
    }

    public override void OnStateBegin()
    {
        IsSwimming = true;
    }

    public override void OnStateEnd(MovementState? next)
    {
        IsSwimming = false;

        // Jump out of water when exiting swim mode
        if (Input.Down("Jump"))
        {
            Player.Body.Velocity += Vector3.Up * 300f;
        }
    }

    public override Vector3 UpdateState(Rotation eyes, Vector3 input)
    {
        // Allow swimming up when jump is pressed
        if (Input.Down("Jump"))
        {
            input += Vector3.Up;
        }

        if (Input.Down("Duck"))
        {
            input += Vector3.Down;
        }

        // For swimming, we want to move in the direction we're looking (including pitch)
        // Don't use base.UpdateState as it zeros out pitch
        var direction = (eyes * input).Normal;

        if (direction.IsNearlyZero(0.1f))
        {
            return Vector3.Zero;
        }

        return direction * GetSpeed();
    }

    protected override void OnFixedUpdate()
    {
        UpdateWaterLevel();
    }

    protected override float GetSpeed()
    {
        return SwimSpeed;
    }

    private void UpdateWaterLevel()
    {
        var worldTransform = Player.WorldTransform;
        var topPoint = worldTransform.PointToWorld(new(0f, 0f, Player.BodyHeight));
        var bottomPoint = worldTransform.Position;
        var maxWaterLevel = 0f;

        foreach (var collider in Player.Body.Touching)
        {
            if (!collider.Tags.Contains("water"))
            {
                continue;
            }

            var closestPoint = collider.FindClosestPoint(topPoint);
            var waterAmount = Vector3.InverseLerp(closestPoint, bottomPoint, topPoint);
            var roundedAmount = MathF.Ceiling(waterAmount * 100f) / 100f;

            if (roundedAmount > maxWaterLevel)
            {
                maxWaterLevel = roundedAmount;
            }
        }

        WaterLevel = maxWaterLevel;
    }
}