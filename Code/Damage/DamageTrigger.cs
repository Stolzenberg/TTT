namespace Mountain;

public sealed class DamageTrigger : Component, Component.ITriggerListener
{
    [Property]
    private float damage = 10f;

    public void OnTriggerEnter(GameObject other)
    {
        other.ServerTakeDamage(new()
        {
            Attacker = this,
            Damage = damage,
        });
    }
}