namespace Mountain;

public sealed class OpenDeathDialog : Component, IInteraction
{
    private Ragdoll ragdoll;

    protected override void OnStart()
    {
        ragdoll = GetComponent<Ragdoll>();
    }

    public bool Press(IInteraction.Event e)
    {
        Log.Info($"Opening death dialog for player. {ragdoll.DamageInfo}");
        DeathInfoModal.Show(ragdoll.DamageInfo);
        return true;
    }
}