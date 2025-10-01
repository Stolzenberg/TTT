using Sandbox.Events;

namespace Mountain;

public interface ITeam : IValid
{
    public Team Team { get; set; }
}

public enum Team
{
    Unassigned = 0,

    Innocent,
    Murder,
}

public record TeamChangedEvent(Team Before, Team After) : IGameEvent;

public record TeamAssignedEvent(Client Client, Team Team) : IGameEvent;

public static class TeamExtensions
{
    private static readonly Dictionary<Team, Color> teamColors = new()
    {
        { Team.Innocent, new Color32(5, 146, 235) },
        { Team.Murder, new Color32(233, 190, 92) },
        { Team.Unassigned, new Color32(255, 255, 255) },
    };

    public static Color GetColor(this Team team)
    {
        return teamColors[team];
    }

    public static Team GetOpponents(this Team team)
    {
        return team switch
        {
            Team.Innocent => Team.Murder,
            Team.Murder => Team.Innocent,
            _ => Team.Unassigned,
        };
    }
}