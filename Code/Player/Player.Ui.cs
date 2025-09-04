namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("User Interface")]
    public NameTagPanel NameTag { get; init; } = null!;
}