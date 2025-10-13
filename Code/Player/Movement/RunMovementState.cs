namespace Mountain;

public sealed class RunMovementState : MovementState
{
    [Property]
    private readonly float speed = 220f;

    public override int Score(Player playerMovement)
    {
        return Input.Down("run") ? 100 : 0;
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