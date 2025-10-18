namespace Mountain;

public sealed class CrouchMovementState : MovementState
{
    [Property]
    private readonly float speed = 80f;
    
    [Property]
    private readonly float colliderSize = 32f;
    
    [Property]
    private readonly float headHeightModifier = 0.5f;

    private float defaultSize;

    public override int Score(Player playerMovement)
    {
        if (!Player.IsOnGround)
        {
            return 0;
        }
        
        return Input.Down("Duck") ? Priority : 0;
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
        Player.Collider.Start = new(0, 0, colliderSize);
        Player.SetHeadPosition(headHeightModifier);
    }

    public override void OnStateEnd(MovementState? next)
    {
        base.OnStateEnd(next);
        Player.Collider.Start = new(0, 0, defaultSize);
        Player.SetHeadPosition();
    }
}