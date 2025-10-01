namespace Mountain;

public sealed class EquipmentCatalog
{
    public static EquipmentResource GetDefinition(EquipmentType equipmentType)
    {
        return ResourceLibrary.Get<EquipmentResource>($"Equipments/{equipmentType.ToString()}/{equipmentType.ToString()}.eqpt");
    }
}