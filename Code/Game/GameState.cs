using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Event dispatched on the server when a <see cref="GameStateMachine" /> changes state.
///     Only invoked on components on the same object as the new state.
/// </summary>
public record EnterStateEvent(GameState State) : IGameEvent;

/// <summary>
///     Event dispatched on the server when a <see cref="GameStateMachine" /> changes state.
///     Only invoked on components on the same object as the old state.
/// </summary>
public record LeaveStateEvent(GameState State) : IGameEvent;

/// <summary>
///     Event dispatched on the server every fixed update while a <see cref="GameStateMachine" /> is active.
///     Only invoked on components on the same object as the state.
/// </summary>
public record UpdateStateEvent(GameState State) : IGameEvent;

public sealed class GameState : Component
{
	/// <summary>
	///     Which state machine does this state belong to?
	/// </summary>
	public GameStateMachine StateMachine =>
        stateMachine ??= Components.GetInAncestorsOrSelf<GameStateMachine>();

	/// <summary>
	///     Which state is this nested in, if any?
	/// </summary>
	public GameState? Parent => Components.GetInAncestors<GameState>(true);

	/// <summary>
	///     Transition to this state by default.
	/// </summary>
	[Property]
    public GameState? DefaultNextState { get; set; }

	/// <summary>
	///     If <see cref="DefaultNextState" /> is given, transition after this delay in seconds.
	/// </summary>
	[Property, HideIf(nameof(DefaultNextState), null)]
    public float DefaultDuration { get; set; }
    private GameStateMachine? stateMachine;
    
    public float RemainingDuration => DefaultDuration - Time.Now + (GameMode.Instance.StateMachine.NextStateTime - DefaultDuration);

    /// <summary>
    ///     Queue up a transition to the given state. This will occur at the end of
    ///     a fixed update on the state machine.
    /// </summary>
    public void Transition(GameState next, float delaySeconds = 0f)
    {
        StateMachine.Transition(next, delaySeconds);
    }

    /// <summary>
    ///     Queue up a transition to the default next state.
    /// </summary>
    public void Transition()
    {
        StateMachine.Transition(DefaultNextState!);
    }

    internal void Enter(bool dispatch)
    {
        Enabled = true;

        if (dispatch)
        {
            GameObject.Dispatch(new EnterStateEvent(this));
        }
    }

    internal void Update()
    {
        Scene.Dispatch(new UpdateStateEvent(this));
    }

    internal void Leave(bool dispatch)
    {
        if (dispatch)
        {
            GameObject.Dispatch(new LeaveStateEvent(this));
        }

        Enabled = false;
    }

    internal IReadOnlyList<GameState> GetAncestors()
    {
        var list = new List<GameState>();

        var parent = Parent;

        while (parent != null)
        {
            list.Add(parent);
            parent = parent.Parent;
        }

        list.Reverse();

        return list;
    }
}