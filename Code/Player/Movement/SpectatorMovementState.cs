namespace Mountain;

public sealed class SpectatorMovementState : MovementState
{
    public override int Score(Player playerMovement)
    {
        if (playerMovement.Health.State == LifeState.Dead)
        {
            return 1000;
        }

        return 0;
    }
    
    public override void AddVelocity()
    {
        Player.Body.Velocity = Vector3.Zero;
    }

    protected override float GetSpeed()
    {
        return 0;
    }
}