using Content.Server._Sunrise.StationEvents.Events;
using Content.Shared.Access;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.StationEvents.Components;

/// <summary>
/// Configuration for the pirate virus station event.
/// </summary>
[RegisterComponent, Access(typeof(PirateVirusRule))]
public sealed partial class PirateVirusRuleComponent : Component
{
    /// <summary>
    /// How many credits to steal from cargo.
    /// </summary>
    [DataField]
    public MinMax CreditTheftRange = new(10000, 30000);

    /// <summary>
    /// Station bank account to steal funds from.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> CargoAccount = "Cargo";
}
