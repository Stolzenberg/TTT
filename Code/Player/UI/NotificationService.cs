using System;

namespace Mountain;

public static class NotificationService
{
    public static event Action<Notification>? OnNotificationAdded;

    /// <summary>
    /// Send a notification to the HUD
    /// </summary>
    public static void Send(string message, NotificationType type)
    {
        var notification = new Notification
        {
            Message = message,
            Type = type,
            CreatedAt = 0
        };

        OnNotificationAdded?.Invoke(notification);
    }

    /// <summary>
    /// Send an info notification
    /// </summary>
    public static void Info(string message)
    {
        Send(message, NotificationType.Info);
    }

    /// <summary>
    /// Send a warning notification
    /// </summary>
    public static void Warning(string message)
    {
        Send(message, NotificationType.Warning);
    }

    /// <summary>
    /// Send an error notification
    /// </summary>
    public static void Error(string message)
    {
        Send(message, NotificationType.Error);
    }
}
