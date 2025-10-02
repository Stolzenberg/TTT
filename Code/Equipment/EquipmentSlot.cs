namespace Mountain;

public enum EquipmentSlot
{
    Undefined = 0,
	
    /// <summary>
    /// Non-pistol guns.
    /// </summary>
    Primary = 1,

    /// <summary>
    /// Pistols.
    /// </summary>
    Secondary = 2,

    /// <summary>
    /// Knives etc.
    /// </summary>
    Melee = 3,

    /// <summary>
    /// Grenades etc.
    /// </summary>
    Utility = 4,

    /// <summary>
    /// C4 etc.
    /// </summary>
    Special = 5
}