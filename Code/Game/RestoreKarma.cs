using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Restores karma for all players at the end of each round (for good behavior).
/// </summary>
public sealed class RestoreKarma : Component, IGameEventHandler<EnterStateEvent>
{
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        // Restore karma for all living players
        foreach (var client in Game.ActiveScene.AllClients().Where(c => c.Player.IsValid()))
        {
            client.RestoreKarma();
        }
    }
}