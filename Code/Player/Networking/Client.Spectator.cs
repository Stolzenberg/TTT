namespace Mountain;

public sealed partial class Client
{
    /// <summary>
    /// The player we are currently spectating
    /// </summary>
    [Sync]
    public Player? SpectatorTarget { get; set; }

    /// <summary>
    /// Index of the current spectator target in the list of living players
    /// </summary>
    private int spectatorTargetIndex = 0;

    /// <summary>
    /// Cycle to the next or previous spectator target
    /// </summary>
    /// <param name="direction">1 for next, -1 for previous</param>
    public void CycleSpectatorTarget(int direction)
    {
        var livingPlayers = Scene.GetLivingPlayers(Player).ToList();

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
        Log.Info("Spectating player: " + SpectatorTarget.Client.DisplayName);

        SpectatorTarget.Possess();
    }

    /// <summary>
    /// Update spectator input and camera logic
    /// </summary>
    private void UpdateSpectator()
    {
        if (!IsLocalClient)
        {
            return;
        }

        if (!Player.IsValid() || Player.Health.State != LifeState.Dead)
        {
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
    }
}