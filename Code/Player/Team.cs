using Sandbox.Events;

namespace Mountain;

public interface ITeam : IValid
{
    public Team Team { get; set; }
}

public enum Team
{
    Unassigned = 0,

    A,
    B,
}

public record TeamChangedEvent(Team Before, Team After) : IGameEvent;

public record TeamAssignedEvent(Client Client, Team Team) : IGameEvent;

public static class TeamExtensions
{
    private static readonly Dictionary<Team, Color> teamColors = new()
    {
        { Team.A, new Color32(5, 146, 235) },
        { Team.B, new Color32(233, 190, 92) },
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
            Team.A => Team.B,
            Team.B => Team.A,
            _ => Team.Unassigned,
        };
    }
}