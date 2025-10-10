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
    Traitor,
}

public record TeamChangedEvent(Team Before, Team After) : IGameEvent;

public record TeamAssignedEvent(Client Client, Team Team) : IGameEvent;

public static class TeamExtensions
{
    private static readonly Dictionary<Team, Color> TeamColors = new()
    {
        { Team.Innocent, new Color32(39, 174, 96) },
        { Team.Traitor, new Color32(192, 57, 43) },
        { Team.Unassigned, new Color32(255, 255, 255) },
    };

    public static Color GetColor(this Team team)
    {
        return TeamColors[team];
    }

    public static Team GetOpponents(this Team team)
    {
        return team switch
        {
            Team.Innocent => Team.Traitor,
            Team.Traitor => Team.Innocent,
            _ => Team.Unassigned,
        };
    }
}