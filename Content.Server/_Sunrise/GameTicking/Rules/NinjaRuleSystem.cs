using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.GameTicking.Rules;

/// <summary>
/// Manages the space ninja outpost game rule:
/// loads the outpost map, links the escape shuttle, checks all objectives
/// when the ninja boards the shuttle, marks the return-to-base objective
/// complete if they're all done, then deletes the ninja and the shuttle.
/// </summary>
public sealed class NinjaRuleSystem : GameRuleSystem<NinjaRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    private const string NinjaReturnToBaseObjectiveProto = "NinjaReturnToBaseObjective";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
        SubscribeLocalEvent<FTLStartedEvent>(OnFTLStarted);
    }

    protected override void Started(
        EntityUid uid,
        NinjaRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        var eligible = new List<EntityUid>();
        var query = EntityQueryEnumerator<StationEventEligibleComponent>();
        while (query.MoveNext(out var stationUid, out _))
            eligible.Add(stationUid);

        if (eligible.Count == 0)
            return;

        component.TargetStation = RobustRandom.Pick(eligible);
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        NinjaRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        args.AddLine(Loc.GetString("ninja-round-end-title"));

        foreach (var (_, sessionData, name) in _antag.GetAntagIdentifiers(uid))
        {
            args.AddLine(Loc.GetString("ninja-round-end-name-user",
                ("name", name),
                ("user", sessionData.UserName)));
        }

        args.AddLine(Loc.GetString(component.EscapedOnShuttle
            ? "ninja-round-end-escaped"
            : "ninja-round-end-not-escaped"));
    }

    private void OnRuleLoadedGrids(Entity<NinjaRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        var query = EntityQueryEnumerator<NinjaShuttleComponent>();
        while (query.MoveNext(out var uid, out var shuttle))
        {
            if (Transform(uid).MapID != args.Map)
                continue;

            shuttle.AssociatedRule = ent;
            break;
        }
    }

    private void OnFTLStarted(ref FTLStartedEvent ev)
    {
        if (!TryComp<NinjaShuttleComponent>(ev.Entity, out var shuttle))
            return;

        if (shuttle.AssociatedRule is not { } ruleUid)
            return;

        if (!TryComp<NinjaRuleComponent>(ruleUid, out var rule))
            return;

        // Find a living ninja aboard the departing shuttle.
        EntityUid? ninjaUid = null;
        var ninjaQuery = EntityQueryEnumerator<SpaceNinjaComponent, MobStateComponent, TransformComponent>();
        while (ninjaQuery.MoveNext(out var uid, out _, out var mobState, out var xform))
        {
            if (xform.GridUid != ev.Entity)
                continue;

            if (mobState.CurrentState != MobState.Alive)
                continue;

            ninjaUid = uid;
            break;
        }

        rule.EscapedOnShuttle = true;

        // Mark the return-to-base objective complete only when every OTHER
        // objective is already finished.
        TryMarkReturnObjective(ninjaUid);
    }

    /// <summary>
    /// Marks <see cref="NinjaReturnToBaseObjectiveProto"/> complete on the ninja's mind
    /// when every other objective in the mind is already completed.
    /// </summary>
    private void TryMarkReturnObjective(EntityUid? ninjaUid)
    {
        if (!TryComp<MindContainerComponent>(ninjaUid, out var mindContainer))
            return;

        Entity<MindContainerComponent?> mob = (ninjaUid.Value, mindContainer);
        if (_mind.GetMind(mob, mob.Comp) is not { } mindId)
            return;

        if (!TryComp<MindComponent>(mindId, out var mind))
            return;

        foreach (var objUid in mind.Objectives)
        {
            if (MetaData(objUid).EntityPrototype?.ID == NinjaReturnToBaseObjectiveProto)
                continue;

            if (!_objectives.IsCompleted(objUid, (mindId, mind)))
                return;
        }

        _codeCondition.SetCompleted(mob, NinjaReturnToBaseObjectiveProto);
    }
}
