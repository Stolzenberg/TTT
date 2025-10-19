using Sandbox.Events;

namespace Mountain;

public sealed class TeamNotifications : Component, IGameEventHandler<TeamAssignedEvent>
{
    void IGameEventHandler<TeamAssignedEvent>.OnGameEvent(TeamAssignedEvent eventArgs)
    {
        if (eventArgs.Team == Team.Unassigned)
        {
            return;
        }
        
        NotificationService.Info("#PLAYING_STATE_STARTED_NOTIFICATION");
        NotificationService.Info($"#{eventArgs.Team.ToString().ToUpper()}_TASKS_NOTIFICATION");
        
    }
}