using System;
using Sandbox.Events;

namespace Mountain;

public record EquipmentShotEvent : IGameEvent;

public sealed class Shootable : EquipmentInputAction
{
    [Property, Feature("Bullet")]
    public float BaseDamage { get; init; } = 25.0f;

    [Property, Feature("Bullet")]
    public float FireRate { get; init; } = 0.2f;

    public float RPMToSeconds => 60 / FireRate;

    [Property, Feature("Bullet")]
    public float DryShootDelay { get; init; } = 0.15f;

    [Property, Feature("Bullet")]
    public float BulletSize { get; init; } = 1.0f;

    [Property, Feature("Bullet")]
    public int BulletCount { get; init; } = 1;

    [Property, Feature("Bullet")]
    public Curve BaseDamageFalloff { get; init; } = new(new List<Curve.Frame> { new(0, 1), new(1, 0) });

    [Property, Feature("Bullet")]
    public float MaxRange { get; init; } = 1024000;

    [Property, Feature("Bullet")]
    public float BulletSpread { get; init; } = 0;

    [Property, Feature("Bullet")]
    public float PenetrationThickness { get; init; } = 32f;

    [Property, Feature("Effects")]
    public GameObject EjectionPrefab { get; init; }

    [Property, Feature("Effects")]
    public GameObject MuzzleFlashPrefab { get; init; }

    /// <summary>
    ///     What sound should we play when we fire?
    /// </summary>
    [Property, Feature("Effects")]
    public SoundEvent ShootSound { get; init; }

    /// <summary>
    ///     What sound should we play when we dry fire?
    /// </summary>
    [Property, Feature("Effects")]
    public SoundEvent DryFireSound { get; init; }

    /// <summary>
    ///     The current equipment's ammo container.
    /// </summary>
    [Property, Category("Ammo"), Feature("Ammo")]
    public EquipmentAmmo? Ammo { get; init; }

    /// <summary>
    ///     Does this equipment require an ammo container to fire its bullets?
    /// </summary>
    [Property, Category("Ammo"), FeatureEnabled("Ammo")]
    public bool RequiresHasAmmo { get; init; } = false;

    /// <summary>
    ///     How quickly can we switch fire mode?
    /// </summary>
    [Property, Feature("Fire Modes")]
    public float FireModeSwitchDelay { get; init; } = 0.3f;

    /// <summary>
    ///     What fire modes do we support?
    /// </summary>
    [Property, Feature("Fire Modes")]
    public List<FireMode> SupportedFireModes { get; init; } =
    [
        FireMode.Semi,
    ];

    /// <summary>
    ///     What's our current fire mode? (Or Default)
    /// </summary>
    [Property, Sync, Feature("Fire Modes")]
    public FireMode CurrentFireMode { get; set; } = FireMode.Automatic;

    /// <summary>
    ///     How many bullets describes a burst?
    /// </summary>
    [Property, Feature("Fire Modes")]
    public int BurstAmount { get; init; } = 3;

    /// <summary>
    ///     How long after we finish a burst until we can shoot again?
    /// </summary>
    [Property, Feature("Fire Modes")]
    public float BurstEndDelay { get; init; } = 0.2f;

    [Sync]
    public TimeSince TimeSinceFireModeSwitch { get; private set; }

    [Sync]
    public TimeSince TimeSinceBurstFinished { get; private set; }

    [Sync]
    public bool IsBurstFiring { get; private set; }

    [Sync]
    public int BurstCount { get; private set; }

    public Ray Ray
    {
        get
        {
            if (Equipment.ViewModel.IsValid())
            {
                return new(Equipment.ViewModel.Muzzle.WorldPosition + Equipment.ViewModel.Muzzle.WorldRotation.Forward,
                    Equipment.ViewModel.Muzzle.WorldRotation.Forward);
            }

            throw new InvalidOperationException("ViewModel is not valid, cannot get AimRay.");
        }
    }

    /// <summary>
    ///     How long since we shot?
    /// </summary>
    public TimeSince TimeSinceShoot { get; private set; }

    /// <summary>
    ///     Fetches the desired model renderer that we'll focus effects on like trail effects, muzzle flashes, etc.
    /// </summary>
    public EquipmentModel Visual
    {
        get
        {
            if (IsProxy || !Equipment.ViewModel.IsValid())
            {
                return Equipment.WorldModel;
            }

            return Equipment.ViewModel;
        }
    }

