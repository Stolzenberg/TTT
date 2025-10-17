using System;

namespace Mountain;

public enum NotificationType
{
    Info,
    Warning,
    Error
}

public class Notification
{
    public NotificationType Type { get; set; }
    public string Message { get; set; }
    public TimeSince CreatedAt { get; set; } = 0;
}