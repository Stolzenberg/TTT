namespace Mountain;

/// <summary>
/// Defines the types of ammunition available in the game.
/// </summary>
public enum AmmoType
{
    /// <summary>
    /// No ammunition type (for melee weapons, etc.)
    /// </summary>
    None = 0,

    /// <summary>
    /// Pistol ammunition (9mm, .45 ACP, etc.)
    /// </summary>
    Pistol = 1,

    /// <summary>
    /// Rifle ammunition (5.56mm, 7.62mm, etc.)
    /// </summary>
    Rifle = 2,

    /// <summary>
    /// Shotgun shells
    /// </summary>
    Shotgun = 3,

    /// <summary>
    /// Sniper rifle ammunition
    /// </summary>
    Sniper = 4,

    /// <summary>
    /// Submachine gun ammunition
    /// </summary>
    SMG = 5,

    /// <summary>
    /// Rockets and explosives
    /// </summary>
    Rocket = 6,
}