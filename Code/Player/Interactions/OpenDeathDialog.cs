namespace Mountain;

public sealed class OpenDeathDialog : Component, IInteraction
{
    private Player player;

    protected override void OnStart()
    {
        player = this.GetPlayerFromComponent() ?? throw new System.InvalidOperationException("OpenDeathDialog must be a child of a Player.");
    }
    
    public bool CanPress(IInteraction.Event e)
    {
        return player.Health.State == LifeState.Dead;
    }
    
    public bool Press(IInteraction.Event e)
    {
        throw new System.NotImplementedException();
    }
}