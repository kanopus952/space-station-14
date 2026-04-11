namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Marks a shuttle grid as the ninja's escape shuttle on the outpost map.
/// Set by <see cref="NinjaRuleSystem"/> when the outpost map is loaded.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaShuttleComponent : Component
{
    /// <summary>
    /// Link to the associated <see cref="NinjaRuleComponent"/> game rule entity.
    /// </summary>
    public EntityUid? AssociatedRule;
}
