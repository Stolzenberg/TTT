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

            SwitchToSlot(slot);

            return;
        }

        HandleWheelEquipmentChange();
    }

    private void HandleWheelEquipmentChange()
    {
        var wheel = Input.MouseWheel;

        // gamepad input
        if (Input.Pressed("NextSlot"))
        {
            wheel.y = -1;
        }

        if (Input.Pressed("PrevSlot"))
        {
            wheel.y = 1;
        }

        if (wheel.y == 0f)
        {
            return;
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

        var slotDelta = wheel.y > 0f ? 1 : -1;
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

        Switch(weaponToSwitchTo.Value);
    }

    private void SwitchToSlot(EquipmentSlot slot)
    {
        var equipment = Equipments.FirstOrDefault(pair => pair.Key == slot);
        if (!equipment.Value.IsValid())
        {
            return;
        }

        Switch(equipment.Value);
    }

    private void Switch(Equipment equipment)
    {
        if (!equipment.IsValid())
        {
            return;
        }

        if (ActiveEquipment.IsValid())
        {
            if (ActiveEquipment.Resource.Slot == equipment.Resource.Slot)
            {
                return;
            }

            if (ActiveEquipment.IsDeployed)
            {
                ActiveEquipment.Holster();
            }
        }

        ServerSetCurrentEquipment(equipment);
    }
}