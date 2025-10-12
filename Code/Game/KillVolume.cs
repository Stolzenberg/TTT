namespace Mountain;

public sealed class KillVolume : Component, Component.ITriggerListener
{
    public void OnTriggerEnter(GameObject other)
    {
        if (!Networking.IsHost)
        {
            return;   
        }
        
        var health = other.Root.GetComponentInChildren<Health>();
        if (!health.IsValid())
        {
            return;
        }
        
        health.ServerTakeDamage(new ()
        {
            Attacker = this,
            Victim = health,
            Inflictor = this,
            Damage = int.MaxValue,
            Position = default,
            Force = default,
            Hitbox = HitboxTags.None,
            Flags = DamageFlags.OutOfMap,
            ArmorDamage = int.MaxValue,
        });
    }
}