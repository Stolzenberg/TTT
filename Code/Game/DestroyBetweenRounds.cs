using Sandbox.Events;

namespace Mountain;

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