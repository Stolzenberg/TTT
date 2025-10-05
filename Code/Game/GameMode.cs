using System;

namespace Mountain;

public sealed class GameMode : SingletonComponent<GameMode>
{
    [Property, Feature("Debugging"), FeatureEnabled("Debugging")]
    public bool DebuggingEnabled { get; set; }
    
    [Property, Feature("Debugging"), Description("How many bots to add when debugging is enabled")]
    public int BotCount { get; set; } = 0;
    
    public GameStateMachine StateMachine => stateMachine ??= GetComponentInChildren<GameStateMachine>();
    private readonly Dictionary<Type, Component> componentCache = new();

    private GameState? prevState;
    private GameStateMachine? stateMachine;

    /// <summary>
    ///     Gets the given component from within the game mode's object hierarchy, or null if not found / enabled.
    /// </summary>
    public T Get<T>() where T : class
    {
        if (!StateMachine.IsValid())
        {
            throw new InvalidOperationException("GameMode's StateMachine is not valid!");
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
            componentCache[typeof(T)] = component ?? throw new($"No {typeof(T).Name} found in the {nameof(GameMode)}.");
        }

        if (component is not T value)
        {
            throw new($"Expected a {typeof(T).Name} to be active in the {nameof(GameMode)}!");
        }

        return value;
    }

    protected override void OnStart()
    {
        if (DebuggingEnabled)
        {
            for (var i = 0; i < BotCount; i++)
            {
                Get<BotManager>().AddBot();
            }
        }
    }
}