namespace Mountain;

[AssetType(Name = "Equipment Resource", Extension = "eqpt", Category = "Equipment")]
public partial class EquipmentResource : GameResource
{
    /// <summary>
    ///     The prefab to create and attach to the player when spawning it in.
    /// </summary>
    [Category("Prefab")]
    public GameObject MainPrefab { get; set; }

    /// <summary>
    ///     A world model that we'll put on the player's arms in third person.
    /// </summary>
    [Category("Prefab")]
    public GameObject? WorldModelPrefab { get; set; }

    /// <summary>
    ///     The prefab to create when making a viewmodel for this equipment.
    /// </summary>
    [Category("Prefab")]
    public GameObject ViewModelPrefab { get; set; }

    [Category("Information")]
    public EquipmentSlot Slot { get; set; } = EquipmentSlot.Undefined;

    [Category("Information")]
    public Model WorldModel { get; set; }

    [Category("Information")]
    public Model ViewModel { get; set; }

    [Category("Information")]
    public string MuzzleBoneName { get; set; } = "muzzle";

    [Category("Information")]
    public string EjectionPortBoneName { get; set; } = "ejection_port";

    [Category("Information")]
    public AmmoType AmmoType { get; set; } = AmmoType.None;

    [Hide]
    public string NameKey => $"{ResourceName.ToUpper()}_NAME";

    [Hide]
    public string DescriptionKey => $"{ResourceName.ToUpper()}_DESCRIPTION";

    public override string ToString()
    {
        return $"{ResourceName}, NameKey: {NameKey}, DescriptionKey: {DescriptionKey}";
    }

    protected override Bitmap CreateAssetTypeIcon(int width, int height)
    {
        return CreateSimpleAssetTypeIcon("receipt", width, height, "#fdea60", "black");
    }
}