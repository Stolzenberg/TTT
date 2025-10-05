using System;

namespace Mountain;

[Flags]
public enum DamageFlags
{
    None = 0,

    /// <summary>
    ///     The victim was wearing kevlar.
    /// </summary>
    Armor = 1,

    /// <summary>
    ///     The victim was wearing a helmet.
    /// </summary>
    Helmet = 2,

    /// <summary>
    ///     This was a knife attack.
    /// </summary>
    Melee = 4,

    /// <summary>
    ///     This was some kind of explosion.
    /// </summary>
    Explosion = 8,

    /// <summary>
    ///     The victim fell.
    /// </summary>
    FallDamage = 16,

    /// <summary>
    ///     The victim was burned.
    /// </summary>
    Burn = 32,

    /// <summary>
    ///     Did the attacker shoot through a wall?
    /// </summary>
    WallBang = 64,

    /// <summary>
    ///     Was the attacker in the air when doing this damage?
    /// </summary>
    AirShot = 128,
}