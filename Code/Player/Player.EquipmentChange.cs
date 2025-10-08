using System;

namespace Mountain;

public sealed partial class Player
{
    private void UpdateEquipmentChange()
    {
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            if (slot == EquipmentSlot.Undefined)
            {
                continue;
            }

            if (!Input.Pressed($"Slot{(int)slot}"))
            {
                continue;
            }

            RequestSwitchToSlot(slot);

            return;
        }

        HandleWheelEquipmentChange();
    }

    private void HandleWheelEquipmentChange()
    {
        var wheel = Input.MouseWheel;

        // gamepad input
        if (Input.Pressed("SlotNext"))
        {
            wheel.y = -1;
        }

        if (Input.Pressed("SlotPrev"))
        {
            wheel.y = 1;
        }

        if (wheel.y == 0f)
        {
            return;
        }
        
        RequestWheelSwitch((int)wheel.y);
    }

    [Rpc.Host]
    private void RequestSwitchToSlot(EquipmentSlot slot)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("SwitchToSlot can only be called on the host.");
        }
        
        var equipment = Equipments.FirstOrDefault(pair => pair.Key == slot);
        if (!equipment.Value.IsValid())
        {
            return;
        }
        
        Log.Info($"{Client.DisplayName} requested switching to slot {slot}");

        Switch(equipment.Value);
    }

    [Rpc.Host]
    private void RequestWheelSwitch(int direction)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("RequestWheelSwitch can only be called on the host.");
        }
        
        var availableWeapons = Equipments.OrderBy(x => x.Key).ToList();
        if (availableWeapons.Count == 0)
        {
            return;
        }

        var currentSlot = 0;
        for (var index = 0; index < availableWeapons.Count; index++)
        {
            var weapon = availableWeapons[index];
            if (!weapon.Value.IsDeployed)
            {
                continue;
            }

            currentSlot = index;

            break;
        }

        var slotDelta = direction > 0 ? 1 : -1;
        currentSlot += slotDelta;

        if (currentSlot < 0)
        {
            currentSlot = availableWeapons.Count - 1;
        }
        else if (currentSlot >= availableWeapons.Count)
        {
            currentSlot = 0;
        }

        var weaponToSwitchTo = availableWeapons[currentSlot];
        if (weaponToSwitchTo.Value == ActiveEquipment)
        {
            return;
        }
        
        Log.Info($"{Client.DisplayName} requested wheel switch to slot {currentSlot}");
        
        Switch(weaponToSwitchTo.Value);
    }

    private void Switch(Equipment equipment)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Switch can only be called on the host.");
        }
        
        if (!equipment.IsValid())
        {
            return;
        }

        if (!Equipments.ContainsValue(equipment))
        {
            throw new InvalidOperationException($"{Client.DisplayName} tried to switch to equipment {equipment} they don't have.");
        }

        if (ActiveEquipment.IsValid())
        {
            if (ActiveEquipment.Resource.Slot == equipment.Resource.Slot)
            {
                Log.Info($"{Client.DisplayName} tried to switch to the same equipment {equipment}");
                return;
            }
        }
        
        Log.Info($"{Client.DisplayName} switched to {equipment} from {ActiveEquipment}");

        SetCurrentEquipment(equipment);
    }
}