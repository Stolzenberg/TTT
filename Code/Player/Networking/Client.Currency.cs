using System;
using Sandbox.Events;

namespace Mountain;

/// <summary>
/// Event dispatched when a client's currency changes.
/// </summary>
public record CurrencyChangedEvent(Client Client, int OldAmount, int NewAmount, int Delta, string Reason) : IGameEvent;

public partial class Client
{
    /// <summary>
    /// The amount of currency this player currently has.
    /// </summary>
    [Sync(SyncFlags.FromHost), Change(nameof(OnCurrencyChanged))]
    public int Currency { get; private set; }

    /// <summary>
    /// Add currency to this player.
    /// </summary>
    /// <param name="amount">Amount to add (can be negative to subtract)</param>
    /// <param name="reason">Reason for the currency change (for logging/UI)</param>
    public void AddCurrency(int amount, string reason = "Unknown")
    {
        if (!Networking.IsHost)
        {
            return;
        }

        var oldAmount = Currency;
        Currency = Math.Max(0, Currency + amount);

        BroadcastCurrencyChange(oldAmount, Currency, amount, reason);

        Log.Info(
            $"{DisplayName} {(amount >= 0 ? "earned" : "lost")} ${Math.Abs(amount)} ({reason}). Total: ${Currency}");
    }

    /// <summary>
    /// Set the player's currency to a specific amount.
    /// </summary>
    public void SetCurrency(int amount, string reason = "Set")
    {
        if (!Networking.IsHost)
        {
            return;
        }

        var oldAmount = Currency;
        Currency = Math.Max(0, amount);

        BroadcastCurrencyChange(oldAmount, Currency, Currency - oldAmount, reason);
    }

    /// <summary>
    /// Try to spend currency. Returns true if the player had enough currency.
    /// </summary>
    public bool TrySpendCurrency(int amount, string reason = "Purchase")
    {
        if (!Networking.IsHost)
        {
            return false;
        }

        if (Currency < amount)
        {
            return false;
        }

        AddCurrency(-amount, reason);

        return true;
    }

    [Rpc.Broadcast]
    private void BroadcastCurrencyChange(int oldAmount, int newAmount, int delta, string reason)
    {
        Scene.Dispatch(new CurrencyChangedEvent(this, oldAmount, newAmount, delta, reason));
    }

    private void OnCurrencyChanged(int oldAmount, int newAmount)
    {
        // Can be used for additional client-side effects
    }
}