using Sandbox.Events;

namespace Mountain;

public sealed class RoundWinCondition : Component, IGameEventHandler<EnterStateEvent>,
    IGameEventHandler<UpdateStateEvent>
{
    [Property]
    public GameState InnocentWin { get; set; }
    
    [Property]
    public GameState MurderWin { get; set; }

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
        var players = Game.ActiveScene.AllClients().Where(c => c.Team != Team.Unassigned).ToList();

        // No players or all players are unassigned
        if (players.Count == 0)
        {
            return;
        }

        // Get the set of teams that have at least one alive player
        var teamsWithAlivePlayers = players.Where(IsPlayerAlive).Select(client => client.Team).Distinct().ToList();

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
            else if (winningTeam == Team.Murder)
            {
                GameMode.Instance.StateMachine.Transition(MurderWin);
            }
        }
        // All dead, innocent wins
        else if (teamsWithAlivePlayers.Count == 0)
        {
            GameMode.Instance.StateMachine.Transition(InnocentWin);
        }
    }

    private static bool IsPlayerAlive(Client client)
    {
        var pawn = client.Player;
        if (pawn == null)
        {
            return false;
        }

        return pawn.Components.TryGet<HealthComponent>(out var health) && health.State == LifeState.Alive;
    }
}