using System;
using Sandbox.Events;

namespace Mountain;

/// <summary>
///     A health component for any kind of GameObject.
/// </summary>
public class Health : Component
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
    ///     How long has it been since we last took damage?
    /// </summary>
    public TimeSince TimeSinceLastDamage { get; private set; } = 0f;

    /// <summary>
    ///     What's our health?
    /// </summary>
    [Sync(SyncFlags.FromHost), Change(nameof(OnHealthPropertyChanged))]
    public float CurrentHealth { get; set; } = 100f;

    [Property, Group("Setup")]
    public float MaxHealth { get; set; } = 100f;

    /// <summary>
    ///     What's our life state?
    /// </summary>
    [Group("Life State"), Sync(SyncFlags.FromHost), Change(nameof(OnStatePropertyChanged))]
    public LifeState State { get; private set; }

    /// <summary>
    ///     Should health regenerate over time?
    /// </summary>
    [Property, Feature("Healing Over Time"), FeatureEnabled("Healing Over Time"),
     ConVar("healing_over_time_enabled", Name = "Healing Over Time Enabled",
         Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public bool EnableHealingOverTime { get; set; } = false;

    /// <summary>
    ///     How much health to restore per second.
    /// </summary>
    [Property, Feature("Healing Over Time"),
     ConVar("healing_per_second", Name = "Amount of Healing Per Second",
         Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public float HealingPerSecond { get; set; } = 5f;

    /// <summary>
    ///     How long (in seconds) must pass without taking damage before healing starts.
    /// </summary>
    [Property, Feature("Healing Over Time"),
     ConVar("healing_delay", Name = "Amount of Seconds without Damage Before Healing Starts",
         Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public float HealingDelay { get; set; } = 3f;

    /// <summary>
    ///     How often (in seconds) to apply healing ticks.
    /// </summary>
    [Property, Feature("Healing Over Time"),
     ConVar("healing_tick_rate", Name = "Healing Tick Rate", Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public float HealingTickRate { get; set; } = 0.1f;

    private TimeSince timeSinceLastHealTick = 0f;

    public void ServerTakeDamage(DamageInfo damageInfo)
    {
        // Apply karma damage multiplier if attacker is a player attacking another player
        var actualDamage = damageInfo.Damage;

        if (damageInfo.Attacker.IsValid() && damageInfo.Victim.IsValid())
        {
            var attackerPlayer = damageInfo.Attacker.GameObject.Root.GetComponentInChildren<Player>();
            var victimPlayer = damageInfo.Victim.GameObject.Root.GetComponentInChildren<Player>();

            if (attackerPlayer.IsValid() && victimPlayer.IsValid() && attackerPlayer != victimPlayer)
            {
                var karmaMultiplier = attackerPlayer.GetKarmaDamageMultiplier();
                actualDamage = damageInfo.Damage * karmaMultiplier;
            }
        }

        BroadcastDamage(actualDamage, damageInfo.Position, damageInfo.Force, damageInfo.Attacker, damageInfo.Inflictor,
            damageInfo.Hitbox, damageInfo.Flags);

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody.IsValid())
        {
            rigidbody.ApplyImpulseAt(damageInfo.Position, damageInfo.Force);
        }

        if (IsGodMode)
        {
            Log.Info(
                $"{GameObject.Name} is in god mode and took no damage. But should have taken {actualDamage} damage from {damageInfo.Attacker.GameObject.Name ?? "unknown"} flags: {damageInfo.Flags}");

            return;
        }

        Log.Info(
            $"{GameObject.Name} took {actualDamage} damage from {damageInfo.Attacker.GameObject.Name ?? "unknown"} flags: {damageInfo.Flags}");


        // Reset the damage timer for healing over time
        TimeSinceLastDamage = 0f;

        CurrentHealth = Math.Max(0f, CurrentHealth - actualDamage);

        if (CurrentHealth > 0f || State != LifeState.Alive)
        {
            return;
        }

        CurrentHealth = 0f;
        State = LifeState.Dead;

        BroadcastKill(actualDamage, damageInfo.Position, damageInfo.Force, damageInfo.Attacker, damageInfo.Inflictor,
            damageInfo.Hitbox, damageInfo.Flags);

        Log.Info($"{GameObject.Name} was killed by {damageInfo.Attacker.GameObject.Name ?? "unknown"}");
    }

    /// <summary>
    ///     Called when <see cref="CurrentHealth" /> is changed across the network.
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
        CurrentHealth = MaxHealth;
    }

    protected override void OnFixedUpdate()
    {
        // Only process healing on the host/server
        if (!Networking.IsHost)
        {
            return;
        }

        // Only heal if the feature is enabled, player is alive, and not at max health
        if (!EnableHealingOverTime || State != LifeState.Alive || CurrentHealth >= MaxHealth)
        {
            return;
        }

        // Check if enough time has passed since last damage
        if (TimeSinceLastDamage < HealingDelay)
        {
            return;
        }

        // Check if it's time for a healing tick
        if (timeSinceLastHealTick < HealingTickRate)
        {
            return;
        }

        // Apply healing
        var healAmount = HealingPerSecond * HealingTickRate;
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + healAmount);
        timeSinceLastHealTick = 0f;
    }

    [Button]
    private void DebugDamage()
    {
        ServerTakeDamage(new DamageInfo
        {
            Attacker = this,
            Victim = this,
            Inflictor = null,
            Damage = 50f,
            Position = GameObject.WorldPosition,
            Force = Vector3.Up * 100f,
            Flags = DamageFlags.None,
        });
    }

    [Rpc.Broadcast]
    private void BroadcastDamage(float damage, Vector3 position, Vector3 force, Component attacker, Component inflictor,
        HitboxTags hitbox, DamageFlags flags)
    {
        var damageInfo = new DamageInfo
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

        if (damageInfo.Attacker.IsValid())
        {
            damageInfo.Attacker.GameObject.Root.Dispatch(new DamageGivenEvent(damageInfo));
        }

        GameObject.Root.Dispatch(new DamageTakenEvent(damageInfo));
        Scene.Dispatch(new DamageTakenGlobalEvent(damageInfo));
    }

    [Rpc.Broadcast]
    private void BroadcastKill(float damage, Vector3 position, Vector3 force, Component attacker, Component inflictor,
        HitboxTags hitbox, DamageFlags flags)
    {
        var damageInfo = new DamageInfo
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

        if (damageInfo.Attacker.IsValid())
        {
            damageInfo.Attacker.GameObject.Root.Dispatch(new KilledEvent(damageInfo));
        }

        GameObject.Root.Dispatch(new KillEvent(damageInfo));
        Scene.Dispatch(new GlobalKillEvent(damageInfo));
    }
}