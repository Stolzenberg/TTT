namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("Collision")]
    public Collider Collider { get; init; } = null!;
}