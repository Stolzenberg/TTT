using System;
using Sandbox.Events;

namespace Mountain;

public partial class Client : ITeam
{
    /// <summary>
    ///     The team this player is on.
    /// </summary>
    [Property, Group("Setup"), Sync(SyncFlags.FromHost), Change(nameof(OnTeamPropertyChanged))]

    public Team Team { get; set; }
    public Color TeamColor => Team.GetColor();

    public void AssignTeam(Team team)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        Team = team;

        BroadcastTeam(team);
    }

    [Rpc.Broadcast]
    private void BroadcastTeam(Team team)
    {
        Scene.Dispatch(new TeamAssignedEvent(this, team));
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