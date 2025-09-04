namespace Mountain;

public sealed class WalkMovementState : MovementState
{
    [Property]
    private readonly float speed = 110f;

    public override void AddVelocity()
    {
        Player.WishVelocity = Player.WishVelocity.WithZ(0);
        base.AddVelocity();
    }

    protected override float GetSpeed()
    {
        return speed;
    }
}