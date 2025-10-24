using System;
using Sandbox.Events;

namespace Mountain;

public sealed class AssignPlayerTeam : Component, IGameEventHandler<EnterStateEvent>
{
    [ConVar("traitor_precentage", ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public static float TraitorPercentage { get; set; } = 0.2f;

    [ConVar("max_traitors", ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public static int MaxTraitors { get; set; } = 3;

    [ConVar("min_traitors", ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public static int MinTraitors { get; set; } = 1;

    [ConVar("detective_percentage", ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public static float DetectivePercentage { get; set; } = 0.2f;

    [ConVar("max_detectives", ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public static int MaxDetectives { get; set; } = 2;

    [ConVar("min_detectives", ConVarFlags.GameSetting | ConVarFlags.Replicated)]
    public static int MinDetectives { get; set; } = 0;

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        var clients = Game.ActiveScene.AllClients().ToList();
        var clientCount = clients.Count;

        // Calculate number of traitors
        var numTraitors = (int)(clientCount * TraitorPercentage);
        numTraitors = Math.Clamp(numTraitors, MinTraitors, MaxTraitors);
        numTraitors = Math.Min(numTraitors, clientCount);

        // Calculate number of detectives
        var numDetectives = (int)(clientCount * DetectivePercentage);
        numDetectives = Math.Clamp(numDetectives, MinDetectives, MaxDetectives);
        numDetectives = Math.Min(numDetectives, clientCount - numTraitors);

        // Shuffle players
        var rng = new Random();
        clients = clients.OrderBy(_ => rng.Next()).ToList();

        // Assign roles
        for (var i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            if (i < numTraitors)
                client.AssignTeam(Team.Traitor);
            else if (i < numTraitors + numDetectives)
                client.AssignTeam(Team.Detective);
            else
                client.AssignTeam(Team.Innocent);
        }
    }
}