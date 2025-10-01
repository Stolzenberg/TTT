namespace Mountain;

public sealed class SpawnFlag : Component
{
    [Property]
    private int SecondsUntilSpawn { get; set; } = 120;
    
    [Property]
    private GameObject FlagPrefab { get; set; } = null!;
    
    
}