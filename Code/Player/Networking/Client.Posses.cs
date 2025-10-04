using System;

namespace Mountain;

public partial class Client
{
    [Sync(SyncFlags.FromHost)]
    public Player? Viewer { get; private set; }
    
    public void Possess(Player player)
    {
        if (Viewer == player)
            return;

        Viewer = player;
    }
    
    public void Unpossess()
    {
        Viewer = null;
    }
}