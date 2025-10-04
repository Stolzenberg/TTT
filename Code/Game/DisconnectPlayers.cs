using Sandbox.Events;

namespace Mountain;

public sealed class DisconnectPlayers : Component, IGameEventHandler<EnterStateEvent>
{
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        foreach (var client in Game.ActiveScene.AllClients())
        {
            client.ServerKick("Disconnected by the server.");
        }
    }
}