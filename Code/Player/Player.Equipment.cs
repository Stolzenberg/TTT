using System;
using Sandbox.Events;

namespace Mountain;

public sealed partial class Player : IGameEventHandler<EquipmentDeployedEvent>, IGameEventHandler<EquipmentHolsteredEvent>
{
    public IEnumerable<Equipment> Equipments => GetComponentsInChildren<Equipment>();

    [Property, Feature("Equipment")]
    public GameObject RightHandSocket { get; init; } = null!;

    [Sync(SyncFlags.FromHost)]
    public Equipment? ActiveEquipment { get; private set; }

    [Sync(SyncFlags.FromHost)]
    public TimeSince TimeSinceEquipmentDeployed { get; private set; }

    void IGameEventHandler<EquipmentDeployedEvent>.OnGameEvent(EquipmentDeployedEvent eventArgs)
    {
        ActiveEquipment = eventArgs.Equipment;
    }

    void IGameEventHandler<EquipmentHolsteredEvent>.OnGameEvent(EquipmentHolsteredEvent eventArgs)
    {
        if (eventArgs.Equipment == ActiveEquipment)
        {
            ActiveEquipment = null;
        }
    }

    public bool Has(ItemResource resource)
    {
        return Equipments.Any(equipment => equipment.Enabled && equipment.Resource == resource);
    }

    [Rpc.Host]
    public void ServerGive(ItemResource item, bool makeActive = true)
    {
        if (Has(item))
        {
            throw new ArgumentException($"Equipment resource {item} already exists in the player's inventory.");
        }

        if (!item.MainPrefab.IsValid())
        {
            throw new ArgumentException($"Equipment resource {item} does not have a valid main prefab.");
        }

        var gameObject = item.MainPrefab.Clone(new CloneConfig
        {
            Transform = new(),
            Parent = GameObject,
        });

        var component = gameObject.GetComponentInChildren<Equipment>(true);
        component.Owner = this;
        gameObject.NetworkSpawn(Network.Owner);

        if (makeActive)
        {
            ServerSetCurrentEquipment(component);
        }
    }

    private void ServerSetCurrentEquipment(Equipment equipment)
    {
        if (equipment == ActiveEquipment)
        {
            return;
        }

        TimeSinceEquipmentDeployed = 0;
        
        ClearCurrentEquipment();
        equipment.Deploy();
    }

    [Rpc.Owner]
    private void SetCurrentEquipment(Equipment equipment)
    {
        if (equipment == ActiveEquipment)
        {
            return;
        }

        ClearCurrentEquipment();
        equipment.Deploy();
    }

    [Rpc.Owner]
    private void ClearCurrentEquipment()
    {
        if (ActiveEquipment.IsValid())
        {
            ActiveEquipment.Holster();
        }
    }
}