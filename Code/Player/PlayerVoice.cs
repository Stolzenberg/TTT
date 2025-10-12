using System;
using Sandbox.Audio;

namespace Mountain;

public partial class PlayerVoice : Voice
{
    public Player Player { get; private set; }

    protected override void OnStart()
    {
        Player = this.GetPlayerFromComponent() ?? throw new InvalidOperationException("PlayerVoice must be a child of a player.");
        Renderer = Player.BodyRenderer;
        Volume = Preferences.VoipVolume * 100f;
        
        TargetMixer = Mixer.FindMixerByName("Voice");
    }
}