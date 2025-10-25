using System;
using Sandbox.Events;

namespace Mountain;

public partial class NameTagPanel : PanelComponent, IGameEventHandler<KillEvent>
{
    [Property, Group("Distance Settings"),
     Description(
         "Maximum distance (in units) at which nametags are visible. Beyond this, nametags are completely hidden."),
     Range(100f, 5000f)]
    public float MaxVisibleDistance { get; set; } = 1000f;

    [Property, Group("Distance Settings"),
     Description(
         "Distance (in units) at which nametags begin to fade. Below this distance, nametags are at full opacity."),
     Range(50f, 2000f)]
    public float FadeStartDistance { get; set; } = 500f;

    [Property, Group("Feature Toggles"), Description("Enable or disable distance-based fading of nametags.")]
    public bool EnableDistanceFade { get; set; } = true;

    private Player NameTagOwningPlayer => this.GetPlayerFromComponent() ??
                                          throw new InvalidOperationException(
                                              "NameTagPanel must be a child of a player.");

    private IlluminationSensor OwnersIlluminationSensor =>
        NameTagOwningPlayer.GetComponent<IlluminationSensor>() ??
        throw new InvalidOperationException("NameTagPanel's owning player must have an IlluminationSensor component.");
    private float CurrentOpacity { get; set; } = 1f;

    void IGameEventHandler<KillEvent>.OnGameEvent(KillEvent eventArgs)
    {
        GameObject.Destroy();
    }

    protected override int BuildHash()
    {
        return HashCode.Combine(NameTagOwningPlayer.Client.DisplayName, CurrentOpacity);
    }

    protected override void OnUpdate()
    {
        UpdateOpacity();
    }

    private void UpdateOpacity()
    {
        var viewer = GetViewerPlayer();
        if (!viewer.IsValid())
        {
            CurrentOpacity = 0f;

            return;
        }

        var opacity = 1f;

        // Calculate distance-based fade
        if (EnableDistanceFade)
        {
            var distance = Vector3.DistanceBetween(viewer.WorldPosition, NameTagOwningPlayer.WorldPosition);

            if (distance >= MaxVisibleDistance)
            {
                CurrentOpacity = 0f;

                return;
            }

            if (distance > FadeStartDistance)
            {
                var fadeRange = MaxVisibleDistance - FadeStartDistance;
                var fadeAmount = (distance - FadeStartDistance) / fadeRange;
                opacity *= 1f - fadeAmount;
            }
        }

        if (!OwnersIlluminationSensor.IsWellLit())
        {
            opacity *= OwnersIlluminationSensor.IlluminationLevel;
        }

        CurrentOpacity = opacity;
        StateHasChanged();
    }

    private Player? GetViewerPlayer()
    {

        // Find the player that owns this camera
        return Scene.GetAllComponents<Player>().FirstOrDefault(p => p.IsValid() && p.IsPossessed);
    }
}