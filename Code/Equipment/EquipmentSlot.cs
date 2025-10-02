namespace Mountain;

public enum EquipmentSlot
{
    Undefined = 0,
    
    Fists = 1,
	
    /// <summary>
    /// Non-pistol guns.
    /// </summary>
    Primary = 2,

    /// <summary>
    /// Pistols.
    /// </summary>
    Secondary = 3,

    /// <summary>
    /// Knives etc.
    /// </summary>
    Melee = 4,

    /// <summary>
    /// Grenades etc.
    /// </summary>
    Utility = 5,

    /// <summary>
    /// C4 etc.
    /// </summary>
    Special = 6
}