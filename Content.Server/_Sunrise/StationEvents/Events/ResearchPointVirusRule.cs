using Content.Server._Sunrise.StationEvents.Components;
using Content.Server.Research.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Research.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;
using System.Globalization;

namespace Content.Server._Sunrise.StationEvents.Events;

/// <summary>
/// Research point virus event: steals a fixed amount of points from station research servers.
/// </summary>
public sealed class ResearchPointVirusRule : StationEventSystem<ResearchPointVirusRuleComponent>
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Added(EntityUid uid, ResearchPointVirusRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        component.TargetStation = null;
        component.PlannedTheft = 0;

        if (!TryGetRandomStation(out var station))
        {
            stationEvent.StartAnnouncement = Loc.GetString(
                "station-event-research-point-virus-start-announcement",
                ("amount", FormatAmount(component.PlannedTheft)));
            base.Added(uid, component, gameRule, args);
            return;
        }

        component.TargetStation = station.Value;
        var requestedTheft = component.PointTheftRange.Next(_random);
        var availablePoints = GetStationAvailablePoints(station.Value);
        component.PlannedTheft = Math.Min(requestedTheft, availablePoints);

        stationEvent.StartAnnouncement = Loc.GetString(
            "station-event-research-point-virus-start-announcement",
            ("amount", FormatAmount(component.PlannedTheft)));

        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, ResearchPointVirusRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.PlannedTheft <= 0)
            return;

        var station = component.TargetStation;
        if (station == null || Deleted(station.Value))
        {
            if (!TryGetRandomStation(out station))
                return;

            component.TargetStation = station.Value;
        }

        var servers = GetStationResearchServers(station.Value);
        if (servers.Count == 0)
            return;

        _random.Shuffle(servers);

        var remainingToSteal = component.PlannedTheft;
        foreach (var server in servers)
        {
            if (remainingToSteal <= 0)
                break;

            if (server.Comp.Points <= 0)
                continue;

            var stolenPoints = Math.Min(server.Comp.Points, remainingToSteal);
            _research.ModifyServerPoints(server.Uid, -stolenPoints, server.Comp);
            remainingToSteal -= stolenPoints;
        }
    }

    private int GetStationAvailablePoints(EntityUid station)
    {
        var total = 0;
        foreach (var server in GetStationResearchServers(station))
        {
            total += Math.Max(0, server.Comp.Points);
        }

        return total;
    }

    private List<(EntityUid Uid, ResearchServerComponent Comp)> GetStationResearchServers(EntityUid station)
    {
        var servers = new List<(EntityUid Uid, ResearchServerComponent Comp)>();
        var query = EntityQueryEnumerator<ResearchServerComponent, TransformComponent>();
        while (query.MoveNext(out var serverUid, out var serverComp, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != station)
                continue;

            servers.Add((serverUid, serverComp));
        }

        return servers;
    }

    private static string FormatAmount(int amount)
    {
        return Math.Round((double) amount, 2).ToString("F2", CultureInfo.InvariantCulture);
    }
}
