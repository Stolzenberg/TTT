using Sandbox.Events;

namespace Mountain;

public sealed class RespawnPlayers : Component, IGameEventHandler<EnterStateEvent>
{
	[Property]
    public bool ForceNew { get; set; }

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllClients())
        {
            player.ServerRespawn(ForceNew);
        }
    }
}