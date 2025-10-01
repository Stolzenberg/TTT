using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Skip to the next state if enough players are connected.
/// </summary>
public sealed class WaitForPlayers : Component, IGameEventHandler<EnterStateEvent>, IGameEventHandler<UpdateStateEvent>
{
    [RequireComponent]
    public GameState State { get; private set; }

    /// <summary>
    ///     Only start the game if there are at least this many players.
    /// </summary>
    [Property, Sync(SyncFlags.FromHost)]
    public int MinPlayerCount { get; set; } = 2;

    [Sync(SyncFlags.FromHost)]
    public bool IsPostponed { get; set; }
    
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        IsPostponed = false;
    }

    void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
    {
        var clientsCount = Game.ActiveScene.AllClients().Count();

        if (!IsPostponed && clientsCount >= MinPlayerCount)
        {
            return;
        }

        if (!Game.ActiveScene.AllClientsReady())
        {
            Log.Info("Waiting for clients to be ready...");
            GameMode.Instance.StateMachine.Transition( eventArgs.State.DefaultNextState!, eventArgs.State.DefaultDuration );
        }

        GameMode.Instance.StateMachine.Transition( eventArgs.State.DefaultNextState!, eventArgs.State.DefaultDuration );
    }

    private void Toggle()
    {
        if (IsPostponed)
        {
            Restart();
        }
        else
        {
            Postpone();
        }
    }

    private void Postpone()
    {
        IsPostponed = true;
    }

    private void Restart()
    {
        State.Transition(State);
    }
}