namespace Mountain;

public sealed partial class Player
{
    private void UpdateInput()
    {
        InputMove(Input.AnalogMove);
        ToggleSprinting(Input.Down("run"));
        ToggleAttack(Input.Down("Attack1"));
    }
}