namespace Mountain;

public sealed class RunMovementState : MovementState
{
    [Property]
    private readonly float speed = 220f;

    public override int Score(Player playerMovement)
    {
        if (playerMovement.IsSprinting)
        {
            return 100;
        }

        return 0;
    }

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