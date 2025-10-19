using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Restores karma for all players at the end of each round (for good behavior).
/// </summary>
public sealed class RestoreKarma : Component, IGameEventHandler<BetweenRoundCleanupEvent>
{
    void IGameEventHandler<BetweenRoundCleanupEvent>.OnGameEvent(BetweenRoundCleanupEvent eventArgs)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        // Restore karma for all living players
        foreach (var player in Scene.GetAllComponents<Player>())
        {
            if (player.IsValid())
            {
                player.RestoreKarma();
            }
        }
    }
}

