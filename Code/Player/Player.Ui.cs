namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("User Interface")]
    public NameTagPanel NameTag { get; init; } = null!;
    
    [Property, Feature("User Interface")]
    public DeathPanel DeathPanel { get; init; } = null!;
}