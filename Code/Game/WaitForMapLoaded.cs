using Sandbox.Events;

namespace Mountain;

public sealed class WaitForMapLoaded : Component, IGameEventHandler<EnterStateEvent>, IGameEventHandler<LeaveStateEvent>
{
    [Property]
    public GameState State { get; private set; }
    
    private MapInstance mapInstance;

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        mapInstance = GameMode.Instance.Get<MapInstance>();
        mapInstance.OnMapLoaded += OnMapLoaded;
        
        if (mapInstance.IsLoaded)
        {
            Log.Info("Map already loaded, proceeding to next state.");
            GameMode.Instance.StateMachine.Transition(State, State.DefaultDuration);
        }
    }

    void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
    {
        mapInstance.OnMapLoaded -= OnMapLoaded;
    }

    private void OnMapLoaded()
    {
        Log.Info($"Map loaded, proceeding to next {State}.");
        GameMode.Instance.StateMachine.Transition(State.DefaultNextState!, State.DefaultDuration);
    }
}