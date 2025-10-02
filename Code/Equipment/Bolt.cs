using Sandbox.Events;

namespace Mountain;

public partial class Boltable : EquipmentInputAction, IGameEventHandler<EquipmentShotEvent>
{
    [Property, Group("Effects")]
    public GameObject EjectionPrefab { get; set; }

    public bool CanBolt { get; private set; }

    protected EquipmentModel Visual
    {
        get
        {
            if (IsProxy || !Equipment.ViewModel.IsValid())
            {
                return Equipment.WorldModel;
            }

            return Equipment.ViewModel;
        }
    }

    void IGameEventHandler<EquipmentShotEvent>.OnGameEvent(EquipmentShotEvent eventArgs)
    {
        CanBolt = true;
    }

    protected override void OnInputUp()
    {
        if (!CanBolt)
        {
            return;
        }

        Equipment.ViewModel.ModelRenderer.Set("b_reload_bolt", true);

        if (!EjectionPrefab.IsValid())
        {
            return;
        }

        if (!Visual.EjectionPort.IsValid())
        {
            return;
        }

        var gameObject = EjectionPrefab.Clone(new CloneConfig
        {
            Parent = Visual.EjectionPort,
            Transform = new(),
            StartEnabled = true,
            Name = $"Bullet ejection: {Equipment.GameObject}",
        });

        gameObject.NetworkSpawn();
    }
}