using System;

namespace Mountain;

public record struct DamageInfo()
{
    public Component Attacker { get; init; }
    public Component Victim { get; init; }
    public Component? Inflictor { get; init; } 
    public float Damage { get; init; }
    public Vector3 Position { get; init; } = default;
    public Vector3 Force { get; init; } = default;
    public HitboxTags Hitbox { get; init; } = default;
    public DamageFlags Flags { get; init; } = DamageFlags.None;
    public float ArmorDamage { get; init; } = 0f;
    
    public bool HasArmor => Flags.HasFlag(DamageFlags.Armor);

    public bool HasHelmet => Flags.HasFlag(DamageFlags.Helmet);

    public bool WasMelee => Flags.HasFlag(DamageFlags.Melee);

    public bool WasExplosion => Flags.HasFlag(DamageFlags.Explosion);

    public bool WasFallDamage => Flags.HasFlag(DamageFlags.FallDamage);

    public DateTime KilledAtUtc { get; init; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"\"{Attacker}\" - \"{Victim}\" with \"{Inflictor}\" ({Damage} damage)";
    }
}