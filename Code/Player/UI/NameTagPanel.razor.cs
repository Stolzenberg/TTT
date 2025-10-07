using System;
using Sandbox.Events;

namespace Mountain;

public partial class NameTagPanel : PanelComponent, IGameEventHandler<GlobalKillEvent>
{
    private Player NameTagOwningPlayer => this.GetPlayerFromComponent() ?? throw new InvalidOperationException("NameTagPanel must be a child of a player.");

    protected override int BuildHash()
    {
        return HashCode.Combine(NameTagOwningPlayer.Client.DisplayName);
    }

    void IGameEventHandler<GlobalKillEvent>.OnGameEvent(GlobalKillEvent eventArgs)
    {
        if (eventArgs.DamageInfo.Victim != NameTagOwningPlayer.Health) return;

        Destroy();
    }
}