using System;
using Sandbox.Events;

namespace Mountain;

public sealed partial class Player : IGameEventHandler<KilledEvent>
{
    /// <summary>
    ///     The player's current karma value. Lower karma means less damage dealt to other players.
    /// </summary>
    [Sync(SyncFlags.FromHost)]
    public float Karma { get; set; } = 1000f;

    /// <summary>
    ///     Starting karma value for new players.
    /// </summary>
    [ConVar("karma_starting", Name = "Starting Karma", 
        Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated, 
        Help = "The karma value that players start with (default: 1000)")]
    public static float KarmaStarting { get; set; } = 1000f;

    /// <summary>
    ///     Maximum karma value.
    /// </summary>
    [ConVar("karma_max", Name = "Maximum Karma", 
        Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated,
        Help = "The maximum karma value a player can have (default: 1000)")]
    public static float KarmaMax { get; set; } = 1000f;

    /// <summary>
    ///     Minimum karma value.
    /// </summary>
    [ConVar("karma_min", Name = "Minimum Karma", 
        Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated,
        Help = "The minimum karma value a player can have (default: 100)")]
    public static float KarmaMin { get; set; } = 100f;

    /// <summary>
    ///     Karma penalty for killing an innocent.
    /// </summary>
    [ConVar("karma_penalty_innocent", Name = "Karma Penalty for Innocent Kill", 
        Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated,
        Help = "Amount of karma lost when an innocent kills another innocent (default: 150)")]
    public static float KarmaPenaltyInnocent { get; set; } = 150f;

    /// <summary>
    ///     Karma penalty for killing a detective.
    /// </summary>
    [ConVar("karma_penalty_detective", Name = "Karma Penalty for Detective Kill", 
        Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated,
        Help = "Amount of karma lost when an innocent kills a detective (default: 200)")]
    public static float KarmaPenaltyDetective { get; set; } = 200f;

    /// <summary>
    ///     Karma restoration per round for good behavior.
    /// </summary>
    [ConVar("karma_restoration", Name = "Karma Restoration Per Round", 
        Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated,
        Help = "Amount of karma restored each round if player doesn't team kill (default: 50)")]
    public static float KarmaRestoration { get; set; } = 50f;

    /// <summary>
    ///     Whether karma system is enabled.
    /// </summary>
    [ConVar("karma_enabled", Name = "Karma System Enabled", 
        Flags = ConVarFlags.GameSetting | ConVarFlags.Replicated,
        Help = "Enable or disable the karma system (default: true)")]
    public static bool KarmaEnabled { get; set; } = true;

    /// <summary>
    ///     Calculate the damage multiplier based on the player's current karma.
    /// </summary>
    /// <returns>Damage multiplier between 0 and 1</returns>
    public float GetKarmaDamageMultiplier()
    {
        if (!KarmaEnabled)
        {
            return 1f;
        }

        // Karma of 1000 = 100% damage (1.0 multiplier)
        // Karma of 500 = 50% damage (0.5 multiplier)
        // Karma of 100 = 10% damage (0.1 multiplier)
        return Math.Clamp(Karma / KarmaStarting, 0.1f, 1f);
    }

    /// <summary>
    ///     Initialize karma when the player starts.
    /// </summary>
    private void InitializeKarma()
    {
        if (Networking.IsHost)
        {
            Karma = KarmaStarting;
        }
    }

    /// <summary>
    ///     Restore some karma at the end of a round (for good behavior).
    /// </summary>
    public void RestoreKarma()
    {
        if (!Networking.IsHost || !KarmaEnabled)
        {
            return;
        }

        Karma = Math.Clamp(Karma + KarmaRestoration, KarmaMin, KarmaMax);
    }

    /// <summary>
    ///     Get the karma penalty amount for team killing based on the victim's team.
    /// </summary>
    private static float GetKarmaPenaltyForTeam(Team victimTeam)
    {
        return victimTeam switch
        {
            Team.Detective => KarmaPenaltyDetective,
            Team.Innocent => KarmaPenaltyInnocent,
            _ => 0f
        };
    }

    /// <summary>
    ///     Reduce karma when a player damages or kills a teammate.
    /// </summary>
    private void ProcessKarmaPenalty(DamageInfo damageInfo)
    {
        if (!Networking.IsHost || !KarmaEnabled)
        {
            return;
        }

        // Only process if both attacker and victim are players
        if (!damageInfo.Victim.IsValid() || !damageInfo.Attacker.IsValid())
        {
            return;
        }

        var victim = damageInfo.Victim.GameObject.Root.GetComponentInChildren<Player>();
        var attacker = damageInfo.Attacker.GameObject.Root.GetComponentInChildren<Player>();

        if (!victim.IsValid() || !attacker.IsValid())
        {
            return;
        }
        
        // Don't penalize for self-damage
        if (victim == attacker)
        {
            return;
        }

        // Get both players' teams
        var attackerTeam = attacker.Client.Team;
        var victimTeam = victim.Client.Team;

        // Check if this is team damage (allies attacking each other)
        if (!attackerTeam.AreTeamsAllied(victimTeam))
        {
            return; // Not team damage, no penalty
        }

        // Get the appropriate penalty based on victim's role
        var penalty = GetKarmaPenaltyForTeam(victimTeam);
        
        if (penalty <= 0f)
        {
            return;
        }

        // Scale penalty based on damage dealt (proportional to kill)
        var damageRatio = Math.Min(damageInfo.Damage / victim.Health.MaxHealth, 1f);
        var scaledPenalty = penalty * damageRatio;

        attacker.Karma = Math.Clamp(attacker.Karma - scaledPenalty, KarmaMin, KarmaMax);
            
        Log.Info($"{attacker.Client.DisplayName} lost {scaledPenalty:F1} karma for team damage. New karma: {attacker.Karma:F1}");
    }

    void IGameEventHandler<KilledEvent>.OnGameEvent(KilledEvent eventArgs)
    {
        ProcessKarmaPenalty(eventArgs.DamageInfo);
    }
}
