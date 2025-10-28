using System;

namespace Mountain;

public sealed class WaitingCamera : Component
{
    [Property]
    public float Radius { get; set; } = 300.0f;

    [Property]
    public float TimeModifier { get; set; } = 0.01f;

    protected override void OnUpdate()
    {
        var time = Time.Now * TimeModifier;
        var x = MathF.Cos(time) * Radius;
        var y = MathF.Sin(time) * Radius;
        Client.Local.Camera.WorldPosition = new(x, y, 1000.0f);
        Client.Local.Camera.WorldRotation = Rotation.LookAt(new(-x, -y, -180), Vector3.Up);
    }
}