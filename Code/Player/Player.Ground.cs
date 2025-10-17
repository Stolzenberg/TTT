using System;

namespace Mountain;

public sealed partial class Player
{
    public Vector3 GroundVelocity { get; set; }
    public bool IsOnGround => GroundObject.IsValid();
    
    [Sync]
    public GameObject? GroundObject { get; set; }
    public Component? GroundComponent { get; set; }

    public float GroundFriction { get; set; }
    public Vector3 GroundNormal { get; set; }

    /// <summary>
    ///     Are we standing on a surface that is physically dynamic
    /// </summary>
    public bool GroundIsDynamic { get; set; }
    public TimeSince TimeSinceGrounded { get; private set; } = 0;
    public TimeSince TimeSinceUngrounded { get; private set; } = 0;

    private TimeUntil timeUntilAllowedGround = 0;

    public void ClearGround()
    {
        GroundObject = null;
    }

    private void CheckGround()
    {
        if (Mode == null)
        {
            return;
        }
        
        var groundVel = GroundVelocity.z;

        // Ground is pushing us crazy, stop being grounded
        if (groundVel > 250)
        {
            PreventGrounding(0.3f);
            UpdateGroundFromTraceResult(default);

            return;
        }

        var velocity = Velocity - GroundVelocity;
        if (timeUntilAllowedGround > 0 || velocity.Length > 500)
        {
            UpdateGroundFromTraceResult(default);

            return;
        }

        var from = WorldPosition + Vector3.Up * 4;
        var to = WorldPosition + Vector3.Down * 8;

        var radiusScale = 1f;
        var tr = TraceBody(from, to, radiusScale, 0.5f);

        while (tr.StartedSolid || tr.Hit && !Mode.IsStandableSurface(tr))
        {
            radiusScale -= 0.1f;
            if (radiusScale < 0.7f)
            {
                UpdateGroundFromTraceResult(default);

                return;
            }

            tr = TraceBody(from, to, radiusScale, 0.5f);
        }

        if (tr is { StartedSolid: false, Hit: true } && Mode.IsStandableSurface(tr))
        {
            UpdateGroundFromTraceResult(tr);
        }
        else
        {
            UpdateGroundFromTraceResult(default);
        }
    }

    private void PreventGrounding(float seconds)
    {
        timeUntilAllowedGround = MathF.Max(timeUntilAllowedGround, seconds);
        UpdateGroundFromTraceResult(default);
    }

    private void UpdateGroundVelocity()
    {
        if (GroundObject is null)
        {
            GroundVelocity = 0;

            return;
        }

        if (GroundComponent is Collider collider)
        {
            GroundVelocity = collider.GetVelocityAtPoint(WorldPosition);
        }

        if (GroundComponent is Rigidbody rigidbody)
        {
            var ourMass = Body.Mass;
            var groundMass = rigidbody.Mass;
            var massFactor = groundMass / (ourMass + groundMass);
            GroundVelocity = rigidbody.GetVelocityAtPoint(WorldPosition) * massFactor;
        }
    }

    private void UpdateGroundFromTraceResult(SceneTraceResult tr)
    {
        var body = tr.Body;

        GroundObject = body?.GameObject;
        GroundComponent = body?.Component;
        GroundIsDynamic = true;

        if (GroundObject is not null)
        {
            TimeSinceGrounded = 0;
            GroundNormal = tr.Normal;
            GroundFriction = tr.Surface.Friction;

            if (tr.Component is Collider collider)
            {
                if (collider.Friction.HasValue)
                {
                    GroundFriction = collider.Friction.Value;
                }

                GroundIsDynamic = collider.IsDynamic;
            }
        }
        else
        {
            TimeSinceUngrounded = 0;
        }
    }
}