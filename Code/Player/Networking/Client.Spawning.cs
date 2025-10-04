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

    public void ServerRespawn(bool forceNew = true)
    {
        var spawnPoint = GameMode.Instance.Get<TeamSpawnAssigner>().GetSpawnPoint(this);
        Log.Info(
            $"Spawning player.. ( {GameObject.Name} ({DisplayName}, {Team}), {spawnPoint.WorldPosition}, [{string.Join(", ", spawnPoint.Tags)}] )");

        if (forceNew || !Player.IsValid() || Player.Health.State == LifeState.Dead)
        {
            Player?.GameObject?.Destroy();
            Player = null;

            ServerSpawn(spawnPoint);
        }
        else
        {
            Player.SetSpawnPoint(spawnPoint);
            Player.ServerRespawn();
        }
    }

    protected void OnRespawnStateChanged(LifeState oldValue, LifeState newValue)
    {
        TimeSinceRespawnStateChanged = 0f;
    }

    private void ServerSpawn(TeamSpawnPoint spawnPoint)
    {
        var gameObject = PlayerPrefab.Clone(spawnPoint.WorldTransform);
        gameObject.Name = $"Player ({DisplayName})";
       
        var player = gameObject.GetComponent<Player>();
        Player = player;

        Player.NameTag.Name = DisplayName;
        Player.Client = this;
        
        Player.SetSpawnPoint(spawnPoint);
        gameObject.NetworkSpawn(Network.Owner);
        RespawnState = RespawnState.Not;
        Player.ServerRespawn();

        Player.Possess(player);
    }
}