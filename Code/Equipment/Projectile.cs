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
    public bool ExplodesOnImpact { get; set; } = false;

    [Property]
    public float ExplosionRadius { get; set; } = 0f;

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
    public float GravityScale { get; set; } = 1f;

    [RequireComponent]
    public Rigidbody Rigidbody { get; set; }
    private bool hasHit;

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
    }

    protected override void OnFixedUpdate()
    {
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
        hasHit = true;

        var contact = collision.Contact;
        var hitPosition = contact.Point;
        var hitNormal = contact.Normal;

        if (ImpactEffectPrefab.IsValid())
        {
            _ = ImpactEffectPrefab.Clone(new CloneConfig
            {
                Transform = new(hitPosition, Rotation.LookAt(hitNormal)),
                StartEnabled = true,
            });
        }

        if (ImpactSound.IsValid())
        {
            Sound.Play(ImpactSound, hitPosition);
        }

        if (ExplodesOnImpact && ExplosionRadius > 0)
        {
            ApplyExplosionDamage(hitPosition);
        }
        else
        {
            ApplyDirectDamage(collision);
        }

        DestroyProjectile();
    }

    private void ApplyDirectDamage(Collision collision)
    {
        var hitObject = collision.Other.GameObject;
        if (!hitObject.IsValid())
        {
            return;
        }

        var health = hitObject.Root.GetComponentInChildren<Health>();
        if (!health.IsValid())
        {
            return;
        }

        var contact = collision.Contact;

        hitObject.ServerTakeDamage(new()
        {
            Attacker = Owner,
            Victim = health,
            Inflictor = Inflictor,
            Position = contact.Point,
            Damage = BaseDamage,
            Force = Rigidbody.Velocity.Normal * Force,
        });
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

    private void DestroyProjectile()
    {
        GameObject.Destroy();
    }
}