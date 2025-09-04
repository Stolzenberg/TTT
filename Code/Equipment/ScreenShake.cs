using Sandbox.Events;

namespace Mountain;

public class ScreenShakeOnShot : EquipmentComponent, IGameEventHandler<EquipmentShotEvent>
{
    [Property]
    public float Length { get; set; } = 0.3f;
    [Property]
    public float Size { get; set; } = 1.05f;

    void IGameEventHandler<EquipmentShotEvent>.OnGameEvent(EquipmentShotEvent eventArgs)
    {
        var shake = new ScreenShake.Random(Length, Size);
        ScreenShaker.Main.Add(shake);
    }
}