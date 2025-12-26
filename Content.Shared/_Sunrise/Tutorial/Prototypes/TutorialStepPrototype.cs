using Content.Shared._Sunrise.TTS;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Prototypes;

[Prototype]
public sealed partial class TutorialStepPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public TutorialBubbleData? Bubble;

    [DataField]
    public string ChatMessage = string.Empty;

    [DataField]
    public string TTSMessage = string.Empty;

    [DataField]
    public string Sender = "base-tutorial-sender";

    [DataField]
    public ProtoId<TTSVoicePrototype> VoiceId = "Jirinovskiy"!;

    [DataField]
    public bool Optional;

    [DataField]
    public List<TutorialCondition> Conditions = new();
}
[DataDefinition]
public sealed partial class TutorialBubbleData
{
    [DataField(required: true)]
    public string Text = string.Empty;

    [DataField(required: true)]
    public TutorialBubbleTarget Target;
}

[DataDefinition]
public sealed partial class TutorialBubbleTarget
{
    [DataField(required: true)]
    public TutorialBubbleTargetType Type;

    [DataField]
    public EntProtoId? Prototype;
}

public enum TutorialBubbleTargetType
{
    Self,
    Entity
}
