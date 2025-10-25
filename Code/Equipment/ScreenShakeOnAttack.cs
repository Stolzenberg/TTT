using Sandbox.Events;

namespace Mountain;

public class ScreenShakeOnAttack : EquipmentComponent, IGameEventHandler<EquipmentShotEvent>,
    IGameEventHandler<EquipmentMeleeAttackEvent>
{
    [Property]
    public float Length { get; set; } = 0.3f;
    [Property]
    public float Size { get; set; } = 1.05f;

    void IGameEventHandler<EquipmentMeleeAttackEvent>.OnGameEvent(EquipmentMeleeAttackEvent eventArgs)
    {
        var shake = new ScreenShake.Random(Length, Size);
        ScreenShaker.Main.Add(shake);
    }

    void IGameEventHandler<EquipmentShotEvent>.OnGameEvent(EquipmentShotEvent eventArgs)
    {
        var shake = new ScreenShake.Random(Length, Size);
        ScreenShaker.Main.Add(shake);
    }
}