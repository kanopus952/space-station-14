// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Humanoid;

/// <summary>
///     Stores the TTS voice for a humanoid character.
///     Companion component to <see cref="HumanoidAppearanceComponent"/>.
///     Auto-attached to any entity that receives <see cref="HumanoidAppearanceComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanoidVoiceComponent : Component
{
    /// <summary>
    ///     The TTS voice prototype used for this humanoid's speech synthesis.
    /// </summary>
    [DataField("voice"), AutoNetworkedField]
    public ProtoId<TTSVoicePrototype> Voice = SharedHumanoidAppearanceSystem.DefaultVoice;
}
