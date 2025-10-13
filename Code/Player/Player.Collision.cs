namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("Collision")]
    public CapsuleCollider Collider { get; init; } = null!;
}