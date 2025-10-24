using Sandbox.Events;

namespace Mountain;

/// <summary>
/// Manages currency rewards for kills and round completion.
/// </summary>
public sealed class CurrencyRewardManager : Component, IGameEventHandler<GlobalKillEvent>,
    IGameEventHandler<RoundWonEvent>
{
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
            attackerClient.AddCurrency(-CurrencyConfig.TeamKillPenalty, "Team Kill Penalty");

            return;
        }

        // Award currency based on teams
        if (attackerTeam == Team.Traitor)
        {
            // Traitor killing innocent or detective
            if (victimTeam == Team.Detective)
            {
                attackerClient.AddCurrency(CurrencyConfig.KillDetectiveReward, "Killed Detective");
            }
            else if (victimTeam == Team.Innocent)
            {
                attackerClient.AddCurrency(CurrencyConfig.KillInnocentReward, "Killed Innocent");
            }
        }
        else if (attackerTeam == Team.Innocent || attackerTeam == Team.Detective)
        {
            // Innocent or detective killing traitor
            if (victimTeam == Team.Traitor)
            {
                attackerClient.AddCurrency(CurrencyConfig.KillTraitorReward, "Killed Traitor");
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
            client.AddCurrency(CurrencyConfig.RoundBaseReward, "Round Participation");

            // Bonus for winning
            var normalizedTeam = client.Team == Team.Detective ? Team.Innocent : client.Team;
            var normalizedWinningTeam = winningTeam == Team.Detective ? Team.Innocent : winningTeam;

            if (normalizedTeam == normalizedWinningTeam)
            {
                client.AddCurrency(CurrencyConfig.RoundWinBonus, "Round Victory");
            }
        }
    }
}