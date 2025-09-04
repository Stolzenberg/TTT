namespace Mountain;

[GameResource("Item Definition", "item", "A Item Definition.", Category = "Item", Icon = "category")]
public class ItemResource : GameResource
{
    public ItemType Type { get; set; }

    /// <summary>
    ///     The prefab to create and attach to the player when spawning it in.
    /// </summary>
    [Category("Prefabs")]
    public GameObject MainPrefab { get; set; }

    /// <summary>
    ///     A world model that we'll put on the player's arms in third person.
    /// </summary>
    [Category("Prefabs")]
    public GameObject WorldModelPrefab { get; set; }

    /// <summary>
    ///     The prefab to create when making a viewmodel for this equipment.
    /// </summary>
    [Category("Prefabs")]
    public GameObject ViewModelPrefab { get; set; }

    [Hide]
    public string NameKey => $"{Type.ToString().ToUpper()}_NAME";

    [Hide]
    public string DescriptionKey => $"{Type.ToString().ToUpper()}_DESCRIPTION";

    public override string ToString()
    {
        return $"{Type}, NameKey: {NameKey}, DescriptionKey: {DescriptionKey}";
    }
}