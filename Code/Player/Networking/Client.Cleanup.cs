namespace Mountain;

public partial class Client : Component.INetworkListener
{
    [Property]
    private readonly float disconnectCleanupTime = 120f;
    
    private RealTimeSince timeSinceDisconnected;

    void INetworkListener.OnDisconnected(Connection channel)
    {
        if (Connection == channel)
        {
            timeSinceDisconnected = 0;
        }
    }

    private void HandleCleanup()
    {
        if (IsConnected)
        {
            return;
        }

        if (!IsLocalPlayer)
        {
            return;
        }

        if (timeSinceDisconnected > disconnectCleanupTime)
        {
            GameObject.Destroy();
        }
    }
}