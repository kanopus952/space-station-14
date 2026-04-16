using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Localization;

namespace Content.Shared._Sunrise.Roadmap;

[Prototype]
public sealed partial class RoadmapVersionsPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField]
    public string Fork { get; set; } = "SUNRISE";

    [DataField]
    public List<RoadmapGroup> Versions = [];
}

[DataDefinition]
public partial record struct RoadmapGroup
{
    [DataField] public string Name;

    [DataField] public List<RoadmapGoal> Goals = [];
}

[DataDefinition]
public sealed partial class RoadmapGoal : ISerializationHooks
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField("name")]
    public LocId? SetName;

    [DataField("desc")]
    public LocId? SetDesc;

    public LocId Name;

    public LocId Desc;

    [DataField("localizationId")]
    public string? CustomLocalizationID;

    [DataField] public RoadmapItemState State = RoadmapItemState.Planned;

    void ISerializationHooks.AfterDeserialization()
    {
        var locId = CustomLocalizationID ?? $"roadmap-goal-{Id}";
        Name = SetName ?? locId;
        Desc = SetDesc ?? $"{locId}.desc";
    }
}

[Serializable, NetSerializable]
public enum RoadmapItemState
{
    Planned,
    InProgress,
    Partial,
    Complete,
}
