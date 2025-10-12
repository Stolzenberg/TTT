using System;

namespace Mountain;

[Flags]
public enum DamageFlags
{
    None = 0,
    Armor = 1,
    Helmet = 1 << 1,
    Melee = 1 << 2,
    Explosion = 1 << 3,
    FallDamage = 1 << 4,
    Burn = 1 << 5,
    WallBang = 1 << 6,
    AirShot = 1 << 7,
    OutOfMap = 1 << 8,
}