using System;
using Sandbox.Events;

namespace Mountain;

public sealed class AssignPlayerTeam : Component, IGameEventHandler<EnterStateEvent>
{
    [ConVar( "traitor_precentage", Name = "Traitor Percentage", Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public float TraitorPercentage { get; set; } = 0.2f;

    [ConVar( "max_traitors", Name = "Max Amount of Traitors", Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public int MaxTraitors { get; set; } = 3;

    [ConVar( "min_traitors", Name = "Min Amount of Traitors", Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public int MinTraitors { get; set; } = 1;

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        var clients = Game.ActiveScene.AllClients().ToList();
        var clientCount = clients.Count;

        // Calculate number of traitors
        var numTraitors = (int)(clientCount * TraitorPercentage);
        numTraitors = Math.Clamp(numTraitors, MinTraitors, MaxTraitors);
        numTraitors = Math.Min(numTraitors, clientCount);

        // Remaining are innocents
        var numInnocents = clientCount - numTraitors;

        // Shuffle players
        var rng = new Random();
        clients = clients.OrderBy(_ => rng.Next()).ToList();

        // Assign roles
        for (var i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            if (i < numTraitors)
                client.AssignTeam(Team.Traitor);
            else
                client.AssignTeam(Team.Innocent);
        }
    }
}