namespace Mountain;

public sealed class Projectile : Component, Component.ICollisionListener
{
    [Property]
    public float BaseDamage { get; set; } = 50f;

    [Property]
    public float Force { get; set; } = 500f;

    [Property]
    public float MaxLifetime { get; set; } = 10f;

    [Property]
    public ExplosionMode ExplosionMode { get; set; } = ExplosionMode.None;

    [Property]
    public float ExplosionRadius { get; set; } = 0f;

    [Property, ShowIf(nameof(ExplosionMode), ExplosionMode.OnTime)]
    public float ExplosionDelay { get; set; } = 3f;

    [Property, ShowIf(nameof(ExplosionMode), ExplosionMode.OnProximity)]
    public float ProximityTriggerRadius { get; set; } = 5f;

    [Property]
    public GameObject? ImpactEffectPrefab { get; set; }

    [Property]
    public SoundEvent? ImpactSound { get; set; }

    [Property, Group("Debug"), Description("Enable debug visualization and logging.")]
    public bool EnableDebug { get; set; } = false;

    [Sync]
    public Player Owner { get; set; }

    [Sync]
    public Component? Inflictor { get; set; }

    public Vector3 InitialVelocity { get; set; }
    public Vector3 InitialAngularVelocity { get; set; }
    public float GravityScale { get; set; } = 1f;

    [RequireComponent]
    public Rigidbody Rigidbody { get; set; }
    private bool hasExploded;
    private bool hasHit;
    private TimeSince timeSinceHit;

    private TimeSince timeSinceSpawned;

    void ICollisionListener.OnCollisionStart(Collision collision)
    {
        if (hasHit)
        {
            return;
        }

        // Don't hit the owner
        if (Owner.IsValid() && collision.Other.GameObject.Root == Owner.GameObject)
        {
            return;
        }

        OnHit(collision);
    }

    protected override void OnAwake()
    {
        if (!Rigidbody.IsValid())
        {
            Rigidbody = Components.GetOrCreate<Rigidbody>();
        }

        Rigidbody.Gravity = false;
        Rigidbody.AngularDamping = 0f;
        Rigidbody.LinearDamping = 0f;
    }

    protected override void OnStart()
    {
        timeSinceSpawned = 0;

        Rigidbody.Velocity = InitialVelocity;
        Rigidbody.AngularVelocity = InitialAngularVelocity;
    }

    protected override void OnFixedUpdate()
    {
        if (hasExploded)
        {
            return;
        }

        // Handle OnTime explosion mode
        if (ExplosionMode == ExplosionMode.OnTime && timeSinceSpawned > ExplosionDelay)
        {
            CreateVisuals(WorldPosition, WorldRotation);
            ApplyExplosionDamage(WorldPosition);
            hasExploded = true;
            DestroyProjectile();

            return;
        }

        // Handle OnProximity explosion mode
        if (ExplosionMode == ExplosionMode.OnProximity && hasHit)
        {
            CheckProximityTrigger();
        }

        if (hasHit)
        {
            return;
        }

        if (timeSinceSpawned > MaxLifetime)
        {
            DestroyProjectile();

            return;
        }

        if (GravityScale > 0)
        {
            Rigidbody.Velocity += Scene.PhysicsWorld.Gravity * GravityScale * Time.Delta;
        }

        if (Rigidbody.Velocity.Length > 0.1f)
        {
            WorldRotation = Rotation.LookAt(Rigidbody.Velocity.Normal);
        }
    }

    private void OnHit(Collision collision)
    {
        if (ExplosionMode != ExplosionMode.OnImpact)
        {
            return;
        }

        hasHit = true;
        timeSinceHit = 0;

        var contact = collision.Contact;
        var hitPosition = contact.Point;
        var hitNormal = contact.Normal;

        CreateVisuals(hitPosition, Rotation.LookAt(hitNormal));

        ApplyExplosionDamage(hitPosition);
        DestroyProjectile();
    }

    private void CreateVisuals(Vector3 hitPosition, Rotation rotation)
    {
        if (ImpactEffectPrefab.IsValid())
        {
            var gameObject = ImpactEffectPrefab.Clone(new CloneConfig
            {
                Transform = new(hitPosition, rotation),
                StartEnabled = true,
            });

            gameObject.AddComponent<DestroyBetweenRounds>();

            gameObject.NetworkSpawn();
        }

        if (ImpactSound.IsValid())
        {
            Sound.Play(ImpactSound, hitPosition);
        }
    }

    private void ApplyExplosionDamage(Vector3 center)
    {
        var overlaps = Scene.FindInPhysics(new Sphere(center, ExplosionRadius));

        if (EnableDebug)
        {
            DebugOverlay.Sphere(new(center, ExplosionRadius), Color.Red, 1);
        }

        foreach (var overlap in overlaps)
        {
            if (!overlap.IsValid())
            {
                continue;
            }

            var health = overlap.Root.GetComponentInChildren<Health>();
            if (!health.IsValid())
            {
                continue;
            }

            var distance = Vector3.DistanceBetween(center, overlap.WorldPosition);
            var falloff = 1f - distance / ExplosionRadius;
            var damage = BaseDamage * falloff;

            var direction = (overlap.WorldPosition - center).Normal;

            overlap.ServerTakeDamage(new()
            {
                Attacker = Owner,
                Victim = health,
                Inflictor = Inflictor,
                Position = overlap.WorldPosition,
                Damage = damage,
                Force = direction * Force * falloff,
            });
        }
    }

    private void CheckProximityTrigger()
    {
        if (hasExploded)
        {
            return;
        }

        var overlaps = Scene.FindInPhysics(new Sphere(WorldPosition, ProximityTriggerRadius));

        foreach (var overlap in overlaps)
        {
            if (!overlap.IsValid())
            {
                continue;
            }

            // Don't trigger on the owner
            if (Owner.IsValid() && overlap.Root == Owner.GameObject)
            {
                continue;
            }

            // Don't trigger on self
            if (overlap.Root == GameObject)
            {
                continue;
            }

            // Check if this is a valid target (has health)
            var health = overlap.Root.GetComponentInChildren<Health>();
            if (health.IsValid())
            {
                CreateVisuals(WorldPosition, WorldRotation);
                ApplyExplosionDamage(WorldPosition);
                hasExploded = true;
                DestroyProjectile();

                return;
            }
        }
    }

    private void DestroyProjectile()
    {
        GameObject.Destroy();
    }
}