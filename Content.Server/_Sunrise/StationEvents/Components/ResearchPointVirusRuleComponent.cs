using Content.Server._Sunrise.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;

namespace Content.Server._Sunrise.StationEvents.Components;

/// <summary>
/// Configuration for the research point virus station event.
/// </summary>
[RegisterComponent, Access(typeof(ResearchPointVirusRule))]
public sealed partial class ResearchPointVirusRuleComponent : Component
{
    /// <summary>
    /// How many research points to steal.
    /// </summary>
    [DataField]
    public MinMax PointTheftRange = new(5000, 20000);

    /// <summary>
    /// Planned station for this event instance.
    /// </summary>
    public EntityUid? TargetStation;

    /// <summary>
    /// Planned amount to steal for this event instance.
    /// </summary>
    public int PlannedTheft;
}
