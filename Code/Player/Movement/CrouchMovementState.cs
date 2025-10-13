namespace Mountain;

public sealed class CrouchMovementState : MovementState
{
    [Property]
    private readonly float speed = 80f;
    
    [Property]
    private readonly float crouchSize = 32f;

    private float defaultSize;

    public override int Score(Player playerMovement)
    {
        return Input.Down("Duck") ? 100 : 0;
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

    public override void OnStateBegin()
    {
        base.OnStateBegin();
        defaultSize = Player.Collider.Start.z;
        Player.Collider.Start = new(0, 0, crouchSize);
    }

    public override void OnStateEnd(MovementState? next)
    {
        base.OnStateEnd(next);
        Player.Collider.Start = new(0, 0, defaultSize);
    }
}