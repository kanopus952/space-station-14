using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared._Sunrise.Shuttles;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Content.Shared.Whitelist;

namespace Content.Server._Sunrise.GameTicking.Rules;

public sealed class NinjaRuleSystem : GameRuleSystem<NinjaRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly MapSystem _map = default!;

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

    protected override void Ended(
        EntityUid uid,
        NinjaRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        RemoveOutpostFtl(component);
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

    protected override void ActiveTick(
        EntityUid uid,
        NinjaRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime)
    {
        if (component.FtlUnlocked)
            return;

        if (Timing.CurTime < component.UpdateNextTime)
            return;

        component.UpdateNextTime = Timing.CurTime + component.ObjectiveCheckInterval;

        // Find a living ninja and check whether all non-return objectives are done.
        var ninjaQuery = EntityQueryEnumerator<SpaceNinjaComponent, MobStateComponent, MindContainerComponent>();
        while (ninjaQuery.MoveNext(out var ninjaUid, out _, out var mobState, out var mindContainer))
        {
            if (mobState.CurrentState != MobState.Alive)
                continue;

            Entity<MindContainerComponent?> mob = (ninjaUid, mindContainer);
            if (_mind.GetMind(mob, mob.Comp) is not { } mindId)
                continue;

            if (!TryComp<MindComponent>(mindId, out var mind))
                continue;

            if (!AllNonReturnObjectivesComplete(mindId, mind))
                break;

            UnlockFtl(component);
            NotifyNinja(mind, Loc.GetString("ninja-ftl-unlocked"));
            break;
        }
    }

    private bool AllNonReturnObjectivesComplete(EntityUid mindId, MindComponent mind)
    {
        foreach (var objUid in mind.Objectives)
        {
            if (MetaData(objUid).EntityPrototype?.ID == NinjaReturnToBaseObjectiveProto)
                continue;

            if (!_objectives.IsCompleted(objUid, (mindId, mind)))
                return false;
        }

        return true;
    }

    private void UnlockFtl(NinjaRuleComponent component)
    {
        component.FtlUnlocked = true;
        SetOutpostFtl(component);
    }

    private void SetOutpostFtl(NinjaRuleComponent component)
    {
        if (component.OutpostMapEntity is not { } mapEnt || !Exists(mapEnt))
            return;

        var ftlComp = EnsureComp<FTLDestinationComponent>(mapEnt);

        ftlComp.Enabled = true;
        ftlComp.Whitelist = new EntityWhitelist
        {
            Components = ["NinjaShuttle"]
        };
        Dirty(mapEnt, ftlComp);
        _console.RefreshShuttleConsoles();
    }

    private void RemoveOutpostFtl(NinjaRuleComponent component)
    {
        if (component.OutpostMapEntity is not { } mapEnt || !Exists(mapEnt))
            return;

        var ftlComp = EnsureComp<FTLDestinationComponent>(mapEnt);

        ftlComp.Enabled = false;
        ftlComp.Whitelist = null;

        Dirty(mapEnt, ftlComp);
        _console.RefreshShuttleConsoles();
    }


    private void NotifyNinja(MindComponent mind, string message)
    {
        if (mind.UserId is not { } userId)
            return;

        if (!_playerManager.TryGetSessionById(userId, out var session))
            return;

        _chatManager.DispatchServerMessage(session, message);
    }

    private void OnRuleLoadedGrids(Entity<NinjaRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        var query = EntityQueryEnumerator<NinjaShuttleComponent>();
        while (query.MoveNext(out var uid, out var shuttle))
        {
            if (Transform(uid).MapID != args.Map)
                continue;

            ent.Comp.OutpostMapEntity = _map.GetMap(args.Map);

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

        if (ninjaUid == null)
            return;

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

        if (!AllNonReturnObjectivesComplete(mindId, mind))
            return;

        _codeCondition.SetCompleted(mob, NinjaReturnToBaseObjectiveProto);
    }
}
