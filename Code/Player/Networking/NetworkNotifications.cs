using Sandbox.Events;

namespace Mountain;

public sealed class NetworkNotifications : Component, IGameEventHandler<PlayerConnectedEvent>
{
    void IGameEventHandler<PlayerConnectedEvent>.OnGameEvent(PlayerConnectedEvent eventArgs)
    {
        NotificationService.Info(LocalizationHelper.Resolve("#PLAYER_CONNECTED_NOTIFICATION",
            eventArgs.Client.DisplayName));
    }
}