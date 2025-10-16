using System;

namespace Mountain;

public sealed class SlideMovementState : MovementState
{
    [Property]
    private readonly float colliderSize = 32f;
    
    [Property]
    private readonly float headHeightModifier = 0.5f;

    [Property]
    private readonly float minSlideAngle = 30f;

    [Property]
    private readonly float minSpeed = 300f;

    [Property]
    private readonly float maxSpeed = 400f;

    [Property]
    private readonly float maxAngle = 60f;

    [Property, Range(0f, 1f)]
    private readonly float movementControl = 0.3f;

    [Property]
    private readonly float minSpeedForDuckSlide = 300f;

    [Property]
    private readonly float minAngleForDuckSlide = 15f;

    private float defaultSize;
    private float currentGroundAngle;

    public override int Score(Player playerMovement)
    {
        // Check if player is on ground
        if (!Player.IsOnGround)
        {
            return 0;
        }

        // Get the ground normal from GroundTransform
        var groundNormal = Player.GroundNormal;

        // Calculate the angle between ground normal and world up
        var angle = Vector3.GetAngle(groundNormal, Vector3.Up);

        // Store the current angle for GetSpeed calculation
        currentGroundAngle = angle;
        
        // If the slope is steep enough, activate slide state
        if (angle >= minSlideAngle)
        {
            return 150; // Higher priority than crouch/run
        }
        
        // Check if player is ducking with enough speed to initiate a slide
        if (Input.Down("Duck") 
            && angle >= minAngleForDuckSlide)
        {
            var velocity = Player.Body.Velocity - Player.GroundVelocity;
            var speed = velocity.Length;
            
            if (speed >= minSpeedForDuckSlide)
            {
                return 150; // Same priority as slope-based slide
            }
        }
        
        return 0;
    }

    public override void AddVelocity()
    {
        var body = Player.Body;
        var wish = Player.WishVelocity;
        var groundFriction = Player.GroundFriction;
        var groundVelocity = Player.GroundVelocity;

        var z = body.Velocity.z;

        var velocity = body.Velocity - Player.GroundVelocity;
        var speed = velocity.Length;

        // Get the slide direction (down the slope)
        var groundNormal = Player.GroundNormal;
        var slideDirection = (Vector3.Down - groundNormal * Vector3.Dot(Vector3.Down, groundNormal)).Normal;
        
        // Calculate the base slide velocity (momentum down the slope)
        var slideSpeed = GetSpeed();
        var slideVelocity = slideDirection * slideSpeed;
        
        if (Vector3.Dot(wish, slideDirection) < 0)
        {
            wish -= slideDirection * Vector3.Dot(wish, slideDirection);
        }
        
        // Apply movement control to allow some steering
        var controlVelocity = wish * movementControl;
        
        // Combine slide momentum with controlled movement
        var targetVelocity = slideVelocity + controlVelocity;
        
        var maxVelocity = MathF.Max(targetVelocity.Length, speed);

        if (Player.IsOnGround)
        {
            velocity = velocity.AddClamped(targetVelocity * groundFriction, targetVelocity.Length * groundFriction);
        }
        else
        {
            var amount = 0.05f;
            velocity = velocity.AddClamped(targetVelocity * amount, targetVelocity.Length);
        }

        if (velocity.Length > maxVelocity)
        {
            velocity = velocity.Normal * maxVelocity;
        }

        velocity += groundVelocity;
                
        if (Player.IsOnGround)
        {
            velocity.z = z;
        }

        body.Velocity = velocity;
    }

    protected override float GetSpeed()
    {
        // Calculate speed based on slope angle
        // minSlideAngle = minSpeed, maxAngle = maxSpeed
        var angleRange = maxAngle - minSlideAngle;
        var angleProgress = (currentGroundAngle - minSlideAngle) / angleRange;
        
        // Clamp between 0 and 1
        angleProgress = angleProgress.Clamp(0f, 1f);
        
        // Interpolate between min and max speed
        return minSpeed + (maxSpeed - minSpeed) * angleProgress;
    }

    public override void OnStateBegin()
    {
        base.OnStateBegin();
        defaultSize = Player.Collider.Start.z;
        Player.Collider.Start = new(0, 0, colliderSize);
        Player.SetHeadPosition(headHeightModifier);
    }

    public override void OnStateEnd(MovementState? next)
    {
        base.OnStateEnd(next);
        Player.Collider.Start = new(0, 0, defaultSize);
        Player.SetHeadPosition();
    }
}
