using System;
using Sandbox.Events;

namespace Mountain;

public sealed partial class Player : IGameEventHandler<DamageTakenEvent>, IRespawnable
{
    /// <summary>
    ///     An accessor for health component if we have one.
    /// </summary>
    [Property]
    public HealthComponent Health { get; set; }

    [Property, Feature("Health"), Group("Effects")]
    public SoundEvent BloodImpactSound { get; set; }

    [Property, Feature("Health"), Group("Effects")]
    public GameObject BloodEffect { get; set; }

    [Property, Feature("Health"), Group("Fall Damage")]
    public float MinimumImpactVelocity { get; set; } = 500f;
    [Property, Feature("Health"), Group("Fall Damage")]
    public float MinimumFallSoundVelocity { get; set; } = 300f;
    [Property, Feature("Health"), Group("Fall Damage")]
    public float FallDamageScale { get; set; } = 0.02f;

    // Velocity tracking for impact detection
    private Vector3 previousVelocity;
    private TimeSince lastImpactTime;
    private const float ImpactCooldown = 0.5f; // Prevent multiple impacts in quick succession

    void IGameEventHandler<DamageTakenEvent>.OnGameEvent(DamageTakenEvent eventArgs)
    {
        var damageInfo = eventArgs.DamageInfo;

        var attacker = eventArgs.DamageInfo.Attacker.GetPlayerFromComponent();
        var victim = eventArgs.DamageInfo.Victim?.GetPlayerFromComponent();

        var position = eventArgs.DamageInfo.Position;
        var force = damageInfo.Force.IsNearZeroLength ? Random.Shared.VectorInSphere() : damageInfo.Force;

        ProceduralHitReaction(damageInfo.Damage / 100f, force);

        if (!damageInfo.Attacker.IsValid())
        {
            return;
        }

        if (attacker != victim && Body.IsValid())
        {
            DamageTakenPosition = position;
            DamageTakenForce = force.Normal * damageInfo.Damage;
        }

        if (BloodEffect.IsValid())
        {
            BloodEffect?.Clone(new CloneConfig
            {
                StartEnabled = true,
                Transform = new(position),
                Name = $"Blood effect from ({GameObject})",
            });
        }

        var snd = Sound.Play(BloodImpactSound, position);
        if (!snd.IsValid())
        {
            return;
        }

        snd.SpacialBlend = Client.IsLocalClient ? 0 : snd.SpacialBlend;
    }

    public void OnKill(DamageInfo damageInfo)
    {
        CreateRagdoll(true);
        
        RequestDropAllEquipment();

        if (Client.IsLocalClient)
        {
            Client.CycleSpectatorTarget(1);
        }
    }

    private void HandleImpactDamage()
    {
        if (Mode is NoClipMovementState)
        {
            return;
        }
        
        var minimumVelocity = MinimumImpactVelocity;
        if (Velocity.Length > MinimumFallSoundVelocity)
        {
            // Sound
        }

        // Check if the velocity suddenly changed (impact detection)
        var velocityChange = (previousVelocity - Velocity).Length;

        var hasImpacted = velocityChange > minimumVelocity && 
                          lastImpactTime > ImpactCooldown;

        if (hasImpacted)
        {
            var damage = velocityChange * FallDamageScale;

            Log.Info("Fall damage should now get applied: " + damage); 
            
            // // Apply fall damage
            // GameObject.ServerTakeDamage(new()
            // {
            //     Attacker = this,
            //     Inflictor = this,
            //     Victim = this,
            //     Damage = damage,
            //     Position = WorldPosition,
            //     Flags = DamageFlags.FallDamage
            // });

            lastImpactTime = 0;
        }

        // Update previous velocity for next frame
        previousVelocity = Velocity;
    }
}