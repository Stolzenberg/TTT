namespace Mountain;

public sealed partial class Client
{
    public CameraComponent Camera { get; private set; } = null!;

    private void SetupCamera()
    {
        Camera = Scene.Camera;
        Camera.FieldOfView = Preferences.FieldOfView;

        Log.Info($"Setup camera for client {DisplayName}");
    }
}