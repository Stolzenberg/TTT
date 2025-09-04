namespace Mountain;

public sealed class ItemCatalog
{
    public static ItemResource GetDefinition(ItemType itemType)
    {
        return ResourceLibrary.Get<ItemResource>($"Items/{itemType.ToString()}/{itemType.ToString()}.item");
    }
}