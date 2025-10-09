using System;
using Sandbox.Events;

namespace Mountain;

/// <summary>
///     A health component for any kind of GameObject.
/// </summary>
public class HealthComponent : Component
{
    /// <summary>
    ///     Are we in god mode?
    /// </summary>
    [Property, Sync(SyncFlags.FromHost)]
    public bool IsGodMode { get; set; } = false;

    /// <summary>
    ///     An action (mainly for ActionGraphs) to respond to when a GameObject's health changes.
    /// </summary>
    [Property]
    public Action<float, float> OnHealthChanged { get; set; }

    /// <summary>
    ///     How long has it been since life state changed?
    /// </summary>
    public TimeSince TimeSinceLifeStateChanged { get; private set; } = 1f;

    /// <summary>
    ///     What's our health?
    /// </summary>
    [Sync(SyncFlags.FromHost), Change(nameof(OnHealthPropertyChanged))]
    public float Health { get; set; } = 100f;

    [Property, Group("Setup")]
    public float MaxHealth { get; set; } = 100f;

    /// <summary>
    ///     What's our life state?
    /// </summary>
    [Group("Life State"), Sync(SyncFlags.FromHost), Change(nameof(OnStatePropertyChanged))]
    public LifeState State { get; private set; }

    public void ServerTakeDamage(DamageInfo damageInfo)
    {
        BroadcastDamage(damageInfo.Damage, damageInfo.Position, damageInfo.Force, damageInfo.Attacker,
            damageInfo.Inflictor, damageInfo.Hitbox, damageInfo.Flags);

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody.IsValid())
        {
            rigidbody.ApplyImpulseAt(damageInfo.Position, damageInfo.Force);
        }
        
        Log.Info($"{GameObject.Name} took {damageInfo.Damage} damage from {damageInfo.Attacker?.GameObject.Name ?? "unknown"}");

        if (IsGodMode)
        {
            return;
        }

        Health = Math.Max(0f, Health - damageInfo.Damage);

        if (Health > 0f || State != LifeState.Alive)
        {
            return;
        }

        Health = 0f;
        State = LifeState.Dead;
        
        BroadcastKill(damageInfo.Damage, damageInfo.Position, damageInfo.Force, damageInfo.Attacker,
            damageInfo.Inflictor, damageInfo.Hitbox, damageInfo.Flags);
        
        Log.Info($"{GameObject.Name} was killed by {damageInfo.Attacker.GameObject.Name ?? "unknown"}");
    }

    /// <summary>
    ///     Called when <see cref="Health" /> is changed across the network.
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    protected void OnHealthPropertyChanged(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(oldValue, newValue);
    }

    protected void OnStatePropertyChanged(LifeState oldValue, LifeState newValue)
    {
        TimeSinceLifeStateChanged = 0f;
    }

    protected override void OnStart()
    {
        Health = MaxHealth;
    }
    
    [Rpc.Broadcast]
    private void BroadcastDamage(float damage, Vector3 position, Vector3 force, Component attacker,
        Component inflictor = default, HitboxTags hitbox = default, DamageFlags flags = default)
    {
        var damageInfo = new DamageInfo()
        {
            Attacker = attacker,
            Victim = this,
            Inflictor = inflictor,
            Damage = damage,
            Position = position,
            Force = force,
            Hitbox = hitbox,
            Flags = flags,
        };

        GameObject.Root.Dispatch(new DamageTakenEvent(damageInfo));
        Scene.Dispatch(new DamageTakenGlobalEvent(damageInfo));

        if (damageInfo.Attacker.IsValid())
        {
            damageInfo.Attacker.GameObject.Root.Dispatch(new DamageGivenEvent(damageInfo));
        }
    }

    [Rpc.Broadcast]
    private void BroadcastKill(float damage, Vector3 position, Vector3 force, Component attacker,
        Component inflictor = default, HitboxTags hitbox = default, DamageFlags flags = default)
    {
        var damageInfo = new DamageInfo()
        {
            Attacker = attacker,
            Victim = this,
            Inflictor = inflictor,
            Damage = damage,
            Position = position,
            Force = force,
            Hitbox = hitbox,
            Flags = flags,
        };

        GameObject.Root.Dispatch(new KillEvent(damageInfo));
        Scene.Dispatch(new GlobalKillEvent(damageInfo));
    }
}