namespace Mountain;

/// <summary>
/// Configuration for currency rewards in the game.
/// </summary>
public static class CurrencyConfig
{
    /// <summary>
    /// Base currency reward for completing a round (win or lose).
    /// </summary>
    [ConVar("currency_round_base", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(0, 10000)]
    public static int RoundBaseReward { get; set; } = 100;

    /// <summary>
    /// Bonus currency for winning a round.
    /// </summary>
    [ConVar("currency_round_win_bonus", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(0, 10000)]
    public static int RoundWinBonus { get; set; } = 200;

    /// <summary>
    /// Currency reward for innocents/detectives killing a traitor.
    /// </summary>
    [ConVar("currency_kill_traitor", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(0, 10000)]
    public static int KillTraitorReward { get; set; } = 500;

    /// <summary>
    /// Currency reward for traitors killing an innocent.
    /// </summary>
    [ConVar("currency_kill_innocent", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(0, 10000)]
    public static int KillInnocentReward { get; set; } = 100;

    /// <summary>
    /// Currency reward for traitors killing a detective.
    /// </summary>
    [ConVar("currency_kill_detective", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(0, 10000)]
    public static int KillDetectiveReward { get; set; } = 150;

    /// <summary>
    /// Penalty for killing a teammate.
    /// </summary>
    [ConVar("currency_teamkill_penalty", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(0, 10000)]
    public static int TeamKillPenalty { get; set; } = 300;

    /// <summary>
    /// Amount of currency in a dropped currency pickup.
    /// </summary>
    [ConVar("currency_pickup_amount", ConVarFlags.GameSetting | ConVarFlags.Replicated), Range(1, 10000)]
    public static int PickupAmount { get; set; } = 50;
}