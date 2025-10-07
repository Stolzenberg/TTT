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

    private void RemoveEquipment(Equipment equipment)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("RemoveEquipment can only be called on the host.");
        }
        
        if (equipment == ActiveEquipment)
        {
            var otherEquipment = Equipments.Where(pair => pair.Value != equipment);
            var orderedBySlot = otherEquipment.OrderBy(pair => pair.Key);
            var targetWeapon = orderedBySlot.FirstOrDefault();

            if (targetWeapon.Value.IsValid())
            {
                Switch(targetWeapon.Value);
            }
        }

        // BUG: When we destroy the gameobject here it is to early when dropping a weapon to update other players world model.
        // Other players wont get the update because the equipment.IsDeployed = false will not get send anymore.
        // Idea: We have to wait until the equipment switch is done
        
        equipment.GameObject.Destroy();
        equipment.Enabled = false;
        
        Log.Info($"{Client.DisplayName} removed equipment {equipment}.");
    }

    public Equipment Give(EquipmentResource equipmentResource, bool makeActive = true)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Give can only be called on the host.");
        }

        if (Has(equipmentResource))
        {
            throw new ArgumentException($"Equipment resource {equipmentResource} already exists in the player's inventory.");
        }

        if (!equipmentResource.MainPrefab.IsValid())
        {
            throw new ArgumentException($"Equipment resource {equipmentResource} does not have a valid main prefab.");
        }

        var gameObject = equipmentResource.MainPrefab.Clone(new CloneConfig
        {
            Transform = new(),
            Parent = GameObject,
        });

        var equipment = gameObject.GetComponentInChildren<Equipment>(true);
        equipment.Owner = this;
        gameObject.NetworkSpawn(Network.Owner);

        Log.Info($"{Client.DisplayName} was given equipment {equipment}.");

        if (makeActive)
        {
            SetCurrentEquipment(equipment);
        }

        return equipment;
    }

    private void SetCurrentEquipment(Equipment equipment)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("SetCurrentEquipment can only be called on the host.");
        }
        
        if (equipment == ActiveEquipment)
        {
            return;
        }
        
        if (!Equipments.ContainsValue(equipment))
        {
            throw new InvalidOperationException($"{Client.DisplayName} tried to set current equipment {equipment} they don't have.");
        }
        
        HolsterCurrentEquipment();

        TimeSinceEquipmentDeployed = 0;

        equipment.Deploy();
    }

    [Rpc.Owner]
    private void HolsterCurrentEquipment()
    {
        Log.Info("Trying to holster current equipment");
        
        if (!ActiveEquipment.IsValid())
        {
            Log.Info("No active equipment to holster");
            return;    
        }
        
        Log.Info($"Holster current equipment {ActiveEquipment}.");
        ActiveEquipment.Holster();
    }

    private void GiveDefaultEquipment()
    {
        foreach (var equipment in DefaultEquipments)
        {
            Give(equipment, false);
        }
        
        var firstEquipment = Equipments.OrderBy(pair => pair.Key).FirstOrDefault();
        if (firstEquipment.Value.IsValid())
        {
            SetCurrentEquipment(firstEquipment.Value);
        }
    }
}