using Sandbox.Events;

namespace Mountain;

public record PlayerBeginConnectEvent(Client Client) : IGameEvent;

public record PlayerConnectedEvent(Client Client) : IGameEvent;

public sealed class NetworkManager : SingletonComponent<NetworkManager>, Component.INetworkListener
{
    /// <summary>
    ///     Which player prefab should we spawn?
    /// </summary>
    [Property]
    public GameObject ClientPrefab { get; init; }
    
    /// <summary>
    ///     Called when a network connection becomes active
    /// </summary>
    /// <param name="channel"></param>
    public void OnActive(Connection channel)
    {
        Log.Info($"Player '{channel.DisplayName}' is becoming active");

        var client = GetOrCreateClient(channel);
        if (!client.IsValid())
        {
            throw new($"Something went wrong when trying to create Client for {channel.DisplayName}");
        }

        OnPlayerJoined(client, channel);
    }

    protected override void OnStart()
    {
        if (!Networking.IsActive)
        {
            Networking.CreateLobby(new());
        }
    }

    public void OnPlayerJoined(Client client, Connection channel)
    {
        Scene.Dispatch(new PlayerBeginConnectEvent(client));

        // Either spawn over network, or claim ownership
        if (!client.Network.Active)
        {
            client.GameObject.NetworkSpawn(channel);
        }
        else
        {
            client.Network.AssignOwnership(channel);
        }
        
        client.HostInit();
        client.ClientInit();

        Scene.Dispatch(new PlayerConnectedEvent(client));
    }

    /// <summary>
    ///     Tries to recycle a player state owned by this player (if they disconnected) or makes a new one.
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    private Client? GetOrCreateClient(Connection channel = null)
    {
        var clients = Scene.GetAllComponents<Client>();

        var possibleClient = clients.FirstOrDefault(x => x.Connection is null && x.SteamId == channel.SteamId);

        if (possibleClient.IsValid())
        {
            Log.Warning($"Found existing player state for {channel.SteamId} that we can re-use. {possibleClient}");

            return possibleClient;
        }

        if (!ClientPrefab.IsValid())
        {
            Log.Warning("Could not spawn player as no ClientPrefab assigned.");

            return null;
        }

        var clientObj = ClientPrefab.Clone();
        clientObj.BreakFromPrefab();
        clientObj.Name = $"Client ({channel.DisplayName})";
        clientObj.Network.SetOrphanedMode(NetworkOrphaned.ClearOwner);

        var client = clientObj.GetComponent<Client>();
        client.AssignTeam(Team.Innocent);

        return client.IsValid() ? client : null;
    }
}