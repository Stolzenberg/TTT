using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Event dispatched on the host when <see cref="RoundCounter" /> should be incremented.
/// </summary>
public record RoundCounterIncrementedEvent : IGameEvent;

/// <summary>
///     Keep track of how many rounds have been played.
/// </summary>
public sealed class RoundCounter : Component,
    IGameEventHandler<RoundCounterIncrementedEvent>
{
	/// <summary>
	///     Current round number, starting at 1.
	/// </summary>
	[Sync(SyncFlags.FromHost), Change(nameof(OnRoundChanged))]
    public int Round { get; set; }

    [Early]
    void IGameEventHandler<RoundCounterIncrementedEvent>.OnGameEvent(RoundCounterIncrementedEvent eventArgs)
    {
        Round += 1;
    }

    private void OnRoundChanged(int oldValue, int newValue)
    {
        Log.Info($"### Round {newValue}");
    }
}

/// <summary>
///     Increments <see cref="RoundCounter" /> when this state is entered.
/// </summary>
public sealed class IncrementRoundCounter : Component, IGameEventHandler<EnterStateEvent>
{
    public void OnGameEvent(EnterStateEvent eventArgs)
    {
        Scene.Dispatch(new RoundCounterIncrementedEvent());
    }
}