namespace Mountain;

public sealed class OpenDeathDialog : Component, IInteraction
{
    [Property]
    private readonly float interactionDistance = 128f;
    private bool isOpen;

    private Ragdoll ragdoll;

    public bool CanPress(IInteraction.Event e)
    {
        if (!Client.Local.IsLocalClient)
        {
            return false;
        }

        if (isOpen)
        {
            return false;
        }

        if (GameMode.Instance.StateMachine.CurrentState != null &&
            !GameMode.Instance.StateMachine.CurrentState.GameObject.Name.Contains("Playing"))
        {
            return false;
        }

        return true;
    }

    public bool Press(IInteraction.Event e)
    {
        Log.Info($"Opening death dialog for player. {ragdoll.DamageInfo}");
        DeathInfoModal.Show(ragdoll.DamageInfo, ragdoll.Client);
        isOpen = true;

        return true;
    }

    protected override void OnStart()
    {
        ragdoll = GetComponent<Ragdoll>();
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