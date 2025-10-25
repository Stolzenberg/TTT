using Sandbox.Events;

namespace Mountain;

public record EquipmentMeleeAttackEvent : IGameEvent;

public sealed class MeleeAttack : EquipmentInputAction
{
    [Property]
    public float BaseDamage { get; init; } = 50f;

    [Property]
    public float AttackRate { get; init; } = 1.0f;

    [Property]
    public float AttackDuration { get; init; } = 0.3f;

    [Property]
    public float AttackRange { get; init; } = 100f;

    [Property]
    public float AttackRadius { get; init; } = 30f;

    [Property]
    public float Force { get; init; } = 500f;

    [Property]
    public DamageFlags DamageFlags { get; init; } = DamageFlags.Melee;

    [Property]
    public int MaxTargetsPerSwing { get; init; } = 5;

    [Property]
    public bool AllowMultipleHitsPerTarget { get; init; } = false;

    [Property]
    public SoundEvent? AttackSound { get; init; }

    [Property]
    public SoundEvent? HitSound { get; init; }

    [Property]
    public SoundEvent? MissSound { get; init; }

    [Property]
    public GameObject? HitEffectPrefab { get; init; }

    [Sync]
    public TimeSince TimeSinceAttack { get; private set; }

    [Sync]
    public bool IsAttacking { get; private set; }

    [Sync]
    public TimeUntil AttackEnds { get; private set; }

    private float AttackCooldown => 1f / AttackRate;
    private HashSet<GameObject> hitTargetsThisSwing = new();

    public bool CanAttack()
    {
        if (!Equipment.IsValid() || !Equipment.Owner.IsValid())
            return false;

        if (Equipment.Tags.Has("no_attacking") || Equipment.Tags.Has("reloading"))
            return false;

        if (IsAttacking)
            return false;

        if (TimeSinceAttack < AttackCooldown)
            return false;

        return true;
    }

    public void Attack()
    {
        TimeSinceAttack = 0;
        IsAttacking = true;
        AttackEnds = AttackDuration;
        hitTargetsThisSwing.Clear();

        AttackEffects();
        Scene.Dispatch(new EquipmentMeleeAttackEvent());

        if (Equipment.Owner.IsValid() && Equipment.Owner.BodyRenderer.IsValid())
        {
            Equipment.Owner.BodyRenderer.Set("b_attack", true);
        }

        if (Equipment.ViewModel.IsValid())
        {
            Equipment.ViewModel.ModelRenderer.Set("b_attack", true);
        }

        var recoil = Equipment.GetComponentInChildren<Recoil>();
        if (recoil.IsValid())
        {
            recoil.Shoot();
        }
    }

    protected override void OnInputDown()
    {
        if (CanAttack())
        {
            Attack();
        }
    }

    protected override void OnFixedUpdate()
    {
        base.OnFixedUpdate();

        if (IsAttacking)
        {
            PerformHitDetection();

            if (AttackEnds)
            {
                IsAttacking = false;
            }
        }
    }

    private void PerformHitDetection()
    {
        if (!Networking.IsHost)
            return;

        var start = Equipment.Owner.EyePosition;
        var forward = Equipment.Owner.EyeAngles.Forward;

        var hits = Scene.Trace.Ray(start, start + forward * AttackRange).UseHitboxes()
            .IgnoreGameObjectHierarchy(Equipment.Owner.GameObject).WithoutTags("trigger").Size(AttackRadius).RunAll();

        ProcessHits(hits);
    }

    private void ProcessHits(IEnumerable<SceneTraceResult> hits)
    {
        var hitCount = 0;
        var anyHit = false;

        foreach (var hit in hits)
        {
            if (!hit.Hit || !hit.GameObject.IsValid())
                continue;

            if (!AllowMultipleHitsPerTarget && hitTargetsThisSwing.Contains(hit.GameObject.Root))
                continue;

            if (hitCount >= MaxTargetsPerSwing)
                break;

            var health = hit.GameObject.Root.GetComponentInChildren<Health>();
            if (health.IsValid())
            {
                ApplyDamage(hit, health);
                hitTargetsThisSwing.Add(hit.GameObject.Root);
                hitCount++;
                anyHit = true;
            }

            SpawnHitEffect(hit);
        }

        if (anyHit)
        {
            PlayHitSound();
        }
        else if (TimeSinceAttack < 0.1f)
        {
            PlayMissSound();
        }
    }

    private void ApplyDamage(SceneTraceResult hit, Health health)
    {
        var direction = (hit.EndPosition - Equipment.Owner.EyePosition).Normal;

        hit.GameObject.ServerTakeDamage(new()
        {
            Attacker = Equipment.Owner,
            Victim = health,
            Inflictor = Equipment,
            Position = hit.EndPosition,
            Damage = BaseDamage,
            Force = direction * Force,
            Hitbox = hit.GetHitboxTags(),
            Flags = DamageFlags,
        });
    }

    [Rpc.Broadcast]
    private void AttackEffects()
    {
        if (AttackSound.IsValid())
        {
            var snd = Sound.Play(AttackSound, Equipment.WorldPosition);
            if (snd.IsValid())
            {
                snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
            }
        }
    }

    private void SpawnHitEffect(SceneTraceResult hit)
    {
        BroadcastHitEffect(hit.GameObject, hit.EndPosition, hit.Normal, hit.Surface);
    }

    [Rpc.Broadcast]
    private void BroadcastHitEffect(GameObject target, Vector3 position, Vector3 normal, Surface surface)
    {
        if (HitEffectPrefab.IsValid())
        {
            _ = HitEffectPrefab.Clone(new CloneConfig
            {
                Transform = new(position, Rotation.LookAt(normal)),
                StartEnabled = true,
            });
        }

        if (surface.IsValid() && surface.PrefabCollection.BluntImpact.IsValid())
        {
            var impact = surface.PrefabCollection.BluntImpact.Clone();
            impact.WorldPosition = position + normal;
            impact.WorldRotation = Rotation.LookAt(-normal);
            impact.SetParent(target);

            Sound.Play(surface.SoundCollection.ScrapeRough, position);
        }
    }

    private void PlayHitSound()
    {
        BroadcastHitSound();
    }

    [Rpc.Broadcast]
    private void BroadcastHitSound()
    {
        if (HitSound is not null)
        {
            var snd = Sound.Play(HitSound, Equipment.WorldPosition);
            if (snd is not null)
            {
                snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
            }
        }
    }

    private void PlayMissSound()
    {
        BroadcastMissSound();
    }

    [Rpc.Broadcast]
    private void BroadcastMissSound()
    {
        if (MissSound is not null)
        {
            var snd = Sound.Play(MissSound, Equipment.WorldPosition);
            if (snd is not null)
            {
                snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
            }
        }
    }
}