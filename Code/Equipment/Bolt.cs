using Sandbox.Events;

namespace Mountain;

public partial class Boltable : EquipmentInputAction, IGameEventHandler<EquipmentShotEvent>
{
    [Property, Group("Effects")]
    public GameObject EjectionPrefab { get; set; }
    
    [Property]
    public float BoltTime { get; set; } = 0.5f;

    [Sync]
    public bool Bolting { get; private set; }

    [Sync]
    public bool HasToBolt { get; private set; }
    
    private TimeSince timeSinceBolt;

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
    
    protected override void OnEnabled()
    {
        BindTag("bolting", () => Bolting);
        BindTag("has_to_bolt", () => HasToBolt);
    }
    
    protected override void OnUpdate()
    {
        if (!Player.IsValid())
        {
            return;
        }

        if (Player.IsProxy)
        {
            return;
        }

        if (!Bolting)
        {
            return;
        }
        
        if (timeSinceBolt <= BoltTime)
        {
            return;
        }

        Bolting = false;
        HasToBolt = false;
    }

    protected override void OnInputUp()
    {
        if (!HasToBolt)
        {
            return;
        }
        
        if (timeSinceBolt <= BoltTime)
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

        timeSinceBolt = 0;
        Bolting = true;
        
        gameObject.NetworkSpawn();
    }

    public void OnGameEvent(EquipmentShotEvent eventArgs)
    {
        HasToBolt = true;
    }
}