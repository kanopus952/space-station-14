using Content.Server._Sunrise.GameTicking.Rules;
using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores state for the ninja outpost game rule.
/// Tracks the target station and whether the ninja escaped on their shuttle.
/// </summary>
[RegisterComponent, Access(typeof(NinjaRuleSystem))]
public sealed partial class NinjaRuleComponent : Component
{
    /// <summary>
    /// The station the ninja is operating against.
    /// Set in <see cref="NinjaRuleSystem.Started"/>.
    /// </summary>
    public EntityUid? TargetStation;

    /// <summary>
    /// Whether the ninja successfully escaped by boarding their shuttle during FTL.
    /// </summary>
    public bool EscapedOnShuttle;

    /// <summary>
    /// Sound played as a notification when the ninja is selected.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Misc/ninja_greeting.ogg");
}
