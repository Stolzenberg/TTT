using Sandbox.Events;

namespace Mountain;

/// <summary>
/// Manages currency rewards for kills and round completion.
/// </summary>
public sealed class CurrencyRewardManager : Component, IGameEventHandler<GlobalKillEvent>,
    IGameEventHandler<RoundWonEvent>
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

    void IGameEventHandler<GlobalKillEvent>.OnGameEvent(GlobalKillEvent eventArgs)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        HandleKillReward(eventArgs.DamageInfo);
    }

    void IGameEventHandler<RoundWonEvent>.OnGameEvent(RoundWonEvent eventArgs)
    {
        if (!Networking.IsHost)
        {
            return;
        }

        AwardRoundCompletionCurrency(eventArgs.WinningTeam);
    }

    private void HandleKillReward(DamageInfo damageInfo)
    {
        if (!damageInfo.Attacker.IsValid() || !damageInfo.Victim.IsValid())
        {
            return;
        }

        var attackerPlayer = damageInfo.Attacker.GameObject.Root.GetComponentInChildren<Player>();
        var victimPlayer = damageInfo.Victim.GameObject.Root.GetComponentInChildren<Player>();

        if (!attackerPlayer.IsValid() || !victimPlayer.IsValid())
        {
            return;
        }

        if (attackerPlayer == victimPlayer)
        {
            // Suicide, no reward
            return;
        }

        var attackerClient = attackerPlayer.Client;
        var victimClient = victimPlayer.Client;

        if (!attackerClient.IsValid() || !victimClient.IsValid())
        {
            return;
        }

        var attackerTeam = attackerClient.Team;
        var victimTeam = victimClient.Team;

        // Check for team kill
        if (attackerTeam.AreTeamsAllied(victimTeam))
        {
            attackerClient.AddCurrency(-TeamKillPenalty, "Team Kill Penalty");

            return;
        }

        // Award currency based on teams
        if (attackerTeam == Team.Traitor)
        {
            // Traitor killing innocent or detective
            if (victimTeam == Team.Detective)
            {
                attackerClient.AddCurrency(KillDetectiveReward, "Killed Detective");
            }
            else if (victimTeam == Team.Innocent)
            {
                attackerClient.AddCurrency(KillInnocentReward, "Killed Innocent");
            }
        }
        else if (attackerTeam == Team.Innocent || attackerTeam == Team.Detective)
        {
            // Innocent or detective killing traitor
            if (victimTeam == Team.Traitor)
            {
                attackerClient.AddCurrency(KillTraitorReward, "Killed Traitor");
            }
        }
    }

    private void AwardRoundCompletionCurrency(Team winningTeam)
    {
        var clients = Game.ActiveScene.AllClients().Where(c =>
            c.IsConnected && c.Team != Team.Unassigned && c.Player.IsValid() &&
            c.Player.Health.State == LifeState.Alive).ToList();

        foreach (var client in clients)
        {
            // Base reward for playing the round
            client.AddCurrency(RoundBaseReward, "Round Participation");

            // Bonus for winning
            var normalizedTeam = client.Team == Team.Detective ? Team.Innocent : client.Team;
            var normalizedWinningTeam = winningTeam == Team.Detective ? Team.Innocent : winningTeam;

            if (normalizedTeam == normalizedWinningTeam)
            {
                client.AddCurrency(RoundWinBonus, "Round Victory");
            }
        }
    }
}