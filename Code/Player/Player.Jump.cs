namespace Mountain;

public sealed partial class Player
{
    [Property, Feature("Jump")]
    private readonly float jumpForce = 330f;
    
    private void HandleJump()
    {
        if (!Input.Pressed("Jump") || !IsOnGround || IsFrozen)
        {
            return;
        }

        ClearGround();

        Body.Velocity += new Vector3(0, 0, jumpForce);
        BroadcastPlayerJumped();
    }

    [Rpc.Broadcast]
    private void BroadcastPlayerJumped()
    {
        TriggerJump();
    }
}