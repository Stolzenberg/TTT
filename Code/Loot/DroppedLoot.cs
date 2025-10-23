namespace Mountain;

/// <summary>
/// Base class for all dropped loot items that can be picked up by players.
/// </summary>
public abstract class DroppedLoot : Component, Component.ITriggerListener, IPickupable
{
    public Rigidbody Rigidbody { get; set; } = null!;

    private const float PickupCooldown = 0.1f;
    protected TimeSince timeSinceDropped;

    /// <summary>
    /// Called when a player attempts to pick up this item.
    /// </summary>
    public abstract bool TryPickup(Player player);

    /// <summary>
    /// Gets the display name of this pickupable item.
    /// </summary>
    public abstract string GetDisplayName();

    public void OnTriggerEnter(GameObject other)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        var player = other.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        if (timeSinceDropped < PickupCooldown)
        {
            return;
        }

        if (TryPickup(player))
        {
            GameObject.Destroy();
            OnPickedUp(player);
        }
    }

    /// <summary>
    /// Called after the item has been successfully picked up and is about to be destroyed.
    /// </summary>
    protected virtual void OnPickedUp(Player player)
    {
        Log.Info($"{player.Client.DisplayName} picked up {GetDisplayName()}.");
    }

    /// <summary>
    /// Creates the visual representation of the dropped loot.
    /// </summary>
    protected abstract void CreateVisuals(GameObject gameObject);

    /// <summary>
    /// Creates the physics colliders for the dropped loot.
    /// </summary>
    protected virtual void CreateColliders(GameObject gameObject, BBox bounds)
    {
        var trigger = gameObject.Components.Create<SphereCollider>();
        trigger.IsTrigger = true;
        trigger.Radius = 32f;

        var min = bounds.Mins;
        var max = bounds.Maxs;

        var collider = gameObject.Components.Create<BoxCollider>();
        collider.Scale = new(max.x - min.x, max.y - min.y, max.z - min.z);
        collider.Center = new(0, 0, (max.z - min.z) / 2);
    }

    /// <summary>
    /// Sets up the rigidbody component for the dropped loot.
    /// </summary>
    protected virtual void CreateRigidbody(GameObject gameObject)
    {
        Rigidbody = gameObject.Components.Create<Rigidbody>();
        Rigidbody.MassOverride = 15f;
    }

    /// <summary>
    /// Initializes the dropped loot GameObject with common components.
    /// </summary>
    protected void InitializeDroppedLoot(GameObject gameObject, BBox bounds)
    {
        timeSinceDropped = 0;

        CreateVisuals(gameObject);
        CreateColliders(gameObject, bounds);
        CreateRigidbody(gameObject);

        gameObject.Components.Create<DestroyBetweenRounds>();
        gameObject.Tags.Add("pickup");
    }
}