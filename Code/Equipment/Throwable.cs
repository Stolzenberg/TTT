namespace Mountain;

public sealed class Throwable : EquipmentInputAction
{
    [Property]
    public GameObject? ProjectilePrefab { get; init; }

    [Property]
    public float ThrowForce { get; init; } = 1000f;

    [Property]
    public float ThrowAngle { get; init; } = 30f;

    [Property]
    public float ThrowGravityScale { get; init; } = 1f;

    [Property]
    public float ThrowCooldown { get; init; } = 0.5f;

    [Property]
    public SoundEvent? ThrowSound { get; init; }

    [Sync]
    public TimeSince TimeSinceThrow { get; private set; }

    [Sync]
    public bool IsThrowing { get; private set; }

    [Sync]
    public TimeUntil ThrowEnds { get; private set; }

    protected override void OnInputDown()
    {
        if (CanThrow())
        {
            Throw();
        }
    }

    protected override void OnFixedUpdate()
    {
        base.OnFixedUpdate();

        if (IsThrowing && ThrowEnds)
        {
            IsThrowing = false;
        }
    }

    private bool CanThrow()
    {
        if (!Equipment.IsValid() || !Equipment.Owner.IsValid())
        {
            return false;
        }

        if (Equipment.Tags.Has("no_throwing") || Equipment.Tags.Has("reloading"))
        {
            return false;
        }

        if (IsThrowing)
        {
            return false;
        }

        if (TimeSinceThrow < ThrowCooldown)
        {
            return false;
        }

        return true;
    }

    private void Throw()
    {
        Log.Info("throw");
        if (!ProjectilePrefab.IsValid())
        {
            Log.Warning("Throwable: No ProjectilePrefab assigned!");

            return;
        }

        TimeSinceThrow = 0;
        IsThrowing = true;
        ThrowEnds = 0.3f;

        ThrowEffects();
        SpawnProjectile();

        if (Equipment.Owner.IsValid() && Equipment.Owner.BodyRenderer.IsValid())
        {
            Equipment.Owner.BodyRenderer.Set("b_attack", true);
        }

        if (Equipment.ViewModel.IsValid())
        {
            Equipment.ViewModel.ModelRenderer.Set("b_attack", true);
        }

        GameObject.Destroy();
    }

    [Rpc.Host]
    private void SpawnProjectile()
    {
        if (!ProjectilePrefab.IsValid())
        {
            return;
        }

        var eyePos = Equipment.Owner.EyePosition;
        var eyeAngles = Equipment.Owner.EyeAngles;

        // Apply throw angle (arc upward) by adjusting pitch
        eyeAngles.pitch -= ThrowAngle;
        var throwRotation = Rotation.From(eyeAngles);
        var throwDirection = throwRotation.Forward;

        // Spawn position slightly in front of player
        var spawnPos = eyePos + eyeAngles.Forward * 50f;

        var projectile = ProjectilePrefab.Clone(new CloneConfig
        {
            Transform = new(spawnPos, Rotation.LookAt(throwDirection)),
            StartEnabled = true,
            Name = $"Thrown {Equipment.GameObject.Name}",
        });

        if (projectile.Components.Get<Projectile>() is { } projectileComponent)
        {
            projectileComponent.Owner = Equipment.Owner;
            projectileComponent.Inflictor = Equipment;

            var ownerVelocity = Equipment.Owner.IsValid() ? Equipment.Owner.Velocity : Vector3.Zero;
            projectileComponent.InitialVelocity = throwDirection * ThrowForce + ownerVelocity;
            projectileComponent.InitialAngularVelocity = throwDirection * ThrowForce + ownerVelocity;
            projectileComponent.GravityScale = ThrowGravityScale;
        }
        else
        {
            Log.Warning("Throwable: ProjectilePrefab does not have a Projectile component!");
        }
    }

    [Rpc.Broadcast]
    private void ThrowEffects()
    {
        if (ThrowSound.IsValid())
        {
            var snd = Sound.Play(ThrowSound, Equipment.WorldPosition);
            if (snd.IsValid())
            {
                snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
            }
        }
    }
}