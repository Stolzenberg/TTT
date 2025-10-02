using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Called on the server when a player (re)spawns.
/// </summary>
public record PlayerSpawnedEvent(Player Player) : IGameEvent;

public sealed partial class Player
{
    /// <summary>
    ///     The position this player last spawned at.
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public Vector3 SpawnPosition { get; set; }

    /// <summary>
    ///     The rotation this player last spawned at.
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public Rotation SpawnRotation { get; set; }

    /// <summary>
    ///     The tags of the last spawn point of this pawn.
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public NetList<string> SpawnPointTags { get; } = new();

    [Sync(SyncFlags.FromHost)]
    public TimeSince TimeSinceLastRespawn { get; private set; }

    public void ServerRespawn()
    {
        TimeSinceLastRespawn = 0f;
        
        OwnerTeleport(SpawnPosition, SpawnRotation);

        foreach (var equipment in DefaultEquipments)
        {
            ServerGive(equipment);
        }
        
        Scene.Dispatch(new PlayerSpawnedEvent(this));
    }

    public void SetSpawnPoint(TeamSpawnPoint spawnPoint)
    {
        SpawnPosition = spawnPoint.WorldPosition;
        SpawnRotation = spawnPoint.WorldRotation;

        SpawnPointTags.Clear();

        foreach (var tag in spawnPoint.Tags)
        {
            SpawnPointTags.Add(tag);
        }
    }
    
    [Rpc.Owner]
    public void OwnerTeleport( Vector3 position, Rotation rotation )
    {
        Transform.World = new( position, rotation );
        Transform.ClearInterpolation();
        EyeAngles = rotation.Angles();
        WishVelocity = Vector3.Zero;
    }
}