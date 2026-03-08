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

    protected override void Added(EntityUid uid, PirateVirusRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        component.CreditTheft ??= component.CreditTheftRange.Next(_random);
        component.CreditTheft = Math.Max(component.CreditTheft.Value, 0);

        stationEvent.StartAnnouncement = Loc.GetString(
            "station-event-pirate-virus-start-announcement",
            ("amount", FormatAmount(component.CreditTheft.Value)));

        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, PirateVirusRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.CreditTheft == null || component.CreditTheft <= 0)
            return;

        if (!TryGetRandomStation(out var randomStation))
            return;

        if (!TryComp<StationBankAccountComponent>(randomStation, out var bank))
            return;

        _cargo.UpdateBankAccount((randomStation.Value, bank), -component.CreditTheft.Value, component.CargoAccount);
    }

    private static string FormatAmount(int amount)
    {
        return Math.Round((double)amount, 2).ToString("F2", CultureInfo.InvariantCulture);
    }
}
