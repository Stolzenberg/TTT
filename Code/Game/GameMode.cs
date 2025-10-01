using System;

namespace Mountain;

public sealed class GameMode : SingletonComponent<GameMode>
{
    [Property, Category("Debugging"), FeatureEnabled("Debugging")]
    public bool DebuggingEnabled { get; set; }
    
    public GameStateMachine StateMachine => stateMachine ??= GetComponentInChildren<GameStateMachine>();
    private readonly Dictionary<Type, Component> componentCache = new();

    private GameState? prevState;
    private GameStateMachine? stateMachine;

    /// <summary>
    ///     Gets the given component from within the game mode's object hierarchy, or null if not found / enabled.
    /// </summary>
    public T? Get<T>(bool required = false) where T : class
    {
        if (!StateMachine.IsValid())
        {
            return null;
        }

        if (prevState != StateMachine.CurrentState)
        {
            prevState = StateMachine.CurrentState;
            componentCache.Clear();
        }

        if (!componentCache.TryGetValue(typeof(T), out var component) || component is { IsValid: false } ||
            component is { Active: false })
        {
            component = GetComponentInChildren<T>() as Component;
            if (component is null)
            {
                if (required)
                {
                    throw new($"Expected a {typeof(T).Name} to be active in the {nameof(GameMode)}!");
                }

                return null;
            }
            
            componentCache[typeof(T)] = component;
        }

        if (required && component is not T)
        {
            throw new($"Expected a {typeof(T).Name} to be active in the {nameof(GameMode)}!");
        }

        return component as T;
    }

    protected override void OnStart()
    {
        if (DebuggingEnabled)
        {
            Get<BotManager>()?.AddBot();
        }
    }
    
    protected override void OnUpdate()
    {
        if (DebuggingEnabled)
        {
            DebugOverlay.ScreenText( new( 500, 50 ), $"Rounds: {Get<RoundCounter>()?.Round}");
            DebugOverlay.ScreenText( new( 500, 70 ), $"My Team: {Get<TeamScoring>()?.MyTeamScore}, Opposing Team: {Get<TeamScoring>()?.OpposingTeamScore} ");
        }
    }
}