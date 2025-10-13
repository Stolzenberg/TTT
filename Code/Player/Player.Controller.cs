namespace Mountain;

public sealed partial class Player
{
    private void UpdateInput()
    {
        InputMove(Input.AnalogMove);
    }
}