    public bool CanShoot()
    {
        if (!Equipment.IsValid())
        {
            return false;
        }

        if (!Equipment.Owner.IsValid())
        {
            return false;
        }

        if (Equipment.Tags.Has("reloading") || Equipment.Tags.Has("no_shooting"))
        {
            return false;
        }

        // Delay checks
        if (TimeSinceShoot < RPMToSeconds)
        {
            return false;
        }

        // Ammo checks
        if (RequiresHasAmmo && (!Ammo.IsValid() || !Ammo.HasAmmo))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Shoot the gun!
    /// </summary>
    public void Shoot()
    {
        TimeSinceShoot = 0;

        if (Ammo is not null)
        {
            Ammo.Ammo--;
        }

        if (CurrentFireMode == FireMode.Burst)
        {
            IsBurstFiring = true;
        }

        ShootEffects();

        GameObject.Dispatch(new EquipmentShotEvent());

        for (var i = 0; i < BulletCount; i++)
        {
            ServerShoot(Ray.Position, Ray.Forward);
        }

        // If we have a recoil function, let it know.
        var recoil = Equipment.GetComponentInChildren<Recoil>();
        if (recoil.IsValid())
        {
            recoil.Shoot();
        }
    }
    
    protected override void OnInputUpdate()
    {
        if (Input.Pressed("FireMode"))
        {
            CycleFireMode();

            return;
        }

        HandleBurst();

        var wantsToShoot = CurrentFireMode switch
        {
            FireMode.Semi => Input.Pressed("attack1"),
            _ => IsDown, // Use the existing button state for auto and burst modes
        };

        if (!wantsToShoot)
        {
            return;
        }

        if (!CanShoot())
        {
            // Dry fire
            if (!Ammo.IsValid() || Ammo.HasAmmo)
            {
                return;
            }

            if (TimeSinceShoot < DryShootDelay)
            {
                return;
            }

            if (Tags.Has("reloading"))
            {
                return;
            }

            DryShoot();
        }
        else
        {
            if (IsBurstFiring)
            {
                return;
            }

            if (TimeSinceBurstFinished < BurstEndDelay)
            {
                return;
            }

            Shoot();
        }
    }

    [Rpc.Host]
    private void ServerShoot(Vector3 position, Vector3 forward)
    {
        foreach (var tr in GetShootTrace(position, forward))
        {
            if (!tr.Hit)
            {
                continue;
            }

            if (tr.Distance == 0)
            {
                continue;
            }

            BroadcastImpactEffects(tr.GameObject, tr.Surface, tr.EndPosition, tr.Normal);

            var damage = CalculateDamageFalloff(BaseDamage, tr.Distance);
            damage = damage.CeilToInt();

            if (tr.GameObject.IsValid())
            {
                tr.GameObject.ServerTakeDamage(new(Equipment.Owner, damage, Equipment, tr.EndPosition, tr.Direction,
                    tr.GetHitboxTags()));
            }
        }
    }

    private void DryShoot()
    {
        TimeSinceShoot = 0f;
        DryShootEffects();
    }

    [Rpc.Broadcast]
    private void DryShootEffects()
    {
        var snd = Sound.Play(DryFireSound, Equipment.WorldPosition);
        snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
        Log.Trace($"Shootable: DryShootSound {DryFireSound.ResourceName}");

        Visual.ModelRenderer.Set("b_attack_dry", true);
    }

    private float CalculateDamageFalloff(float damage, float distance)
    {
        var distDelta = distance / MaxRange;
        var damageMultiplier = BaseDamageFalloff.Evaluate(distDelta);

        return damage * damageMultiplier;
    }

    [Rpc.Broadcast]
    private void BroadcastImpactEffects(GameObject target, Surface surface, Vector3 pos, Vector3 normal)
    {
        var impact = surface.PrefabCollection.BulletImpact.Clone();
        impact.WorldPosition = pos + normal;
        impact.WorldRotation = Rotation.LookAt(-normal);
        impact.SetParent(target, true);
        
        Sound.Play(surface.SoundCollection.Bullet, pos);
    }

    [Rpc.Broadcast]
    private void ShootEffects()
    {
        if (MuzzleFlashPrefab.IsValid())
        {
            if (Visual.Muzzle.IsValid())
            {
                var muzzleFlashObj = MuzzleFlashPrefab.Clone(new CloneConfig
                {
                    Parent = Visual.Muzzle,
                    Transform = new(),
                    StartEnabled = true,
                    Name = $"Muzzle flash: {Equipment.GameObject}",
                });
            }
        }

        if (EjectionPrefab.IsValid())
        {
            if (Visual.EjectionPort.IsValid())
            {
                EjectionPrefab.Clone(new CloneConfig
                {
                    Parent = Visual.EjectionPort,
                    Transform = new(),
                    StartEnabled = true,
                    Name = $"Bullet ejection: {Visual.GameObject}",
                });
            }
        }

        if (Sound.Play(ShootSound, Equipment.WorldPosition) is { } snd)
        {
            snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
            Log.Trace($"Shootable: ShootSound {ShootSound.ResourceName}");
        }

        // Third person
        if (Equipment.Owner.IsValid() && Equipment.Owner.BodyRenderer.IsValid())
        {
            Equipment.Owner.BodyRenderer.Set("b_attack", true);
        }

        // First person
        if (Equipment.ViewModel.IsValid())
        {
            Equipment.ViewModel.ModelRenderer.Set("b_attack", true);
        }
    }

    private void HandleBurst()
    {
        if (IsBurstFiring && BurstCount >= BurstAmount - 1 || Tags.Has("reloading") && IsBurstFiring)
        {
            ClearBurst();
        }

        if (CurrentFireMode == FireMode.Burst && IsBurstFiring && CanShoot())
        {
            BurstCount++;
            Shoot();
        }
    }

    private void CycleFireMode()
    {
        if (TimeSinceFireModeSwitch < FireModeSwitchDelay)
        {
            return;
        }

        if (IsBurstFiring)
        {
            return;
        }

        if (IsDown)
        {
            return;
        }

        var curIndex = GetFireModeIndex(CurrentFireMode);
        var length = SupportedFireModes.Count;
        var newIndex = (curIndex + 1 + length) % length;

        // We didn't change anything
        if (newIndex == curIndex)
        {
            return;
        }

        CurrentFireMode = SupportedFireModes[newIndex];

        Equipment.ViewModel.SetFireMode(CurrentFireMode);

        TimeSinceFireModeSwitch = 0;
    }

    private int GetFireModeIndex(FireMode fireMode)
    {
        var i = 0;
        foreach (var mode in SupportedFireModes)
        {
            if (mode == fireMode)
            {
                return i;
            }

            i++;
        }

        return 0;
    }

    private void ClearBurst()
    {
        TimeSinceBurstFinished = 0;
        IsBurstFiring = false;
        BurstCount = 0;
    }

    private IEnumerable<SceneTraceResult> GetShootTrace(Vector3 position, Vector3 direction)
    {
        var hits = new List<SceneTraceResult>();

        var rot = Rotation.LookAt(direction);

        var forward = rot.Forward;
        forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * BulletSpread * 0.25f;

        forward = forward.Normal;

        var original = DoTraceBullet(position, position + forward * MaxRange, BulletSize);
        var sceneTraceResults = original.ToList();

        foreach (var traceResult in sceneTraceResults)
        {
            Log.Info($"tr.Hitbox: {traceResult.Hitbox}");
        }

        if (sceneTraceResults.Count == 0)
        {
            return sceneTraceResults;
        }

        // Run through and fix the start positions for the traces
        // By using the last end position as the start

        var startPos = sceneTraceResults.ElementAt(0).StartPosition;
        List<SceneTraceResult> fixedPath = new();
        for (var i = 0; i < sceneTraceResults.Count; i++)
        {
            var el = sceneTraceResults.ElementAt(i);

            fixedPath.Add(el with { StartPosition = startPos });
            startPos = el.EndPosition;
        }

        var entries = new List<(SceneTraceResult Trace, float Thickness)>();

        // Then, trace backwards from the end so we can get exit points and thickness
        for (var i = fixedPath.Count - 1; i >= 0; i--)
        {
            var el = fixedPath.ElementAt(i);

            // Do a trace back, from the end position to the start, this'll give us the LAST entry's exit point.
            var backTrace = DoTraceBulletOne(el.EndPosition, el.StartPosition, BulletSize);
            var impact = backTrace.EndPosition;

            // From that, we can calculate the surface thickness
            var thickness = (el.StartPosition - impact).Length;

            // Return the element starting at the exit point, it's more useful that way.
            el = el with { StartPosition = impact };
            entries.Insert(0, (el, thickness));
        }

        float accThickness = 0;
        foreach (var el in entries)
        {
            accThickness += el.Thickness;
            if (accThickness >= PenetrationThickness)
            {
                break;
            }

            hits.Add(el.Trace);
        }

        return hits;
    }

    private IEnumerable<SceneTraceResult> DoTraceBullet(Vector3 start, Vector3 end, float radius)
    {
        return TraceBullet(start, end, radius).RunAll();
    }

    private SceneTraceResult DoTraceBulletOne(Vector3 start, Vector3 end, float radius)
    {
        return TraceBullet(start, end, radius).Run();
    }

    private SceneTrace TraceBullet(Vector3 start, Vector3 end, float radius)
    {
        return Scene.Trace.Ray(start, end).UseHitboxes().IgnoreGameObjectHierarchy(GameObject.Root)
            .WithoutTags("trigger").Size(radius);
    }
}