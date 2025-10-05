using System;

namespace Mountain;

public sealed class GameStateMachine : SingletonComponent<GameStateMachine>
{
    /// <summary>
    ///     Which state is currently active?
    /// </summary>
    [Property, Sync(SyncFlags.FromHost)]
    public GameState? CurrentState
    {
        get => currentState;
        private set
        {
            if (currentState == value)
            {
                return;
            }

            currentState = value;

            if (!Networking.IsHost)
            {
                EnableActiveStates(false);
            }
        }
    }

    /// <summary>
    ///     Which state will we transition to next, at <see cref="NextStateTime" />?
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public GameState? NextState { get; set; }

    /// <summary>
    ///     What time will we transition to <see cref="NextState" />?
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public float NextStateTime { get; set; }

    /// <summary>
    ///     All states found on descendant objects.
    /// </summary>
    public IEnumerable<GameState> States =>
        Components.GetAll<GameState>(FindMode.EverythingInSelfAndDescendants);

    /// <summary>
    ///     How many instant state transitions in a row until we throw an error?
    /// </summary>
    public const int MaxInstantTransitions = 16;
    private GameState? currentState;

    /// <summary>
    ///     Queue up a transition to the given state. This will occur at the end of
    ///     a fixed update on the state machine.
    /// </summary>
    public void Transition(GameState next, float delaySeconds = 0f)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Cannot transition states from a non-host client.");
        }

        NextState = next;
        NextStateTime = Time.Now + delaySeconds;
    }

    /// <summary>
    ///     Removes any pending transitions, so this state machine will remain in the
    ///     current state until another transition is queued with <see cref="Transition" />.
    /// </summary>
    public void ClearTransition()
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Cannot transition states from a non-host client.");
        }
        
        NextState = null;
        NextStateTime = float.PositiveInfinity;
    }

    protected override void OnStart()
    {
        foreach (var state in States)
        {
            state.Enabled = false;
            state.GameObject.Enabled = state.GameObject == GameObject;
        }

        if (Networking.IsHost && CurrentState is { } current)
        {
            Transition(current);
        }
    }

    protected override void OnFixedUpdate()
    {
        if (!Networking.IsHost)
        {
            return;
        }

        if (CurrentState is not { } current)
        {
            return;
        }

        current.Update();

        var transitions = 0;

        while (transitions++ < MaxInstantTransitions)
        {
            if (NextState is not { } next || !(Time.Now >= NextStateTime))
            {
                return;
            }

            if (next.DefaultNextState is not null)
            {
                Transition(next.DefaultNextState, next.DefaultDuration);
            }
            else
            {
                ClearTransition();
            }

            CurrentState = next;

            EnableActiveStates(true);
        }
    }

    private void EnableActiveStates(bool dispatch)
    {
        var current = CurrentState;
        var active = current?.GetAncestors() ?? [];
        var activeSet = active.ToHashSet();

        var toDeactivate = new Queue<GameState>(States.Where(x => x.Enabled && !activeSet.Contains(x)).Reverse());
        var toActivate = new Queue<GameState>(active.Where(x => !x.Enabled));

        if (current != null)
        {
            toActivate.Enqueue(current);
        }

        while (toDeactivate.TryDequeue(out var next))
        {
            next.Leave(dispatch);

            if (toDeactivate.All(x => x.GameObject != next.GameObject) &&
                toActivate.All(x => x.GameObject != next.GameObject))
            {
                next.GameObject.Enabled = false;
            }
        }

        while (toActivate.TryDequeue(out var next))
        {
            next.GameObject.Enabled = true;

            next.Enter(dispatch);
        }
    }
}