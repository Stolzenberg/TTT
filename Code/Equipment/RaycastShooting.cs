namespace Mountain;

/// <summary>
/// Implements hitscan/raycast shooting behavior.
/// Instantaneous bullet hits with penetration support.
/// </summary>
public sealed class RaycastShooting : ShootingBehavior
{
    [Property, Feature("Bullet")]
    public float BaseDamage { get; init; } = 25.0f;

    [Property, Feature("Bullet")]
    public float BulletSize { get; init; } = 1.0f;

    [Property, Feature("Bullet")]
    public int BulletCount { get; init; } = 1;

    [Property, Feature("Bullet")]
    public Curve BaseDamageFalloff { get; init; } = new(new List<Curve.Frame> { new(0, 1), new(1, 0) });

    [Property, Feature("Bullet")]
    public float MaxRange { get; init; } = 1024000;

    [Property, Feature("Bullet")]
    public float BulletSpread { get; init; } = 0;

    [Property, Feature("Bullet")]
    public float Force { get; init; } = 1000f;

    [Property, Feature("Bullet")]
    public float PenetrationThickness { get; init; } = 32f;

    protected override void PerformShoot()
    {
        for (var i = 0; i < BulletCount; i++)
        {
            ServerShoot(Ray.Position, Ray.Forward);
        }
    }

    [Rpc.Host]
    private void ServerShoot(Vector3 position, Vector3 forward)
    {
        foreach (var tr in GetShootTrace(position, forward))
        {
            if (!tr.Hit || tr.Distance == 0)
            {
                continue;
            }

            BroadcastImpactEffects(tr.GameObject, tr.Surface, tr.EndPosition, tr.Normal);

            var damage = CalculateDamageFalloff(BaseDamage, tr.Distance);
            damage = damage.CeilToInt();

            if (!tr.GameObject.IsValid())
            {
                continue;
            }

            var health = tr.GameObject.Root.GetComponentInChildren<Health>();
            if (!health.IsValid())
            {
                continue;
            }

            tr.GameObject.ServerTakeDamage(new()
            {
                Attacker = Equipment.Owner,
                Victim = health,
                Inflictor = Equipment,
                Position = tr.EndPosition,
                Damage = damage,
                Force = tr.Direction * Force,
                Hitbox = tr.GetHitboxTags(),
            });
        }
    }

    private float CalculateDamageFalloff(float damage, float distance)
    {
        var distDelta = distance / MaxRange;
        var damageMultiplier = BaseDamageFalloff.Evaluate(distDelta);

        return damage * damageMultiplier;
    }

    [Rpc.Broadcast]
    private void BroadcastImpactEffects(GameObject target, Surface surface, Vector3 pos, Vector3 normal)
    {
        var impact = surface.PrefabCollection.BulletImpact.Clone();
        impact.WorldPosition = pos + normal;
        impact.WorldRotation = Rotation.LookAt(-normal);
        impact.SetParent(target);

        Sound.Play(surface.SoundCollection.Bullet, pos);
    }

    private IEnumerable<SceneTraceResult> GetShootTrace(Vector3 position, Vector3 direction)
    {
        var hits = new List<SceneTraceResult>();
        var rot = Rotation.LookAt(direction);
        var forward = rot.Forward;

        // Apply spread
        forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * BulletSpread * 0.25f;
        forward = forward.Normal;

        var original = DoTraceBullet(position, position + forward * MaxRange, BulletSize);
        var sceneTraceResults = original.ToList();

        foreach (var traceResult in sceneTraceResults)
        {
            Log.Info($"tr.Hitbox: {traceResult.Hitbox}");
        }

        if (sceneTraceResults.Count == 0)
        {
            return sceneTraceResults;
        }

        // Fix start positions for traces by using the last end position as the start
        var startPos = sceneTraceResults.ElementAt(0).StartPosition;
        List<SceneTraceResult> fixedPath = new();
        for (var i = 0; i < sceneTraceResults.Count; i++)
        {
            var el = sceneTraceResults.ElementAt(i);
            fixedPath.Add(el with { StartPosition = startPos });
            startPos = el.EndPosition;
        }

        var entries = new List<(SceneTraceResult Trace, float Thickness)>();

        // Trace backwards to get exit points and thickness
        for (var i = fixedPath.Count - 1; i >= 0; i--)
        {
            var el = fixedPath.ElementAt(i);
            var backTrace = DoTraceBulletOne(el.EndPosition, el.StartPosition, BulletSize);
            var impact = backTrace.EndPosition;
            var thickness = (el.StartPosition - impact).Length;

            el = el with { StartPosition = impact };
            entries.Insert(0, (el, thickness));
        }

        // Calculate penetration
        float accThickness = 0;
        foreach (var el in entries)
        {
            accThickness += el.Thickness;
            if (accThickness >= PenetrationThickness)
            {
                break;
            }

            hits.Add(el.Trace);
        }

        return hits;
    }

    private IEnumerable<SceneTraceResult> DoTraceBullet(Vector3 start, Vector3 end, float radius)
    {
        return TraceBullet(start, end, radius).RunAll();
    }

    private SceneTraceResult DoTraceBulletOne(Vector3 start, Vector3 end, float radius)
    {
        return TraceBullet(start, end, radius).Run();
    }

    private SceneTrace TraceBullet(Vector3 start, Vector3 end, float radius)
    {
        return Scene.Trace.Ray(start, end).UseHitboxes().IgnoreGameObjectHierarchy(GameObject.Root)
            .WithoutTags("trigger").Size(radius);
    }
}