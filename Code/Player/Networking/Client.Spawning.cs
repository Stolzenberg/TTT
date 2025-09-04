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
    public DamageInfo LastDamageInfo { get; private set; }

    /// <summary>
    ///     Are we ready to respawn?
    /// </summary>
    [Sync(SyncFlags.FromHost), Change(nameof(OnRespawnStateChanged))]
    public RespawnState RespawnState { get; set; }

    public bool IsRespawning => RespawnState is RespawnState.Delayed;

    public void ServerRespawn(bool forceNew = true)
    {
        var spawnPoint = Game.ActiveScene.Get<TeamSpawnAssigner>() is { } spawnAssigner
            ? spawnAssigner.GetSpawnPoint(this)
            : Game.ActiveScene.GetRandomSpawnPoint(Team);

        Log.Info(
            $"Spawning player.. ( {GameObject.Name} ({DisplayName}, {Team}), {spawnPoint.Position}, [{string.Join(", ", spawnPoint.Tags)}] )");

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

    public void OnKill(DamageInfo damageInfo)
    {
        LastDamageInfo = damageInfo;
    }

    protected void OnRespawnStateChanged(LifeState oldValue, LifeState newValue)
    {
        TimeSinceRespawnStateChanged = 0f;
    }

    private void ServerSpawn(SpawnPointInfo spawnPoint)
    {
        var prefab = PlayerPrefab.Clone(spawnPoint.Transform);
        prefab.Name = $"Player ({DisplayName})";
       
        var player = prefab.GetComponent<Player>();

        player.NameTag.Name = DisplayName;
        player.WorldRotation = spawnPoint.Rotation;
        player.Client = this;
        
        player.SetSpawnPoint(spawnPoint);
        prefab.NetworkSpawn(Network.Owner);

        Player = player;

        RespawnState = RespawnState.Not;
        Player.ServerRespawn();
    }
}