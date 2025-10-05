using System;

namespace Mountain;

public enum RespawnState
{
    Not,
    Requested,
    Delayed,
    Immediate,
}

public partial class Client
{
	/// <summary>
	///     The prefab to spawn when we want to make a player pawn for the player.
	/// </summary>
	[Property]
    public GameObject PlayerPrefab { get; set; }

    public TimeSince TimeSinceRespawnStateChanged { get; private set; }

    /// <summary>
    ///     Are we ready to respawn?
    /// </summary>
    [Sync(SyncFlags.FromHost), Change(nameof(OnRespawnStateChanged))]
    public RespawnState RespawnState { get; set; }

    public bool IsRespawning => RespawnState is RespawnState.Delayed;

    public void Respawn(bool forceNew = true)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Respawn can only be called on the host.");
        }
        
        var spawnPoint = GameMode.Instance.Get<TeamSpawnAssigner>().GetSpawnPoint(this);
        Log.Info(
            $"Spawning ({DisplayName}, {Team}), {spawnPoint.WorldPosition}, [{string.Join(", ", spawnPoint.Tags)}]");

        if (forceNew || !Player.IsValid() || Player.Health.State == LifeState.Dead)
        {
            Player?.GameObject?.Destroy();
            Player = null;

            Spawn(spawnPoint);
        }
        else
        {
            Player.SetSpawnPoint(spawnPoint);
            Player.Respawn();
        }
    }

    protected void OnRespawnStateChanged(LifeState oldValue, LifeState newValue)
    {
        TimeSinceRespawnStateChanged = 0f;
    }

    private void Spawn(TeamSpawnPoint spawnPoint)
    {
        var gameObject = PlayerPrefab.Clone(spawnPoint.WorldTransform);
        gameObject.Name = $"Player ({DisplayName})";
       
        var player = gameObject.GetComponent<Player>();
        Player = player;
        Player.Client = this;
        
        Player.SetSpawnPoint(spawnPoint);
        gameObject.NetworkSpawn(Network.Owner);
        RespawnState = RespawnState.Not;
        Player.Respawn();
    }
}