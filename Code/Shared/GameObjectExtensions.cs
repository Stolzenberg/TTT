namespace Mountain;

public static class GameObjectExtensions
{
    [Rpc.Host]
    public static void ServerTakeDamage(this GameObject go, DamageInfo damageInfo)
    {
        foreach (var damageable in go.Root.GetComponents<HealthComponent>())
        {
            damageable.ServerTakeDamage(damageInfo);
        }
    }
    
    public static Player? GetPlayer(this GameObject gameObject)
    {
        if (!gameObject.IsValid())
        {
            return null;
        }

        return !gameObject.IsValid() ? null : gameObject.Root.GetComponentInChildren<Player>();
    }
}