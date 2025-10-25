using System;

namespace Mountain;

/// <summary>
/// A component that measures how illuminated a game object is by using two cameras
/// (one pointing up, one pointing down) that render to textures for brightness analysis.
/// </summary>
public class IlluminationSensor : Component
{
    [Property, Group("Settings"), Description("Width of the render texture used for illumination sampling."),
     Range(16, 256)]
    public int TextureWidth { get; set; } = 64;

    [Property, Group("Settings"), Description("Height of the render texture used for illumination sampling."),
     Range(16, 256)]
    public int TextureHeight { get; set; } = 64;

    [Property, Group("Settings"), Description("Field of view for the illumination cameras."), Range(60f, 120f)]
    public float CameraFov { get; set; } = 90f;

    [Property, Group("Settings"), Description("How often (in seconds) to update illumination calculations."),
     Range(0.016f, 1f)]
    public float UpdateInterval { get; set; } = 0.1f;

    [Property, Group("Settings"),
     Description(
         "Weight of the upward camera in the final calculation (0-1). Downward camera weight is (1 - this value)."),
     Range(0f, 1f)]
    public float UpwardCameraWeight { get; set; } = 0.5f;

    [Property, Group("Debug"), Description("Enable debug visualization and logging.")]
    public bool EnableDebug { get; set; } = false;

    /// <summary>
    /// The current illumination level (0-1, where 0 is completely dark and 1 is fully lit).
    /// </summary>
    public float IlluminationLevel { get; private set; } = 1f;
    private CameraComponent? downwardCamera;
    private GameObject? downwardCameraObject;
    private Texture? downwardRenderTexture;
    private float timeSinceLastUpdate;
    private CameraComponent? upwardCamera;

    private GameObject? upwardCameraObject;
    private Texture? upwardRenderTexture;

    /// <summary>
    /// Manually trigger an illumination update (useful for immediate readings).
    /// </summary>
    public void ForceUpdate()
    {
        UpdateIllumination();
    }

