using Sandbox.Events;

namespace Mountain;

public sealed class RoundWinCondition : Component, IGameEventHandler<EnterStateEvent>,
    IGameEventHandler<UpdateStateEvent>
{
    [Property]
    public GameState InnocentWin { get; set; }
    
    [Property]
    public GameState TraitorWin { get; set; }

    void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
    {
        CheckCondition();
    }

    void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
    {
        CheckCondition();
    }

    private void CheckCondition()
    {
        var clients = Game.ActiveScene.AllClients().Where(c => c.Team != Team.Unassigned).ToList();

        // No players or all players are unassigned
        if (clients.Count == 0)
        {
            return;
        }

        // Get alive players and normalize their teams (Detective counts as Innocent)
        var aliveClients = clients.Where(IsPlayerAlive).ToList();
        var teamsWithAlivePlayers = aliveClients
            .Select(client => NormalizeTeam(client.Team))
            .Distinct()
            .ToList();

        // If there's only one team left with alive players, that team wins the round
        if (teamsWithAlivePlayers.Count == 1)
        {
            // Transition to prepare state for next round
            var winningTeam = teamsWithAlivePlayers.First();

            // Increment score for the winning team
            GameMode.Instance.Get<TeamScoring>().IncrementScore(winningTeam);

            if (winningTeam == Team.Innocent)
            {
                GameMode.Instance.StateMachine.Transition(InnocentWin);
            }
            else if (winningTeam == Team.Traitor)
            {
                GameMode.Instance.StateMachine.Transition(TraitorWin);
            }
        }
        // All dead, innocent wins
        else if (teamsWithAlivePlayers.Count == 0)
        {
            GameMode.Instance.StateMachine.Transition(InnocentWin);
        }
    }

    /// <summary>
    /// Normalize team for win condition - Detective counts as Innocent
    /// </summary>
    private static Team NormalizeTeam(Team team)
    {
        return team == Team.Detective ? Team.Innocent : team;
    }

    private static bool IsPlayerAlive(Client client)
    {
        var player = client.Player;
        if (!player.IsValid())
        {
            return false;
        }

        return player.Health.State == LifeState.Alive;
    }
}