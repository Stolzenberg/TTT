using System;
using Sandbox.Events;

namespace Mountain;

public record EquipmentShotEvent : IGameEvent;

/// <summary>
/// Base class for all shooting behaviors (raycast, projectile, etc.)
/// Handles shared logic like fire rate, fire modes, ammo consumption, and effects.
/// </summary>
public abstract class ShootingBehavior : EquipmentInputAction
{
    [Property, Feature("Shooting")]
    public float FireRate { get; init; } = 150f;

    public float RPMToSeconds => 60 / FireRate;

    [Property, Feature("Shooting")]
    public float DryShootDelay { get; init; } = 0.15f;

    /// <summary>
    /// How long since we shot?
    /// </summary>
    [Sync]
    public TimeSince TimeSinceShoot { get; private set; }

    [Property, Feature("Fire Modes")]
    public float FireModeSwitchDelay { get; init; } = 0.3f;

    [Property, Feature("Fire Modes")]
    public List<FireMode> SupportedFireModes { get; init; } = [FireMode.Semi];

    [Property, Sync, Feature("Fire Modes")]
    public FireMode CurrentFireMode { get; set; } = FireMode.Automatic;

    [Property, Feature("Fire Modes")]
    public int BurstAmount { get; init; } = 3;

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

    [Property, Category("Ammo"), Feature("Ammo")]
    public EquipmentAmmo? Ammo { get; init; }

    [Property, Category("Ammo"), FeatureEnabled("Ammo")]
    public bool RequiresHasAmmo { get; init; } = false;

    [Property, Feature("Effects")]
    public GameObject? EjectionPrefab { get; init; }

    [Property, Feature("Effects")]
    public GameObject? MuzzleFlashPrefab { get; init; }

    [Property, Feature("Effects")]
    public SoundEvent? ShootSound { get; init; }

    [Property, Feature("Effects")]
    public SoundEvent? DryFireSound { get; init; }

    protected Ray Ray
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

    protected EquipmentModel Visual
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

    protected virtual bool CanShoot()
    {
        if (!Equipment.IsValid() || !Equipment.Owner.IsValid())
        {
            return false;
        }

        if (Equipment.Tags.Has("reloading") || Equipment.Tags.Has("no_shooting") || Equipment.Tags.Has("bolting") ||
            Equipment.Tags.Has("has_to_bolt"))
        {
            return false;
        }

        if (TimeSinceShoot < RPMToSeconds)
        {
            return false;
        }

        if (RequiresHasAmmo && (!Ammo.IsValid() || !Ammo.HasAmmo))
        {
            return false;
        }

        return true;
    }

    protected void Shoot()
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

        // Derived classes implement the actual shooting mechanism
        PerformShoot();

        // Trigger recoil
        var recoil = Equipment.GetComponentInChildren<Recoil>();
        if (recoil.IsValid())
        {
            recoil.Shoot();
        }
    }

    /// <summary>
    /// Implemented by derived classes to perform the actual shooting (raycast, spawn projectile, etc.)
    /// </summary>
    protected abstract void PerformShoot();

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
            _ => IsDown,
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

    [Rpc.Broadcast]
    private void ShootEffects()
    {
        if (MuzzleFlashPrefab.IsValid() && Visual.Muzzle.IsValid())
        {
            MuzzleFlashPrefab.Clone(new CloneConfig
            {
                Parent = Visual.Muzzle,
                Transform = new(),
                StartEnabled = true,
                Name = $"Muzzle flash: {Equipment.GameObject}",
            });
        }

        if (EjectionPrefab.IsValid() && Visual.EjectionPort.IsValid())
        {
            EjectionPrefab.Clone(new CloneConfig
            {
                Parent = Visual.EjectionPort,
                Transform = new(),
                StartEnabled = true,
                Name = $"Bullet ejection: {Visual.GameObject}",
            });
        }

        if (ShootSound is not null && Sound.Play(ShootSound, Equipment.WorldPosition) is { } snd)
        {
            snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
            Log.Trace($"ShootingBehavior: ShootSound {ShootSound.ResourceName}");
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

    private void DryShoot()
    {
        TimeSinceShoot = 0f;
        DryShootEffects();
    }

    [Rpc.Broadcast]
    private void DryShootEffects()
    {
        if (DryFireSound is not null)
        {
            var snd = Sound.Play(DryFireSound, Equipment.WorldPosition);
            snd.SpacialBlend = Equipment.Owner.IsProxy ? snd.SpacialBlend : 0;
            Log.Trace($"ShootingBehavior: DryShootSound {DryFireSound.ResourceName}");
        }

        Visual.ModelRenderer.Set("b_attack_dry", true);
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
        if (TimeSinceFireModeSwitch < FireModeSwitchDelay || IsBurstFiring || IsDown)
        {
            return;
        }

        var curIndex = GetFireModeIndex(CurrentFireMode);
        var length = SupportedFireModes.Count;
        var newIndex = (curIndex + 1 + length) % length;

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
}