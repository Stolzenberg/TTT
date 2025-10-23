using System;
using Sandbox.Events;

namespace Mountain;

public class Reloadable : EquipmentInputAction, IGameEventHandler<EquipmentHolsteredEvent>
{
    /// <summary>
    ///     How long does it take to reload?
    /// </summary>
    [Property]
    public float ReloadTime { get; init; } = 1.0f;

    /// <summary>
    ///     How long does it take to reload while empty?
    /// </summary>
    [Property]
    public float EmptyReloadTime { get; init; } = 2.0f;

    [Property]
    public bool SingleReload { get; init; } = false;

    [Property]
    public EquipmentAmmo AmmoComponent { get; init; }

    [Property]
    public Dictionary<float, SoundEvent> TimedReloadSounds { get; init; } = new();

    [Property]
    public Dictionary<float, SoundEvent> EmptyReloadSounds { get; init; } = new();

    [Sync]
    private bool IsReloading { get; set; }

    private bool queueCancel;
    private TimeUntil timeUntilReload;

    void IGameEventHandler<EquipmentHolsteredEvent>.OnGameEvent(EquipmentHolsteredEvent eventArgs)
    {
        if (!IsProxy && IsReloading)
        {
            CancelReload();
        }
    }

    protected override void OnEnabled()
    {
        BindTag("reloading", () => IsReloading);
    }

    protected override void OnInput()
    {
        if (CanReload())
        {
            StartReload();
        }
    }

    protected override void OnUpdate()
    {
        if (!Player.IsValid())
        {
            return;
        }

        if (!Player.IsLocallyControlled)
        {
            return;
        }

        if (SingleReload && IsReloading && Input.Pressed("Attack1"))
        {
            queueCancel = true;
        }

        if (IsReloading && timeUntilReload)
        {
            EndReload();
        }
    }

    private bool CanReload()
    {
        if (IsReloading || !AmmoComponent.IsValid() || AmmoComponent.IsFull)
        {
            return false;
        }

        // Check if player has ammo in inventory for this weapon type
        var ammoType = Equipment.Resource.AmmoType;
        if (ammoType != AmmoType.None && !Player.HasAmmo(ammoType))
        {
            return false;
        }

        return true;
    }

    private float GetReloadTime()
    {
        return AmmoComponent.HasAmmo ? ReloadTime : EmptyReloadTime;
    }

    private Dictionary<float, SoundEvent> GetReloadSounds()
    {
        return AmmoComponent.HasAmmo ? TimedReloadSounds : EmptyReloadSounds;
    }

    [Rpc.Owner]
    private void StartReload()
    {
        queueCancel = false;

        if (!IsProxy)
        {
            IsReloading = true;
            timeUntilReload = GetReloadTime();
        }

        if (!Player.IsPossessed)
        {
            return;
        }

        if (SingleReload)
        {
            Equipment.ViewModel.ModelRenderer.Set("b_reloading", true);

            var hasAmmo = AmmoComponent.HasAmmo;
            Equipment.ViewModel.ModelRenderer.Set(!hasAmmo ? "b_reloading_first_shell" : "b_reloading_shell", true);
        }
        else
        {
            Equipment.ViewModel.ModelRenderer.Set("b_reload", true);
        }

        Equipment.Owner.BodyRenderer.Set("b_reload", true);

        foreach (var kv in GetReloadSounds())
        {
            // Play this sound after a certain time but only if we're reloading.
            PlayAsyncSound(kv.Key, kv.Value, () => IsReloading);
        }
    }

    [Rpc.Owner]
    private void CancelReload()
    {
        IsReloading = false;
    }

    [Rpc.Owner]
    private void EndReload()
    {
        var ammoType = Equipment.Resource.AmmoType;

        if (SingleReload)
        {
            // Try to take 1 ammo from player inventory
            if (ammoType != AmmoType.None)
            {
                var ammoTaken = Player.TakeAmmo(ammoType, 1);
                if (ammoTaken > 0)
                {
                    AmmoComponent.Ammo++;
                    AmmoComponent.Ammo = AmmoComponent.Ammo.Clamp(0, AmmoComponent.MaxAmmo);
                }
            }
            else
            {
                // No ammo type, just refill (for melee or infinite ammo weapons)
                AmmoComponent.Ammo++;
                AmmoComponent.Ammo = AmmoComponent.Ammo.Clamp(0, AmmoComponent.MaxAmmo);
            }

            // Reload more!
            if (!queueCancel && AmmoComponent.Ammo < AmmoComponent.MaxAmmo &&
                (ammoType == AmmoType.None || Player.HasAmmo(ammoType)))
            {
                StartReload();
            }
            else
            {
                Equipment.ViewModel.ModelRenderer.Set("b_reloading", false);
                IsReloading = false;
            }
        }
        else
        {
            IsReloading = false;

            // Calculate how much ammo we need to fill the magazine
            var ammoNeeded = AmmoComponent.MaxAmmo - AmmoComponent.Ammo;

            if (ammoType != AmmoType.None)
            {
                // Take ammo from player inventory
                var ammoTaken = Player.TakeAmmo(ammoType, ammoNeeded);
                AmmoComponent.Ammo += ammoTaken;
            }
            else
            {
                // No ammo type, just refill (for melee or infinite ammo weapons)
                AmmoComponent.Ammo = AmmoComponent.MaxAmmo;
            }
        }

        // Tags will be better so we can just react to stimuli.
        Equipment.ViewModel.ModelRenderer.Set("b_reload", false);
    }

    private async void PlayAsyncSound(float delay, SoundEvent snd, Func<bool> playCondition = null)
    {
        await GameTask.DelaySeconds(delay);

        // Can we play this sound?
        if (!playCondition.Invoke())
        {
            return;
        }

        if (!GameObject.IsValid())
        {
            return;
        }

        GameObject.PlaySound(snd);
    }
}