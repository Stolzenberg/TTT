namespace Mountain;

public partial class Player
{
    public bool IsAttacking { get; private set; }

    private void ToggleAttack(bool isAttacking)
    {
        IsAttacking = isAttacking;
    }
}