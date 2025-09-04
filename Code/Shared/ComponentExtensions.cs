namespace Mountain;

public static class ComponentExtensions
{
    public static Player? GetPlayerFromComponent(this Component component)
    {
        if (component is Player player)
        {
            return player;
        }

        if (!component.IsValid())
        {
            return null;
        }

        return !component.GameObject.IsValid() ? null : component.GameObject.Root.GetComponentInChildren<Player>();
    }
}