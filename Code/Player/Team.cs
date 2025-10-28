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
    Detective,
}

public record TeamChangedEvent(Team Before, Team After) : IGameEvent;

public record TeamAssignedEvent(Client Client, Team Team) : IGameEvent;

public static class TeamExtensions
{
    private static readonly Dictionary<Team, Color> TeamColors = new()
    {
        { Team.Innocent, new(39, 174, 96) },
        { Team.Traitor, new(192, 57, 43) },
        { Team.Detective, new(41, 128, 185) },
        { Team.Unassigned, new(255, 255, 255) },
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
            Team.Detective => Team.Traitor,
            _ => Team.Unassigned,
        };
    }

    /// <summary>
    ///     Check if two teams are allies (should not attack each other).
    /// </summary>
    public static bool AreTeamsAllied(this Team team1, Team team2)
    {
        // Innocents and Detectives are allies
        return team1 is Team.Innocent or Team.Detective && team2 is Team.Innocent or Team.Detective;
    }
}