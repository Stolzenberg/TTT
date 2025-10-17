using Sandbox.Events;

namespace Mountain;

public sealed class NetworkNotifications : Component, IGameEventHandler<PlayerBeginConnectEvent>, IGameEventHandler<PlayerConnectedEvent>
{
    void IGameEventHandler<PlayerBeginConnectEvent>.OnGameEvent(PlayerBeginConnectEvent eventArgs)
    {
        NotificationService.Info(LocalizationHelper.Resolve("#PLAYER_CONNECTING_NOTIFICATION", eventArgs.Client.DisplayName));
    }

    void IGameEventHandler<PlayerConnectedEvent>.OnGameEvent(PlayerConnectedEvent eventArgs)
    {
        NotificationService.Info(LocalizationHelper.Resolve("#PLAYER_CONNECTED_NOTIFICATION", eventArgs.Client.DisplayName));
    }
}