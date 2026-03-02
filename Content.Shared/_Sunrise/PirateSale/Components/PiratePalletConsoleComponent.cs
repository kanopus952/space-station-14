using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.PirateSale.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[AutoGenerateComponentPause]
public sealed partial class PiratePalletConsoleComponent : Component
{
    [DataField]
    public SoundSpecifier ErrorSound = new SoundCollectionSpecifier("CargoError");

    /// <summary>
    /// The time at which the console will be able to play the deny sound.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDenySoundTime = TimeSpan.Zero;

    /// <summary>
    /// The time between playing the deny sound.
    /// </summary>
    [DataField]
    public TimeSpan DenySoundDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How many credits are converted into one doubloon.
    /// </summary>
    [DataField]
    public int CreditsPerDoubloon = 250;

    /// <summary>
    /// Stack prototype used when spawning converted doubloons.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> DoubloonStack = "Doubloon";

    /// <summary>
    /// Keeps unconverted credits between sales so partial conversions are not lost.
    /// </summary>
    [DataField]
    public int CreditRemainder = 0;

    /// <summary>
    /// Cached UI data synchronized to clients.
    /// </summary>
    [AutoNetworkedField]
    public int UiAppraisalCredits;

    [AutoNetworkedField]
    public int UiDoubloons;

    [AutoNetworkedField]
    public int UiCount;

    [AutoNetworkedField]
    public bool UiEnabled;
}
