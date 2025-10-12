using Sandbox.Events;

namespace Mountain;

public sealed class NoCollisionBetweenPlayers : Component, IGameEventHandler<EnterStateEvent>, IGameEventHandler<LeaveStateEvent>,
    IGameEventHandler<PlayerSpawnedEvent>
{
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.Tags.Set("no-player-collision", true);
        }
    }

    void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.Tags.Set("no-player-collision", false);
        }
    }

    void IGameEventHandler<PlayerSpawnedEvent>.OnGameEvent(PlayerSpawnedEvent eventArgs)
    {
        eventArgs.Player.Tags.Set("no-player-collision", true);
    }
}