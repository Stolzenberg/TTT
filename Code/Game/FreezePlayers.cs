using Sandbox.Events;

namespace Mountain;

public sealed class FreezePlayers : Component, IGameEventHandler<EnterStateEvent>, IGameEventHandler<LeaveStateEvent>,
    IGameEventHandler<PlayerSpawnedEvent>
{
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.IsFrozen = true;
        }
    }

    void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.IsFrozen = false;
        }
    }

    void IGameEventHandler<PlayerSpawnedEvent>.OnGameEvent(PlayerSpawnedEvent eventArgs)
    {
        eventArgs.Player.IsFrozen = true;
    }
}