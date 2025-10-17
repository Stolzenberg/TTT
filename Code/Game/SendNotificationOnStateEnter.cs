using Sandbox.Events;

namespace Mountain;

public sealed class SendNotificationOnStateEnter : Component, IGameEventHandler<EnterStateEvent>
{
    [Property]
    public NotificationType NotificationType { get; set; }

    [Property]
    public string NotificationMessage { get; set; }

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        NotificationService.Send(NotificationMessage, NotificationType);
    }
}