    /// <summary>
    /// Check if the object is considered "well lit" based on a threshold.
    /// </summary>
    public bool IsWellLit(float threshold = 0.2f)
    {
        return IlluminationLevel >= threshold;
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        InitializeCameras();
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();
        if (upwardCamera == null || downwardCamera == null)
        {
            InitializeCameras();
        }
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (upwardCamera == null || downwardCamera == null)
        {
            return;
        }

        timeSinceLastUpdate += Time.Delta;

        if (timeSinceLastUpdate >= UpdateInterval)
        {
            UpdateIllumination();
            timeSinceLastUpdate = 0f;
        }

        if (EnableDebug)
        {
            DrawDebugInfo();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CleanupCameras();
    }

    private void InitializeCameras()
    {
        CleanupCameras();

        // Create upward-facing camera
        upwardCameraObject = new(true, "IlluminationSensorUpCamera");
        upwardCameraObject.SetParent(GameObject);
        upwardCameraObject.LocalPosition = new(0, 0, 16);
        upwardCameraObject.LocalRotation = Rotation.From(new(-90, 0, 0)); // Looking up

        upwardCamera = upwardCameraObject.Components.Create<CameraComponent>();
        upwardCamera.FieldOfView = CameraFov;
        upwardCamera.Priority = -100; // Low priority so it doesn't interfere with main camera
        upwardCamera.Enabled = true;
        upwardCamera.IsMainCamera = false;

        // Create downward-facing camera
        downwardCameraObject = new(true, "IlluminationSensorDownCamera");
        downwardCameraObject.SetParent(GameObject);
        downwardCameraObject.LocalPosition = new(0, 0, 16);
        downwardCameraObject.LocalRotation = Rotation.From(new(90, 0, 0)); // Looking down

        downwardCamera = downwardCameraObject.Components.Create<CameraComponent>();
        downwardCamera.FieldOfView = CameraFov;
        downwardCamera.Priority = -100;
        downwardCamera.Enabled = true;
        downwardCamera.IsMainCamera = false;

        // Create render textures
        CreateRenderTextures();
    }

    private void CreateRenderTextures()
    {
        // Note: In S&box, render textures are typically created through the graphics API
        // This is a simplified version - you may need to adjust based on the actual S&box API

        var textureBuilder =
            Texture.CreateRenderTarget("lightmap", ImageFormat.RGBA8888, new(TextureWidth, TextureHeight));

        upwardRenderTexture = textureBuilder;
        downwardRenderTexture = textureBuilder;

        if (upwardCamera != null)
        {
            upwardCamera.RenderTarget = upwardRenderTexture;
        }

        if (downwardCamera != null)
        {
            downwardCamera.RenderTarget = downwardRenderTexture;
        }
    }

    private void UpdateIllumination()
    {
        if (upwardCamera == null || downwardCamera == null)
        {
            return;
        }

        if (upwardRenderTexture == null || downwardRenderTexture == null)
        {
            return;
        }

        // Note: In S&box, cameras render automatically to their render targets when enabled.
        // The textures are updated continuously as the cameras render each frame.

        // Calculate brightness from both textures
        var upwardBrightness = CalculateTextureBrightness(upwardRenderTexture);
        var downwardBrightness = CalculateTextureBrightness(downwardRenderTexture);

        // Combine the two brightness values with weighting
        IlluminationLevel = upwardBrightness * UpwardCameraWeight + downwardBrightness * (1f - UpwardCameraWeight);

        if (EnableDebug)
        {
            Log.Info(
                $"IlluminationSensor: Up: {upwardBrightness:F2}, Down: {downwardBrightness:F2}, Combined: {IlluminationLevel:F2}");
        }
    }

    private float CalculateTextureBrightness(Texture texture)
    {
        try
        {
            var pixels = texture.GetPixels();
            if (pixels == null || pixels.Length == 0)
            {
                return 1f;
            }

            var totalBrightness = 0f;
            var pixelCount = 0;

            foreach (var pixel in pixels)
            {
                // Normalize pixel values to 0-1 range (in case they're 0-255)
                var r = pixel.r > 1f ? pixel.r / 255f : pixel.r;
                var g = pixel.g > 1f ? pixel.g / 255f : pixel.g;
                var b = pixel.b > 1f ? pixel.b / 255f : pixel.b;

                // Calculate luminance using standard formula (Rec. 709) in linear space
                var luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;

                totalBrightness += luminance;
                pixelCount++;
            }

            if (pixelCount == 0)
            {
                return 1f;
            }

            var averageBrightness = totalBrightness / pixelCount;

            if (EnableDebug)
            {
                Log.Info(
                    $"IlluminationSensor: Pixel count: {pixelCount}, Total brightness: {totalBrightness:F2}, Average: {averageBrightness:F2}");
            }

            return averageBrightness;
        }
        catch (Exception ex)
        {
            Log.Warning($"IlluminationSensor: Error calculating brightness: {ex.Message}");

            return 1f;
        }
    }

    private void DrawDebugInfo()
    {
        if (!Scene.IsValid())
        {
            return;
        }

        var pos = WorldPosition;

        // Draw upward ray
        DebugOverlay.Line(pos, pos + WorldRotation.Up * 100f, Color.Yellow);
        DebugOverlay.Text(pos + WorldRotation.Up * 110f, $"Illumination: {IlluminationLevel:F2}", 0f, TextFlag.Center,
            Color.White);

        // Draw downward ray
        DebugOverlay.Line(pos, pos + WorldRotation.Down * 100f, Color.Cyan);

        // Draw illumination sphere (size based on illumination level)
        DebugOverlay.Sphere(new(pos, 20f * IlluminationLevel), Color.White.WithAlpha(IlluminationLevel));
    }

    private void CleanupCameras()
    {
        if (upwardCameraObject?.IsValid() == true)
        {
            upwardCameraObject.Destroy();
        }

        if (downwardCameraObject?.IsValid() == true)
        {
            downwardCameraObject.Destroy();
        }

        upwardCameraObject = null;
        downwardCameraObject = null;
        upwardCamera = null;
        downwardCamera = null;
        upwardRenderTexture = null;
        downwardRenderTexture = null;
    }
}