using System;
using Sandbox.Events;

namespace Mountain;

public sealed partial class Player : IGameEventHandler<EquipmentDeployedEvent>,
    IGameEventHandler<EquipmentHolsteredEvent>
{
    public Dictionary<EquipmentSlot, Equipment> Equipments => GetComponentsInChildren<Equipment>()
        .ToDictionary(e => e.Resource.Slot);

    [Property, Feature("Equipment")]
    public GameObject RightHandSocket { get; init; } = null!;

    [Property, Feature("Equipment")]
    public EquipmentResource[] DefaultEquipments { get; private set; } = [];

    [Sync(SyncFlags.FromHost)]
    public Equipment? ActiveEquipment { get; private set; }

    [Sync(SyncFlags.FromHost)]
    public TimeSince TimeSinceEquipmentDeployed { get; private set; }

    // Gets called form child of player (the equipment) when deployed
    void IGameEventHandler<EquipmentDeployedEvent>.OnGameEvent(EquipmentDeployedEvent eventArgs)
    {
        ActiveEquipment = eventArgs.Equipment;
    }

    // Gets called form child of player (the equipment) when holstered  
    void IGameEventHandler<EquipmentHolsteredEvent>.OnGameEvent(EquipmentHolsteredEvent eventArgs)
    {
        if (eventArgs.Equipment == ActiveEquipment)
        {
            ActiveEquipment = null;
        }
    }

    public bool Has(EquipmentResource resource)
    {
        return Equipments.Any(pair =>
            pair.Value.Enabled && pair.Value.Resource == resource || pair.Key == resource.Slot);
    }

    [Rpc.Host]
    private void ServerRemoveEquipment(Equipment equipment)
    {
        if (equipment == ActiveEquipment)
        {
            var otherEquipment = Equipments.Where(pair => pair.Value != equipment);
            var orderedBySlot = otherEquipment.OrderBy(pair => pair.Key);
            var targetWeapon = orderedBySlot.FirstOrDefault();

            if (targetWeapon.Value.IsValid())
            {
                Switch(targetWeapon.Value);
            }
            else
            {
                ClearCurrentEquipment();
                ActiveEquipment = null;
            }
        }

        equipment.GameObject.Destroy();
    }

    public Equipment ServerGive(EquipmentResource equipment, bool makeActive = true)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("ServerGive can only be called on the host.");
        }

        if (Has(equipment))
        {
            throw new ArgumentException($"Equipment resource {equipment} already exists in the player's inventory.");
        }

        if (!equipment.MainPrefab.IsValid())
        {
            throw new ArgumentException($"Equipment resource {equipment} does not have a valid main prefab.");
        }

        var gameObject = equipment.MainPrefab.Clone(new CloneConfig
        {
            Transform = new(),
            Parent = GameObject,
        });

        var component = gameObject.GetComponentInChildren<Equipment>(true);
        component.Owner = this;
        gameObject.NetworkSpawn(Network.Owner);

        Log.Info($"{this} was given equipment {equipment}.");

        if (makeActive)
        {
            ServerSetCurrentEquipment(component);
        }

        return component;
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
    private void ClearCurrentEquipment()
    {
        if (ActiveEquipment.IsValid())
        {
            ActiveEquipment.Holster();
        }
    }

    private void ServerGiveDefaultEquipment()
    {
        foreach (var equipment in DefaultEquipments)
        {
            ServerGive(equipment, false);
        }
        
        var firstEquipment = Equipments.OrderBy(pair => pair.Key).FirstOrDefault();
        if (firstEquipment.Value.IsValid())
        {
            ServerSetCurrentEquipment(firstEquipment.Value);
        }
    }
    
    private void ServerRemoveAllEquipments()
    {
        foreach (var equipment in Equipments.Values.ToArray())
        {
            ServerRemoveEquipment(equipment);
        }
    }
}