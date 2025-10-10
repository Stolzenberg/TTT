using Sandbox.Events;

namespace Mountain;

public record TeamScoreIncrementedEvent(Team Team, int Score) : IGameEvent;

public sealed class TeamScoring : Component
{
    [Sync(SyncFlags.FromHost)]
    public NetDictionary<Team, int> Scores { get; } = new();

    public int MyTeamScore => Scores.GetValueOrDefault(Client.Local.Team);
    public int OpposingTeamScore => Scores.GetValueOrDefault(Client.Local.Team.GetOpponents());

    public Team GetHighest()
    {
        var tScore = Scores.GetValueOrDefault(Team.Innocent);
        var ctScore = Scores.GetValueOrDefault(Team.Traitor);

        if (tScore > ctScore)
        {
            return Team.Innocent;
        }

        if (ctScore > tScore)
        {
            return Team.Traitor;
        }

        return Team.Unassigned;
    }

    public void IncrementScore(Team team, int amount = 1)
    {
        var score = Scores.GetValueOrDefault(team) + amount;

        Scores[team] = score;

        Scene.Dispatch(new TeamScoreIncrementedEvent(team, score));
    }
}