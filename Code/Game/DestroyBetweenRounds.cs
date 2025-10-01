using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Called on the server to clean up objects that shouldn't persist between rounds.
/// </summary>
public record BetweenRoundCleanupEvent : IGameEvent;

/// <summary>
///     Destroy this object when a <see cref="BetweenRoundCleanupEvent" /> is dispatched.
/// </summary>
public sealed class DestroyBetweenRounds : Component, IGameEventHandler<BetweenRoundCleanupEvent>
{
    void IGameEventHandler<BetweenRoundCleanupEvent>.OnGameEvent(BetweenRoundCleanupEvent eventArgs)
    {
        GameObject.Destroy();
    }
}