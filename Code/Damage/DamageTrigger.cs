namespace Mountain;

public sealed class DamageTrigger : Component, Component.ITriggerListener
{
    [Property]
    private float damage = 10f;
    
    [Property]
    private float damageInterval = 1f;
    
    private TimeSince timeSinceDamage;
    private List<GameObject> targets = [];
    
    public void OnTriggerEnter(GameObject other)
    {
        if (!targets.Contains(other))
        {
            targets.Add(other);
        }
    }
    
    public void OnTriggerExit(GameObject other)
    {
        targets.Remove(other);
    }
    
    protected override void OnFixedUpdate()
    {
        if (timeSinceDamage < damageInterval)
        {
            return;
        }
        
        foreach (var gameObject in targets)
        {
            gameObject.ServerTakeDamage(new(this, damage, this));
        }
        
        timeSinceDamage = 0f;
    }
}