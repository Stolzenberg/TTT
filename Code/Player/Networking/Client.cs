using System;

namespace Mountain;

public partial class Client : Component
{
    public static Client Local { get; private set; }

    [Sync(SyncFlags.FromHost)]
    public ulong SteamId { get; set; }

    public Connection? Connection => Network.Owner;

    /// <summary>
    ///     Is this player connected? Clients can linger around in competitive matches to keep a player's slot in a team if
    ///     they disconnect.
    /// </summary>
    public bool IsConnected => Connection is not null && (Connection.IsActive || Connection.IsHost);

    /// <summary>
    ///     Name of this player
    /// </summary>
    public string DisplayName => $"{Name}{(!IsConnected ? " (Disconnected)" : "")}";

    public bool IsLocalClient => !IsProxy && !IsBot && Connection == Connection.Local;

    /// <summary>
    /// The main PlayerPawn of this player if one exists, will not change when the player possesses gadgets etc. (synced)
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public Player? Player { get; private set; }

    [Sync]
    public bool IsReady { get; private set; }

    /// <summary>
    ///     The player's name, which might have to persist if they leave
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public string SteamName { get; set; }
    private string Name => IsBot ? $"{BotManager.Instance.GetName(BotId)}" : SteamName;

    public void HostInit()
    {
        if (Connection is null)
        {
            throw new InvalidOperationException("Cannot initialize Client without a valid Connection.");
        }

        SteamId = Connection.SteamId;
        SteamName = Connection.DisplayName;

        InitializeKarma();
    }

    [Rpc.Owner]
    public void ClientInit()
    {
        if (!IsLocalClient)
        {
            return;
        }

        Local = this;
        IsReady = true;

        SetupCamera();
    }

    public void ServerKick(string reason = "No reason")
    {
        if (Player.IsValid())
        {
            Player.GameObject.Destroy();
        }

        GameObject.Destroy();

        // Kick the client
        Network.Owner.Kick(reason);
    }

    protected override void OnUpdate()
    {
        HandleCleanup();
        UpdateSpectator();
    }
}