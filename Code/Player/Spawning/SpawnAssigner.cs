﻿namespace Mountain;

public record struct SpawnPointInfo(Transform Transform, IReadOnlyList<string> Tags)
{
    public Vector3 Position => Transform.Position;
    public Rotation Rotation => Transform.Rotation;
}
