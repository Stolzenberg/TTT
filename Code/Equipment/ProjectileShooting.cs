namespace Mountain;

/// <summary>
/// Implements projectile-based shooting behavior.
/// Spawns physical projectiles that travel through the world.
/// </summary>
public sealed class ProjectileShooting : ShootingBehavior
{
    [Property, Feature("Projectile")]
    public GameObject? ProjectilePrefab { get; init; }

    [Property, Feature("Projectile")]
    public float ProjectileSpeed { get; init; } = 1000f;

    [Property, Feature("Projectile")]
    public float ProjectileSpread { get; init; } = 0f;

    [Property, Feature("Projectile")]
    public int ProjectileCount { get; init; } = 1;

    protected override void PerformShoot()
    {
        Log.Info("perform shoot projectile");

        for (var i = 0; i < ProjectileCount; i++)
        {
            SpawnProjectile();
        }
    }

    [Rpc.Host]
    private void SpawnProjectile()
    {
        if (!ProjectilePrefab.IsValid())
        {
            Log.Warning("ProjectileShooting: No ProjectilePrefab assigned!");

            return;
        }

        var muzzle = Visual.Muzzle;
        if (!muzzle.IsValid())
        {
            Log.Warning("ProjectileShooting: No muzzle found on visual model!");

            return;
        }

        // Calculate direction with spread
        var direction = Ray.Forward;
        if (ProjectileSpread > 0)
        {
            var spread = (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * ProjectileSpread * 0.25f;
            direction += spread;
            direction = direction.Normal;
        }

        // Spawn the projectile
        var projectile = ProjectilePrefab.Clone(new CloneConfig
        {
            Transform = new Transform(muzzle.WorldPosition, Rotation.LookAt(direction)),
            StartEnabled = true,
            Name = $"Projectile from {Equipment.GameObject.Name}",
        });

        // Configure the projectile component
        if (projectile.Components.Get<Projectile>() is { } projectileComponent)
        {
            projectileComponent.Owner = Equipment.Owner;
            projectileComponent.Inflictor = Equipment;

            // Calculate initial velocity (projectile speed + inherited velocity from owner)
            var ownerVelocity = Vector3.Zero;
            if (Equipment.Owner.IsValid())
            {
                ownerVelocity = Equipment.Owner.Velocity;
            }

            projectileComponent.InitialVelocity = direction * ProjectileSpeed + ownerVelocity;
        }
        else
        {
            Log.Warning("ProjectileShooting: ProjectilePrefab does not have a Projectile component!");
        }
    }
}