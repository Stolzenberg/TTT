using Sandbox.Events;

namespace Mountain;

public sealed class DeletePlayers : Component, IGameEventHandler<EnterStateEvent>, IGameEventHandler<LeaveStateEvent>
{
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.GameObject.Destroy();
        }
    }

    void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllPlayers())
        {
            player.GameObject.Destroy();
        }
    }
}