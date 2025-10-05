using System;

namespace Mountain;

public partial class Client
{
    /// <summary>
    /// The client we're currently in the view of (clientside).
    /// Usually the local client, apart from when spectating etc.
    /// </summary>
    public static Client? Viewer { get; private set; }

    /// <summary>
    /// Are we in the view of this client (clientside)
    /// </summary>
    public bool IsViewer => Viewer == this;
    
    public static void Possess(Player player)
    {
        if (!player.IsValid())
        {
            throw new InvalidOperationException("Cannot possess a null or invalid player.");
        }

        if (!Local.IsValid())
        {
            throw new InvalidOperationException("Cannot possess a player when there is no local client.");
        }

        Viewer = player.Client;
    }

    public static void Unpossess()
    {
        Viewer = null;
    }
}