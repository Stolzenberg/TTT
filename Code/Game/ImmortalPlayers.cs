using Sandbox.Events;

namespace Mountain;

public sealed class ImmortalPlayers : Component, IGameEventHandler<EnterStateEvent>, IGameEventHandler<LeaveStateEvent>,
    IGameEventHandler<PlayerSpawnedEvent>
{
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.Health.IsGodMode = true;
        }
    }

    void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.Health.IsGodMode = false;
        }
    }

    void IGameEventHandler<PlayerSpawnedEvent>.OnGameEvent(PlayerSpawnedEvent eventArgs)
    {
        eventArgs.Player.Health.IsGodMode = true;
    }
}