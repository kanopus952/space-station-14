using Content.Server._Sunrise.StationEvents.Components;
using Content.Server.Cargo.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.Cargo.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using System.Globalization;

namespace Content.Server._Sunrise.StationEvents.Events;

/// <summary>
/// Pirate virus event: hacks random station airlocks and steals credits from cargo.
/// </summary>
public sealed class PirateVirusRule : StationEventSystem<PirateVirusRuleComponent>
{
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, PirateVirusRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (!TryGetRandomStation(out var randomStation))
            return;

        if (!TryComp<StationBankAccountComponent>(randomStation, out var bank))
            return;

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        var cargoBalance = _cargo.GetBalanceFromAccount((randomStation.Value, bank), component.CargoAccount);
        if (cargoBalance <= 0)
            return;

        var theftMax = component.CreditTheftRange.Next(_random);

        theftMax = Math.Min(theftMax, Math.Max(0, cargoBalance));

        var stolen = Math.Min(theftMax, cargoBalance);

        var amountText = Math.Round((double)stolen, 2).ToString("F2", CultureInfo.InvariantCulture);
        stationEvent.StartAnnouncement = Loc.GetString("station-event-pirate-virus-start-announcement", ("amount", amountText));

        base.Started(uid, component, gameRule, args);

        _cargo.UpdateBankAccount((randomStation.Value, bank), -stolen, component.CargoAccount);
    }
}
