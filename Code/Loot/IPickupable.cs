namespace Mountain;

/// <summary>
/// Interface for items that can be picked up by players.
/// </summary>
public interface IPickupable
{
    /// <summary>
    /// Called when a player attempts to pick up this item.
    /// </summary>
    /// <param name="player">The player attempting to pick up the item</param>
    /// <returns>True if the pickup was successful, false otherwise</returns>
    bool TryPickup(Player player);

    /// <summary>
    /// Gets the display name of this pickupable item.
    /// </summary>
    string GetDisplayName();
}