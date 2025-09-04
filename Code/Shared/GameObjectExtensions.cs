namespace Mountain;

public static class GameObjectExtensions
{
	public static void ServerTakeDamage(this GameObject go, DamageInfo damageInfo)
    {
        foreach (var damageable in go.Root.GetComponents<HealthComponent>())
        {
            damageable.ServerTakeDamage(damageInfo);
        }
    }
}