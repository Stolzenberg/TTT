namespace Mountain;

/// <summary>
/// A pickupable currency item that players can collect from the map.
/// </summary>
public sealed class DroppedCurrency : DroppedLoot
{
    /// <summary>
    /// Amount of currency in a dropped currency pickup.
    /// </summary>
    [ConVar("currency_pickup_amount", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(1, 10000)]
    public static int PickupAmount { get; set; } = 50;

    [Property]
    public int Amount { get; set; } = 50;

    [Property]
    public Model? CurrencyModel { get; set; }

    [Property]
    public SoundEvent? PickupSound { get; set; }

    public override bool TryPickup(Player player)
    {
        if (!player.IsValid() || !player.Client.IsValid())
        {
            return false;
        }

        player.Client.AddCurrency(Amount, "Pickup");

        // Play pickup sound
        if (PickupSound != null)
        {
            Sound.Play(PickupSound, WorldPosition);
        }

        return true;
    }

    public override string GetDisplayName()
    {
        return $"${Amount}";
    }

    /// <summary>
    /// Spawn a currency pickup at the specified position.
    /// </summary>
    public static DroppedCurrency Create(int amount, Vector3 position, Rotation rotation, Model? customModel = null)
    {
        var actualAmount = amount > 0 ? amount : PickupAmount;
        var gameObject = new GameObject
        {
            WorldPosition = position,
            WorldRotation = rotation,
            Name = $"Currency_Amount_${actualAmount}",
        };

        var currency = gameObject.Components.Create<DroppedCurrency>();
        currency.Amount = actualAmount;
        currency.CurrencyModel = customModel ?? GetDefaultModel();

        return currency;
    }

    protected override void OnStart()
    {
        var bounds = CurrencyModel?.Bounds ?? BBox.FromPositionAndSize(Vector3.Zero, 16f);
        InitializeDroppedLoot(GameObject, bounds);
    }

    protected override void CreateVisuals(GameObject gameObject)
    {
        if (CurrencyModel == null)
        {
            return;
        }

        var renderer = gameObject.Components.Create<ModelRenderer>();
        renderer.Model = CurrencyModel;
    }
}