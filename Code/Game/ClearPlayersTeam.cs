using Sandbox.Events;

namespace Mountain;

public sealed class ClearPlayersTeam : Component, IGameEventHandler<EnterStateEvent>
{
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        foreach (var player in Game.ActiveScene.AllClients())
        {
            player.AssignTeam(Team.Unassigned);
        }
    }
}