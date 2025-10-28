namespace Mountain;

public sealed partial class Player
{
    /// <summary>
    /// Are we possessing this pawn right now? (Clientside)
    /// </summary>
    public bool IsPossessed => Current == this;

    /// <summary>
    /// Is this pawn locally controlled by us?
    /// </summary>
    public bool IsLocallyControlled => IsPossessed && !IsProxy && !Client.IsBot;
    private static Player? Current { get; set; }

    public void Possess()
    {
        Possess(this);
    }

    public void DePossess()
    {
        DePossess(this);
    }

    private static void Possess(Player player)
    {
        if (player.IsPossessed)
        {
            Log.Warning("Trying to possess a player that is already possessed by you.");

            return;
        }

        if (Current.IsValid())
        {
            Log.Info($"Player.Posses: Current: {Current.ToString()}");
            DePossess(Current);
        }

        Current = player;
        Client.Possess(player);

        Current.ApplyClothing();
        Current.ActiveEquipment?.CreateViewModel(false);
        Current.SetHeadPosition();

        Log.Info($"Player.Posses: Possessing {player.ToString()}");
    }

    private static void DePossess(Player player)
    {
        var wasPossessed = player.IsValid() && player.IsPossessed;

        Current = null;
        Client.Unpossess();

        if (!wasPossessed)
        {
            Log.Warning($"Trying to depossess a player {player} that is not possessed by you.");

            return;
        }

        player.ApplyClothing();
        player.ActiveEquipment?.DestroyViewModel();

        Log.Info($"De-possessing {player}");
    }
}