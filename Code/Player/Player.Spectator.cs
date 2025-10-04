namespace Mountain;

public sealed partial class Player
{
    /// <summary>
    /// The player we are currently spectating (null for free camera)
    /// </summary>
    [Sync]
    public Player? SpectatorTarget { get; set; }

    /// <summary>
    /// Index of the current spectator target in the list of living players
    /// </summary>
    private int spectatorTargetIndex = 0;

    /// <summary>
    /// Update spectator input and camera logic
    /// </summary>
    private void UpdateSpectator()
    {
        if (!Client.IsLocalClient || Health.State != LifeState.Dead)
        {
            SpectatorTarget = null;
            return;
        }
        
        // Handle spectator cycling input
        if (Input.Pressed("Right"))
        {
            CycleSpectatorTarget(1);
        }
        else if (Input.Pressed("Left"))
        {
            CycleSpectatorTarget(-1);
        }
        
        if (SpectatorTarget.IsValid() && SpectatorTarget.Health.State == LifeState.Dead)
        {
            // Spectated player died, switch to next target
            CycleSpectatorTarget(1);
        }
        
        UpdateSpectatorCamera();
    }

    /// <summary>
    /// Cycle to the next or previous spectator target
    /// </summary>
    /// <param name="direction">1 for next, -1 for previous</param>
    private void CycleSpectatorTarget(int direction)
    {
        var livingPlayers = Scene.GetLivingPlayers(this).ToList();
        
        if (livingPlayers.Count == 0)
        {
            SpectatorTarget = null;
            return;
        }

        // If we don't have a current target, start with the first player
        if (SpectatorTarget == null || !livingPlayers.Contains(SpectatorTarget))
        {
            spectatorTargetIndex = 0;
        }
        else
        {
            // Find current target index and move in the specified direction
            var currentIndex = livingPlayers.IndexOf(SpectatorTarget);
            if (currentIndex >= 0)
            {
                spectatorTargetIndex = currentIndex;
            }
        }

        // Cycle through the list
        spectatorTargetIndex = (spectatorTargetIndex + direction) % livingPlayers.Count;
        if (spectatorTargetIndex < 0)
        {
            spectatorTargetIndex = livingPlayers.Count - 1;
        }

        SpectatorTarget = livingPlayers[spectatorTargetIndex];
        SpectatorTarget.Possess();
    }

    /// <summary>
    /// Update the spectator camera to follow the target player
    /// </summary>
    private void UpdateSpectatorCamera()
    {
        if (!Client.IsLocalClient || Health.State != LifeState.Dead || !Camera.IsValid())
        {
            return;
        }

        // If we have a spectator target, position camera at their head
        if (!SpectatorTarget.IsValid() || !SpectatorTarget.head.IsValid())
        {
            return;
        }

        Camera.WorldPosition = SpectatorTarget.head.WorldPosition;
        Camera.WorldRotation = SpectatorTarget.EyeAngles;
            
        // Copy their field of view settings
        Camera.FieldOfView = SpectatorTarget.Camera?.FieldOfView ?? Preferences.FieldOfView;
    }
}
