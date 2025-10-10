using Sandbox.Audio;

namespace Mountain;

public partial class ClientVoice : Voice
{
    /// <summary>
    /// The target <see cref="Client"/>
    /// </summary>
    [Property]
    public Client Client { get; set; }

    protected override void OnStart()
    {
        TargetMixer = Mixer.FindMixerByName("Voice");
        Volume = Preferences.VoipVolume;
    }
}