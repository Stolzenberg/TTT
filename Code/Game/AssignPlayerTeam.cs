using System;
using Sandbox.Events;

namespace Mountain;

public sealed class AssignPlayerTeam : Component, IGameEventHandler<EnterStateEvent>
{
    [ConVar( "murder_precentage", ConVarFlags.Replicated )]
    public float MurderPercentage { get; set; } = 0.2f;

    [ConVar( "max_murders", ConVarFlags.Replicated )]
    public int MaxMurders { get; set; } = 3;

    [ConVar( "min_murders", ConVarFlags.Replicated )]
    public int MinMurders { get; set; } = 1;

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        var clients = Game.ActiveScene.AllClients().ToList();
        var clientCount = clients.Count;

        // Calculate number of murders
        var numMurders = (int)(clientCount * MurderPercentage);
        numMurders = Math.Clamp(numMurders, MinMurders, MaxMurders);
        numMurders = Math.Min(numMurders, clientCount);

        // Remaining are innocents
        var numInnocents = clientCount - numMurders;

        // Shuffle players
        var rng = new Random();
        clients = clients.OrderBy(_ => rng.Next()).ToList();

        // Assign roles
        for (var i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            if (i < numMurders)
                client.AssignTeam(Team.Murder);
            else
                client.AssignTeam(Team.Innocent);
        }
    }
}