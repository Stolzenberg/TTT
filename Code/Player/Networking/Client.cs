using System;
using Sandbox.Events;

namespace Mountain;

public partial class Client : Component, ITeam
{
    /// <summary>
    ///     The team this player is on.
    /// </summary>
    [Property, Group("Setup"), Sync(SyncFlags.FromHost), Change(nameof(OnTeamPropertyChanged))]

    public Team Team { get; set; }
    public static Client Local { get; private set; }

    [Sync(SyncFlags.FromHost), Property]
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

    /// <summary>
    ///     Is this the local player for this client
    /// </summary>
    public bool IsLocalPlayer => !IsProxy && !IsBot && Connection == Connection.Local;

    /// <summary>
    ///     Unique colour or team color of this player
    /// </summary>
    public Color PlayerColor => Team.GetColor();

    [Sync(SyncFlags.FromHost)]
    public Player? Player { get; private set; }

    [Sync]
    public bool IsReady { get; private set; }
    private string Name => IsBot ? $"{BotManager.Instance.GetName(BotId)}" : SteamName;

    /// <summary>
    ///     The player's name, which might have to persist if they leave
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public string SteamName { get; set; }

    public void HostInit()
    {
        if (Connection is null)
        {
            throw new InvalidOperationException("Cannot initialize Client without a valid Connection.");
        }

        SteamId = Connection.SteamId;
        SteamName = Connection.DisplayName;

        ServerRespawn();
    }

    [Rpc.Owner]
    public void ClientInit()
    {
        Local = this;

        IsReady = true;
    }

    public void Kick(string reason = "No reason")
    {
        if (Player.IsValid())
        {
            Player.GameObject.Destroy();
        }

        GameObject.Destroy();

        // Kick the client
        Network.Owner.Kick(reason);
    }

    public void AssignTeam(Team team)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        Team = team;

        Scene.Dispatch(new TeamAssignedEvent(this, team));
    }

    protected override void OnUpdate()
    {
        HandleCleanup();
    }

    /// <summary>
    ///     Called when <see cref="Team" /> changes across the network.
    /// </summary>
    private void OnTeamPropertyChanged(Team before, Team after)
    {
        GameObject.Root.Dispatch(new TeamChangedEvent(before, after));

        if (Player.IsValid())
        {
            Player.GameObject.Root.Dispatch(new TeamChangedEvent(before, after));
        }
    }
}