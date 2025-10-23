using System;
using Sandbox.Citizen;

namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("Animator")]
    public SkinnedModelRenderer BodyRenderer { get; init; } = null!;
    private const float RotationSpeedUpdatePeriod = 0.1f;
    [Property, Feature("Animator"), Category("Aim"), Range(0, 1)]
    private readonly float aimStrengthEyes = 0.5f;
    [Property, Feature("Animator"), Category("Aim"), Range(0, 1)]
    private readonly float aimStrengthHead = 0.5f;
    [Property, Feature("Animator"), Category("Aim"), Range(0, 1)]
    private readonly float aimStrengthBody = 0.5f;
    [Property, Feature("Animator"), Category("Rotation"), Range(0, 90)]
    private readonly float rotationAngleLimit = 45.0f;
    [Property, Feature("Animator"), Category("Rotation"), Range(0, 2)]
    private readonly float rotationSpeed = 1.0f;
    [Property, Feature("Animator"), Category("Rotation")]
    private readonly float rotationSpeedModifier = 5.0f;

    private float animRotationSpeed;
    private Vector3.SmoothDamped smoothedMove = new(0, 0, 0.5f);
    private Vector3.SmoothDamped smoothedSkid = new(0, 0, 0.5f);
    private Vector3.SmoothDamped smoothedWish = new(0, 0, 0.5f);
    private TimeSince timeSinceRotationSpeedUpdate;

    public void TriggerJump()
    {
        BodyRenderer.Set("b_jump", true);
    }

    private void ProceduralHitReaction(float damageScale = 1.0f, Vector3 force = default)
    {
        const int boneId = 0;
        var tx = BodyRenderer.GetBoneObject(boneId);

        if (!tx.IsValid())
        {
            return;
        }

        var localToBone = tx.Transform.Local.Position;
        if (localToBone == Vector3.Zero)
        {
            localToBone = Vector3.One;
        }

        BodyRenderer.Set("hit", true);
        BodyRenderer.Set("hit_bone", boneId);
        BodyRenderer.Set("hit_offset", localToBone);
        BodyRenderer.Set("hit_direction", force.Normal);
        BodyRenderer.Set("hit_strength", force.Length / 1000.0f * damageScale);
    }

    private void UpdateVelocity()
    {
        var rot = BodyRenderer.WorldRotation;
        var vel = Velocity;
        var wishVel = WishVelocity;

        HandleSkid(vel, wishVel, rot);
        HandleLegs(wishVel, rot);
        HandleWish(rot, wishVel);
    }

    private void ApplyAnimationParameters()
    {
        var holdType = CitizenAnimationHelper.HoldTypes.None;
        if (ActiveEquipment != null)
        {
            holdType = ActiveEquipment.HoldType;
        }

        BodyRenderer.Set("holdtype", (int)holdType);
        BodyRenderer.Set("b_noclip", Mode is NoClipMovementState);
        BodyRenderer.Set("duck", Mode is CrouchMovementState ? 1 : 0);
        BodyRenderer.Set("special_movement_states", Mode is SlideMovementState ? 3 : 0);
        BodyRenderer.Set("b_swim", Mode is SwimMovementState ? 0 : 1);
    }

    private void HandleWish(Rotation rot, Vector3 wishVel)
    {
        var localVelocity = GetLocalVelocity(rot, wishVel);

        BodyRenderer.Set("wish_direction", GetAngle(localVelocity));
        BodyRenderer.Set("wish_speed", wishVel.Length);
        BodyRenderer.Set("wish_groundspeed", wishVel.WithZ(0f).Length);
        BodyRenderer.Set("wish_x", localVelocity.x);
        BodyRenderer.Set("wish_y", localVelocity.y);
        BodyRenderer.Set("wish_z", localVelocity.z);
    }

    private void HandleLegs(Vector3 wishVel, Rotation rot)
    {
        var smoothed = wishVel;
        {
            smoothedWish.Target = smoothed;
            smoothedWish.SmoothTime = 0.6f;
            smoothedWish.Update(Time.Delta);
            smoothed = smoothedWish.Current;

            // Stop walking if we're too slow
            smoothed = ApplyDeadZone(smoothed, 10);
        }

        smoothed = GetLocalVelocity(rot, smoothed);
        BodyRenderer.Set("move_direction", GetAngle(smoothed));
        BodyRenderer.Set("move_speed", smoothed.Length);
        BodyRenderer.Set("move_groundspeed", smoothed.WithZ(0f).Length);
        BodyRenderer.Set("move_x", smoothed.x);
        BodyRenderer.Set("move_y", smoothed.y);
        BodyRenderer.Set("move_z", smoothed.z);
    }

    private void HandleSkid(Vector3 vel, Vector3 wishVel, Rotation rot)
    {
        const float skidAmount = 0.5f; // multiplier for moving skid
        const float pushSkidAmount = 1.0f; // multiplier for sliding down a slope when standing still

        const float skidDelay = 0.2f; // in seconds, longer means bigger gap between the velocities, more skidding

        // smooth version of our velocity
        var smoothed = vel;
        {
            smoothedMove.Target = smoothed;
            smoothedMove.SmoothTime = skidDelay;
            smoothedMove.Update(Time.Delta);
            smoothed = smoothedMove.Current;
        }

        // skid is the difference between our old velocity and our current velocity
        var skid = (smoothed - vel) * skidAmount;

        // if we're standing still, use our actual velity as the skid
        skid = Vector3.Lerp(skid, vel * pushSkidAmount, wishVel.Length.Remap(100, 0));

        // smooth our skidders
        smoothedSkid.Target = skid;
        smoothedSkid.SmoothTime = 0.5f;
        smoothedSkid.Update(Time.Delta);
        skid = smoothedSkid.Current;

        // convert to model space
        skid = GetLocalVelocity(rot, skid);

        BodyRenderer.Set("skid_x", skid.x / 1200.0f);
        BodyRenderer.Set("skid_y", skid.y / 1200.0f);
    }

    private void UpdateRotation()
    {
        var targetAngle = Rotation.FromYaw(EyeAngles.yaw);
        var velocity = WishVelocity.WithZ(0);

        var rotateDifference = BodyRenderer.WorldRotation.Distance(targetAngle);
        var oldRotation = BodyRenderer.WorldRotation;

        // We're over the limit - snap it 
        if (rotateDifference > rotationAngleLimit)
        {
            var delta = 1f - rotationAngleLimit / rotateDifference;
            var newRotation = Rotation.Lerp(BodyRenderer.WorldRotation, targetAngle, delta);

            BodyRenderer.WorldRotation = newRotation;
        }

        if (velocity.Length > 1)
        {
            var rotationFactor = Time.Delta * rotationSpeed * velocity.Length.Remap(0, 100);
            rotationFactor = rotationFactor.Clamp(0, 1); // Ensure the factor stays within valid bounds

            BodyRenderer.WorldRotation = Rotation.Slerp(BodyRenderer.WorldRotation, targetAngle, rotationFactor);
        }


        var oldYaw = oldRotation.Angles().yaw;
        var newYaw = BodyRenderer.WorldRotation.Angles().yaw;

        var deltaYaw = MathX.DeltaDegrees(newYaw, oldYaw);
        animRotationSpeed = (animRotationSpeed + deltaYaw).Clamp(-90f, 90f);

        if (timeSinceRotationSpeedUpdate < RotationSpeedUpdatePeriod)
        {
            return;
        }

        BodyRenderer.Set("move_rotationspeed", animRotationSpeed * rotationSpeedModifier);

        timeSinceRotationSpeedUpdate = 0f;
        animRotationSpeed = 0f;
    }

    private void UpdateEyes()
    {
        var eyeAngles = EyeAngles;

        BodyRenderer.SetLookDirection("aim_eyes", eyeAngles.Forward, aimStrengthEyes);
        BodyRenderer.SetLookDirection("aim_head", eyeAngles.Forward, aimStrengthHead);
        BodyRenderer.SetLookDirection("aim_body", eyeAngles.Forward, aimStrengthBody);
    }

    private static Vector3 GetLocalVelocity(Rotation rotation, Vector3 worldVelocity)
    {
        var forward = rotation.Forward.Dot(worldVelocity);
        var sideward = rotation.Right.Dot(worldVelocity);

        return new(forward, sideward, worldVelocity.z);
    }

    private static Vector3 ApplyDeadZone(Vector3 velocity, float minimum)
    {
        return velocity.IsNearlyZero(minimum) ? 0f : velocity;
    }

    private static float GetAngle(Vector3 localVelocity)
    {
        return MathF.Atan2(localVelocity.y, localVelocity.x).RadianToDegree().NormalizeDegrees();
    }
}