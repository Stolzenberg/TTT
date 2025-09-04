using System;

namespace Mountain;

public sealed partial class Player
{
    public float BodyHeight => MathF.Abs(capsuleCollider.Start.z - capsuleCollider.End.z);
    public float BodyRadius => capsuleCollider.Radius;

    [Property, Feature("Trace")]
    private readonly CapsuleCollider capsuleCollider = null!;

    public BBox BodyBox(float scale = 1.0f, float heightScale = 1.0f)
    {
        return new(new(-BodyRadius * 0.5f * scale, -BodyRadius * 0.5f * scale, 0),
            new Vector3(BodyRadius * 0.5f * scale, BodyRadius * 0.5f * scale, BodyHeight * heightScale));
    }

    /// <summary>
    ///     Trace the aabb body from one position to another and return the result
    /// </summary>
    public SceneTraceResult TraceBody(Vector3 from, Vector3 to, float scale = 1.0f, float heightScale = 1.0f)
    {
        return Scene.Trace.Box(BodyBox(scale, heightScale), from, to).IgnoreGameObjectHierarchy(GameObject)
            .WithCollisionRules(Tags).Run();
    }
}