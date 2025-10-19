namespace Mountain;

public sealed class OpenDeathDialog : Component, IInteraction
{
    [Property]
    private readonly float interactionDistance = 128f;
    
    private Ragdoll ragdoll;
    private bool isOpen;

    protected override void OnStart()
    {
        ragdoll = GetComponent<Ragdoll>();
    }

    public bool Press(IInteraction.Event e)
    {
        Log.Info($"Opening death dialog for player. {ragdoll.DamageInfo}");
        DeathInfoModal.Show(ragdoll.DamageInfo, ragdoll.Client);
        isOpen = true;
        return true;
    }
    
    protected override void OnUpdate()
    {
        if (!isOpen)
        {
            return;
        }
        
        if (!Client.Local.IsLocalClient)
        {
            return;
        }
        
        if (!Client.Local.Player.IsValid())
        {
            return;
        }

        var distance = WorldPosition.Distance(Client.Local.Player.WorldPosition);
        if (distance < interactionDistance)
        {
            return;
        }

        DeathInfoModal.Close();
        isOpen = false;
    }
}