using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq;

namespace Mountain;

public partial class Hud : PanelComponent
{
    private EquipmentAmmo? Ammo =>
        Client.Viewer?.Player?.ActiveEquipment?.Components?.Get<EquipmentAmmo>(FindMode
            .EverythingInSelfAndDescendants) ?? null;

    private Health? Health =>
        Client.Viewer?.Player?.Components?.Get<Health>(FindMode.EverythingInSelfAndDescendants) ?? null;
    
    private List<Notification> Notifications { get; set; } = new();
    private const int MaxNotifications = 5;
    private const float NotificationLifetime = 6.0f;

    protected override void OnEnabled()
    {
        base.OnEnabled();
        NotificationService.OnNotificationAdded += HandleNotification;
    }

    protected override void OnDisabled()
    {
        base.OnDisabled();
        NotificationService.OnNotificationAdded -= HandleNotification;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        
        // Remove old notifications
        Notifications.RemoveAll(n => n.CreatedAt > NotificationLifetime);
        
        StateHasChanged();
    }

    private void HandleNotification(Notification notification)
    {
        Notifications.Add(notification);
        
        // Keep only the most recent notifications
        if (Notifications.Count > MaxNotifications)
        {
            Notifications.RemoveAt(0);
        }
        
        StateHasChanged();
    }

    private float GetHealthPercentage()
    {
        if (Health == null || Health.MaxHealth <= 0)
        {
            return 0f;
        }

        return Math.Max(0f, Math.Min(100f, Health.CurrentHealth / Health.MaxHealth * 100f));
    }

    private string GetHealthDisplay()
    {
        if (Health == null)
        {
            return "0";
        }

        return ((int)Math.Ceiling(Health.CurrentHealth)).ToString();
    }

    private string GetFormattedState()
    {
        var state = GameMode.Instance.StateMachine.CurrentState;

        return "#" + state?.GameObject.Name.ToUpper().Replace("_", " ");
    }

    private string GetFormattedTime()
    {
        var remainingDuration = GameMode.Instance.StateMachine.CurrentState?.RemainingDuration;

        if (remainingDuration == null)
        {
            return "00:00";
        }

        var totalSeconds = (int)Math.Ceiling(remainingDuration.Value);

        if (totalSeconds < 60)
        {
            return totalSeconds.ToString("00");
        }

        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }

    private string GetTimeTextClass()
    {
        if (GameMode.Instance.StateMachine.CurrentState != null &&
            !GameMode.Instance.StateMachine.CurrentState.GameObject.Name.Contains("Playing"))
        {
            return "";
        }

        var remainingDuration = GameMode.Instance.StateMachine.CurrentState?.RemainingDuration;

        if (remainingDuration == null)
        {
            return "";
        }

        var totalSeconds = (int)Math.Ceiling(remainingDuration.Value);

        return totalSeconds < 20 ? "time-urgent" : "";
    }
}