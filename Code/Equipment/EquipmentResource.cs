namespace Mountain;

[AssetType( Name = "Equipment Resource", Extension = "eqpt", Category = "Equipment" )]
public partial class EquipmentResource : GameResource
{
    public EquipmentSlot Slot { get; set; } = EquipmentSlot.Undefined;

    /// <summary>
    ///     The prefab to create and attach to the player when spawning it in.
    /// </summary>
    public GameObject MainPrefab { get; set; }

    /// <summary>
    ///     A world model that we'll put on the player's arms in third person.
    /// </summary>
    public GameObject? WorldModelPrefab { get; set; }

    /// <summary>
    ///     The prefab to create when making a viewmodel for this equipment.
    /// </summary>
    public GameObject ViewModelPrefab { get; set; }

    /// <summary>
    ///     The prefab to create when making a dropped model for this equipment.
    /// </summary>
    public GameObject DroppedWorldModelPrefab { get; set; }

    [Hide]
    public string NameKey => $"{ResourceName.ToUpper()}_NAME";

    [Hide]
    public string DescriptionKey => $"{ResourceName.ToUpper()}_DESCRIPTION";

    protected override Bitmap CreateAssetTypeIcon( int width, int height )
    {
        return CreateSimpleAssetTypeIcon("receipt", width, height, "#fdea60", "black");
    }

    public override string ToString()
    {
        return $"{ResourceName}, NameKey: {NameKey}, DescriptionKey: {DescriptionKey}";
    }
}