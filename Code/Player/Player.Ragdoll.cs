using Sandbox.Events;

namespace Mountain;

public record OnPlayerRagdolledEvent : IGameEvent;

public sealed partial class Player
{
    [Property, Feature("Ragdoll")]
    public ModelPhysics Physics { get; set; }

    public Vector3 DamageTakenPosition { get; private set; }
    public Vector3 DamageTakenForce { get; private set; }

    [Rpc.Broadcast(NetFlags.HostOnly)]
    private void CreateRagdoll(bool ragdoll)
    {
        if (!Physics.IsValid())
        {
            return;
        }

        Physics.Enabled = ragdoll;

        var ev = new OnPlayerRagdolledEvent();
        Scene.Dispatch(ev);

        BodyRenderer.UseAnimGraph = !ragdoll;
        Collider.IsTrigger = ragdoll;

        GameObject.Tags.Set("ragdoll", ragdoll);

        if (!ragdoll)
        {
            GameObject.LocalPosition = Vector3.Zero;
            GameObject.LocalRotation = Rotation.Identity;
        }

        if (ragdoll && DamageTakenForce.LengthSquared > 0f)
        {
            ApplyRagdollImpulses(DamageTakenPosition, DamageTakenForce);
        }

        Transform.ClearInterpolation();
    }

    private void ApplyRagdollImpulses(Vector3 position, Vector3 force)
    {
        if (!Physics.IsValid() || !Physics.PhysicsGroup.IsValid())
        {
            return;
        }

        foreach (var physicsBody in Physics.PhysicsGroup.Bodies)
        {
            physicsBody.ApplyImpulseAt(position, force);
        }
    }
}