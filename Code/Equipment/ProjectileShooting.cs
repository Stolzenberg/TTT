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
        if (!Ray.HasValue)
        {
            return;
        }

        for (var i = 0; i < ProjectileCount; i++)
        {
            SpawnProjectile(Ray.Value.Position, Ray.Value.Forward);
        }
    }

    [Rpc.Host]
    private void SpawnProjectile(Vector3 worldPosition, Vector3 direction)
    {
        if (!ProjectilePrefab.IsValid())
        {
            Log.Error("ProjectileShooting: No ProjectilePrefab assigned!");

            return;
        }

        // Calculate direction with spread
        if (ProjectileSpread > 0)
        {
            var spread = (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * ProjectileSpread * 0.25f;
            direction += spread;
            direction = direction.Normal;
        }

        // Spawn the projectile
        var gameObject = ProjectilePrefab.Clone(new CloneConfig
        {
            Transform = new(worldPosition, Rotation.LookAt(direction)),
            StartEnabled = true,
            Name = $"Projectile from {Equipment.GameObject.Name}",
        });

        var projectileComponent = gameObject.GetComponent<Projectile>();

        projectileComponent.Owner = Equipment.Owner;
        projectileComponent.Inflictor = Equipment;

        // Calculate initial velocity (projectile speed + inherited velocity from owner)
        var ownerVelocity = Vector3.Zero;
        if (Equipment.Owner.IsValid())
        {
            ownerVelocity = Equipment.Owner.Velocity;
        }

        projectileComponent.InitialVelocity = direction * ProjectileSpeed + ownerVelocity;

        gameObject.NetworkSpawn();
    }
}