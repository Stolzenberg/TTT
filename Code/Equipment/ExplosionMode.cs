namespace Mountain;

public enum ExplosionMode
{
    /// <summary>
    /// No explosion occurs
    /// </summary>
    None,

    /// <summary>
    /// Explodes when colliding with something
    /// </summary>
    OnImpact,

    /// <summary>
    /// Explodes after a set time delay
    /// </summary>
    OnTime,

    /// <summary>
    /// Explodes when near a target (proximity trigger)
    /// </summary>
    OnProximity
}