using System;

namespace Mountain;

public static class SceneExtensions
{
    public static IEnumerable<Client> AllClients(this Scene scene)
    {
        return scene.GetAllComponents<Client>();
    }

    public static IEnumerable<Player> AllPlayers(this Scene scene)
    {
        return scene.GetAllComponents<Player>();
    }

    public static Player GetPlayerById(this Scene scene, Guid id)
    {
        return AllPlayers(scene).First(x => x.Id == id);
    }

    public static List<Client> GetClientsByTeam(this Scene scene, Team team)
    {
        return AllClients(scene).Where(x => x.Team == team).ToList();
    }

    public static bool OnlyOneTeamLeft(this Scene scene)
    {
        var teams = AllClients(scene).Select(x => x.Team).Distinct().ToList();

        return teams.Count == 1;
    }

    public static bool AllClientsReady(this Scene scene)
    {
        return AllClients(scene).All(x => x.IsReady);
    }

    public static TeamSpawnPoint GetRandomSpawnPoint(this Scene scene, Team team, params string[] tags)
    {
        return Random.Shared.FromArray(GetSpawnPoints(scene, team, tags).ToArray());
    }

    /// <summary>
    ///     Get all spawn point transforms for the given team.
    /// </summary>
    public static IEnumerable<TeamSpawnPoint> GetSpawnPoints(this Scene scene, Team team, params string[] tags)
    {
        return scene.GetAllComponents<TeamSpawnPoint>().Where(x => x.Team == team)
            .Where(x => tags.Length == 0 || tags.Any(x.Tags.Contains));
    }
}