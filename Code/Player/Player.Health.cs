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

        snd.SpacialBlend = Client.IsLocalPlayer ? 0 : snd.SpacialBlend;
    }

    public void OnKill(DamageInfo damageInfo)
    {
        CreateRagdoll(true);
        NameTag.Destroy();
        DeathPanel.Show(damageInfo);
    }
}