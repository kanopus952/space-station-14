using Content.Shared._Sunrise.TTS;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Prototypes;

[Prototype]
/// <summary>
///     Prototype describing a single step in a tutorial sequence.
///     A step may display a tutorial bubble, send chat and/or TTS messages,
///     and is completed when all specified conditions are satisfied.
/// </summary>
public sealed partial class TutorialStepPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Optional tutorial bubble displayed to the player during this step.
    /// </summary>
    [DataField]
    public TutorialBubbleData? Bubble;

    /// <summary>
    ///     Chat message sent when this step becomes active.
    /// </summary>
    [DataField]
    public string ChatMessage = string.Empty;

    /// <summary>
    ///     Text passed to the TTS system when this step becomes active.
    /// </summary>
    [DataField]
    public string TTSMessage = string.Empty;

    /// <summary>
    ///     Sender name used for chat message (like narrator).
    /// </summary>
    [DataField]
    public string Sender = "base-tutorial-sender";

    /// <summary>
    ///     TTS voice prototype used to play the TTS message.
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype> VoiceId = "Jirinovskiy"!;

    /// <summary>
    ///     If true, this step may be skipped without blocking tutorial progress.
    /// </summary>
    [DataField]
    public bool Optional;

    /// <summary>
    ///     Range that used for finding entities in conditions
    /// </summary>
    [DataField]
    public int ObserveRange = 10;

    /// <summary>
    ///     Conditions that must be satisfied to complete this tutorial step.
    /// </summary>
    [DataField]
    public List<TutorialCondition> Conditions = new();
}

/// <summary>
///     Data describing a tutorial bubble shown to the player.
/// </summary>
[DataDefinition]
public sealed partial class TutorialBubbleData
{
    /// <summary>
    ///     Text displayed inside the tutorial bubble.
    /// </summary>
    [DataField(required: true)]
    public string Text = string.Empty;

    /// <summary>
    ///     Target that the tutorial bubble is anchored to.
    /// </summary>
    [DataField(required: true)]
    public TutorialBubbleTarget Target;
}

/// <summary>
///     Defines the target that a tutorial bubble is attached to.
/// </summary>
[DataDefinition]
public sealed partial class TutorialBubbleTarget
{
    /// <summary>
    ///     Type of the bubble target (self or specific entity).
    /// </summary>
    [DataField(required: true)]
    public TutorialBubbleTargetType Type;

    /// <summary>
    ///     Entity prototype used as a target when <see cref="Type"/> is Entity.
    /// </summary>
    [DataField]
    public EntProtoId? Prototype;
}
/// <summary>
///     Specifies how the tutorial bubble target is resolved.
/// </summary>
public enum TutorialBubbleTargetType
{
    /// <summary>
    ///     Bubble is attached to the player entity.
    /// </summary>
    Self,

    /// <summary>
    ///     Bubble is attached to an entity matching the specified prototype.
    /// </summary>
    Entity
}
