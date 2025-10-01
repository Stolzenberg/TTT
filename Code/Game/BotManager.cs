namespace Mountain;

/// <summary>
///     A singleton component which handles the bots in a game. Gives them funny names.
/// </summary>
public sealed class BotManager : SingletonComponent<BotManager>
{
    private static readonly string[] BotNames =
    [
        "Gordon",
        "Jamie",
        "Nigella",
        "Heston",
        "Anthony",
        "Ainsley",
        "Delia",
        "Loyd",
        "Paul",
        "Marco",
    ];

    [Sync(SyncFlags.FromHost)]
    private int CurrentBotId { get; set; }
    public string[] Names;

    public void AddBot()
    {
        var clientObj = NetworkManager.Instance.ClientPrefab.Clone();
        clientObj.Name = $"Client ({GetName(CurrentBotId)})";

        var client = clientObj.GetComponent<Client>();
        client.BotId = CurrentBotId;
        client.AssignTeam(Team.Murder);
        
        NetworkManager.Instance.OnPlayerJoined(client, Connection.Host);

        CurrentBotId++;
    }

    public string GetName(int id)
    {
        return Names[id % Names.Length];
    }

    protected override void OnAwake()
    {
        base.OnAwake();

        Names = BotNames.Shuffle().ToArray();
    }

    [ConCmd("addbot", ConVarFlags.Server)]
    private static void Command_Add_Bot()
    {
        Instance.AddBot();
    }

    [ConCmd("kickbots", ConVarFlags.Server)]
    private static void Command_Kick_Bots()
    {
        foreach (var client in Game.ActiveScene.AllClients())
        {
            if (client.IsBot)
            {
                client.Kick();
            }
        }
    }
}