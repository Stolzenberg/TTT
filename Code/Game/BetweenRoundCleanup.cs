﻿using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Called on the server to clean up objects that shouldn't persist between rounds.
/// </summary>
public record BetweenRoundCleanupEvent : IGameEvent;

/// <summary>
///     Dispatches a <see cref="BetweenRoundCleanupEvent" /> when entering this state.
/// </summary>
public sealed class BetweenRoundCleanup : Component, IGameEventHandler<EnterStateEvent>
{
    [Early]
    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        Dispatch();
    }

    [Rpc.Broadcast(NetFlags.HostOnly)]
    public void Dispatch()
    {
        Scene.Dispatch(new BetweenRoundCleanupEvent());
    }
}