using System;

namespace Mountain;

public sealed class Ragdoll : Component
{
    public static Ragdoll Create(Player player, DamageInfo damageInfo)
    {
        if (!Networking.IsHost)
        {
            throw new InvalidOperationException("Creating a ragdoll can only be created on the host.");
        }

        var gameObject = new GameObject
        {
            WorldPosition = player.WorldPosition,
            WorldRotation = player.WorldRotation,
            Name = player.GameObject.Name + " Ragdoll",
            Tags = { "ragdoll" },
            NetworkMode = NetworkMode.Object,
        };
        
        var ragdoll = gameObject.AddComponent<Ragdoll>();
        
        ragdoll.DamageInfo = damageInfo;
        
        gameObject.AddComponent<OpenDeathDialog>();
        gameObject.AddComponent<DestroyBetweenRounds>();
        
        var renderer = gameObject.AddComponent<SkinnedModelRenderer>();
        renderer.Model = player.BodyRenderer.Model;
        renderer.ClearParameters();
        
        ragdoll.ModelPhysics = gameObject.AddComponent<ModelPhysics>();
        ragdoll.ModelPhysics.Model = renderer.Model;
        ragdoll.ModelPhysics.Renderer = renderer;
        
        ragdoll.Dresser = gameObject.AddComponent<Dresser>();
        ragdoll.Dresser.Source = Dresser.ClothingSource.OwnerConnection;
        ragdoll.Dresser.BodyTarget = renderer;
        
        gameObject.NetworkSpawn(player.Network.Owner);
        ragdoll.Dresser.Apply();

        return ragdoll;
    }

    public Dresser Dresser { get; set; }
    public ModelPhysics ModelPhysics { get; set; }
    public DamageInfo DamageInfo { get; set; }
    
    public void ApplyRagdollImpulses(Vector3 position, Vector3 force)
    {
        if (!ModelPhysics.IsValid())
        {
            return;
        }
        
        foreach (var physicsBody in ModelPhysics.Bodies)
        {
            physicsBody.Component.ApplyImpulseAt(position, force);
        }
    }
}