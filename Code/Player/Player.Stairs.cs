using System;

namespace Mountain;

public sealed partial class Player
{
    [Property, Group("Movement")]
    public float MaxStepHeight { get; set; } = 24f;

    [Property, Group("Movement")]
    public float StepSmoothSpeed { get; set; } = 200f;
    private float currentStepOffset;

    private float targetStepHeight;

    private void HandleStairs()
    {
        if (currentStepOffset > 0)
        {
            currentStepOffset = Math.Max(0, currentStepOffset - StepSmoothSpeed * Time.Delta);
            WorldPosition = WorldPosition.WithZ(WorldPosition.z - (currentStepOffset * Time.Delta));

            return;
        }

        var moveDirection = WishVelocity.WithZ(0).Normal;
        if (moveDirection.IsNearZeroLength)
        {
            return;
        }

        var heightPercentage = 0.2f;
        var colliderLowerHeight = BodyHeight * heightPercentage;
        var checkDistance = BodyRadius + 2f; // Check slightly ahead

        var lowerCheckStart = WorldPosition + Vector3.Up * colliderLowerHeight;
        var lowerCheckEnd = lowerCheckStart + moveDirection * checkDistance;

        var lowerTrace = TraceBody(lowerCheckStart, lowerCheckEnd, 2f, heightPercentage).WithTag("World").Run();

        // Debug visualization
        DebugOverlay.Line(lowerCheckStart, lowerCheckEnd, lowerTrace.Hit ? Color.Yellow : Color.Green);

        // If no collision in lower area, nothing to step over
        if (!lowerTrace.Hit)
        {
            return;
        }

        // Found collision in lower area - now check if we can step onto it
        // Trace forward from above the obstacle to see if we can step onto it
        var stepCheckHeight = MaxStepHeight;
        var forwardCheckStart = WorldPosition + moveDirection * checkDistance + Vector3.Up * stepCheckHeight;
        var forwardCheckEnd = forwardCheckStart + Vector3.Down * (stepCheckHeight + 16f);

        var stepTrace = TraceBody(forwardCheckStart, forwardCheckEnd, 2f).WithTag("World").Run();

        // DebugOverlay.Line(forwardCheckStart, forwardCheckEnd, stepTrace.Hit ? Color.Cyan : Color.Red);

        if (!stepTrace.Hit)
        {
            return; // No surface to step onto
        }

        // Calculate step height
        var stepHeight = stepTrace.EndPosition.z - WorldPosition.z;

        // Validate the step is within acceptable range
        if (stepHeight <= 2f || stepHeight > MaxStepHeight)
        {
            return; // Too small or too tall
        }

        // Check if the surface is walkable (not too steep)
        var angle = Vector3.GetAngle(stepTrace.Normal, Vector3.Up);
        if (angle > 30f)
        {
            return; // Too steep
        }

        // Check if there's enough vertical space for the player's body at the step destination
        var potentialStepPosition = stepTrace.EndPosition;
        var bodySpaceCheckStart = potentialStepPosition + Vector3.Up * BodyHeight;
        var bodySpaceCheckEnd = bodySpaceCheckStart + Vector3.Up * BodyHeight;

        var bodySpaceTrace = TraceBody(bodySpaceCheckStart, bodySpaceCheckEnd).WithTag("World").Run();

        // DebugOverlay.Line(bodySpaceCheckStart, bodySpaceCheckEnd, bodySpaceTrace.Hit ? Color.Red : Color.Blue);

        if (bodySpaceTrace.Hit)
        {
            return; // Not enough vertical space for the player's body
        }

        // Debug visualization of the step
        // DebugOverlay.Sphere(new (stepTrace.EndPosition, 2f), Color.Green, 3f);

        // Smoothly step up
        targetStepHeight = stepHeight;
        var stepAmount = Math.Min(StepSmoothSpeed * Time.Delta, targetStepHeight - currentStepOffset);

        if (stepAmount > 0)
        {
            // WorldPosition += Vector3.Up * stepAmount;
            WorldPosition += Vector3.Up * stepAmount;
            currentStepOffset += stepAmount;
        }

        // Reset step offset when we've reached the target
        if (currentStepOffset >= targetStepHeight - heightPercentage)
        {
            currentStepOffset = 0;
            targetStepHeight = 0;
        }
    }
}