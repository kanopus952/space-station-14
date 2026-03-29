using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server._Sunrise.TTS;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server._Sunrise.Auth;
using Content.Shared._Sunrise.TTS;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Eui;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Events;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.Map.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Configuration;
using Robust.Server.GameStates;
using Content.Shared._Sunrise.Tutorial.Components.Trackers;

namespace Content.Server._Sunrise.Tutorial;

/// <summary>
/// System for educating new players
/// </summary>
public sealed class TutorialSystem : SharedTutorialSystem
{
    [Dependency] private readonly TTSSystem _tts = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly AccountCreationManager _accountCreation = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private EntityUid? _tutorialMap;
    private readonly Dictionary<ICommonSession, TutorialCompletionEui> _completionEuis = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialPlayerComponent, TutorialStepChangedEvent>(OnStepChanged);
        SubscribeLocalEvent<TutorialPlayerComponent, TutorialStepsCompletedEvent>(OnStepsCompleted);
        SubscribeLocalEvent<TutorialPlayerComponent, TutorialEndedEvent>(OnTutorialComplete);
        SubscribeLocalEvent<TutorialPlayerComponent, ExpandPvsEvent>(OnExpandPvs);
        SubscribeNetworkEvent<TutorialQuitRequestEvent>(OnTutorialQuitRequest);
        SubscribeNetworkEvent<TutorialStartRequestEvent>(OnStartRequest);
        SubscribeNetworkEvent<TutorialWindowDataRequestEvent>(OnWindowDataRequest);
    }

    private void OnExpandPvs(Entity<TutorialPlayerComponent> ent, ref ExpandPvsEvent args)
    {
        args.Entities ??= [];

        if (Exists(ent.Comp.CurrentBubbleTarget))
            args.Entities.Add(ent.Comp.CurrentBubbleTarget.Value);

        if (Exists(ent.Comp.Target))
            args.Entities.Add(ent.Comp.Target.Value);
    }

    private void OnStepsCompleted(Entity<TutorialPlayerComponent> ent, ref TutorialStepsCompletedEvent args)
    {
        if (!_player.TryGetSessionByEntity(ent, out var session))
            return;

        SaveTutorialCompletion(session.UserId, ent.Comp.SequenceId);
        ShowCompletionEui(ent.Owner, session);
    }

    private void OnTutorialComplete(Entity<TutorialPlayerComponent> ent, ref TutorialEndedEvent args)
    {
        if (!_player.TryGetSessionByEntity(ent, out var session))
            return;

        CloseCompletionEui(session);
        QueueDel(ent.Comp.Grid);
        QueueDel(ent);

        _ticker.Respawn(session);
    }

    private async void SaveTutorialCompletion(NetUserId userId, ProtoId<TutorialSequencePrototype> sequenceId)
    {
        try
        {
            var createdTime = await _accountCreation.TryGetAccountCreatedTimeAsync(userId);

            TimeSpan? accountAge = null;
            if (createdTime.HasValue)
                accountAge = DateTimeOffset.UtcNow - createdTime.Value;

            await _db.AddTutorial(userId.UserId, sequenceId, accountAge);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save tutorial completion for {userId}: {e}");
        }
    }

    private void ShowCompletionEui(EntityUid player, ICommonSession session)
    {
        CloseCompletionEui(session);

        var eui = new TutorialCompletionEui(player);
        _completionEuis[session] = eui;
        _eui.OpenEui(eui, session);
    }

    private void CloseCompletionEui(ICommonSession session)
    {
        if (!_completionEuis.Remove(session, out var eui))
            return;

        if (!eui.IsShutDown)
            eui.Close();
    }

    public void OnCompletionEuiClosed(ICommonSession session)
    {
        _completionEuis.Remove(session);
    }

    public void HandleCompletionAction(EntityUid player, string actionId)
    {
        if (actionId != TutorialCompletionActions.Leave)
            return;

        if (!TryComp(player, out TutorialPlayerComponent? comp))
            return;

        EndTutorial((player, comp));
    }

    private void OnTutorialQuitRequest(TutorialQuitRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } entity)
            return;

        if (!TryComp(entity, out TutorialPlayerComponent? comp))
            return;

        EndTutorial((entity, comp));
    }

    private async void OnWindowDataRequest(TutorialWindowDataRequestEvent msg, EntitySessionEventArgs args)
    {
        List<string>? completed = null;

        try
        {
            completed = await _db.GetTutorial(args.SenderSession.UserId.UserId);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to fetch tutorial completion list for {args.SenderSession.UserId}: {e}");
        }

        completed ??= [];
        RaiseNetworkEvent(
            new TutorialWindowDataResponseEvent(completed),
            Filter.SinglePlayer(args.SenderSession));
    }

    private void OnStartRequest(TutorialStartRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!_proto.TryIndex(msg.SequenceId, out var sequence))
            return;

        if (!CanStartTutorial())
        {
            RaiseNetworkEvent(
                new TutorialStartDeniedEvent("tutorial-start-denied-max-active"),
                Filter.SinglePlayer(args.SenderSession));
            return;
        }

        TryCreateMap();
        var gridUid = LoadLocation(sequence.Grid);

        if (gridUid == EntityUid.Invalid)
            return;

        var spawnPoint = GetSpawnPoint(gridUid);

        if (!TrySpawnNextTo(sequence.PlayerEntity, spawnPoint, out var uid))
            return;

        if (spawnPoint == EntityUid.Invalid)
            return;

        var (mindId, _) = _mind.CreateMind(args.SenderSession.UserId);
        _mind.SetUserId(mindId, args.SenderSession.UserId);
        _mind.TransferTo(mindId, uid);
        _ticker.PlayerJoinGame(args.SenderSession, true);

        var tutorial = EnsureComp<TutorialPlayerComponent>(uid.Value);
        tutorial.SequenceId = msg.SequenceId;
        tutorial.Grid = gridUid;
        EnsureComp<TutorialProgressBarComponent>(uid.Value);
        InitializeTutorial((uid.Value, tutorial));
    }

    private bool CanStartTutorial()
    {
        var maxActive = _cfg.GetCVar(SunriseCCVars.TutorialMaxActive);
        if (maxActive <= 0)
            return true;

        var count = 0;
        var query = EntityQueryEnumerator<TutorialPlayerComponent>();
        while (query.MoveNext(out _))
        {
            count++;
            if (count >= maxActive)
                return false;
        }

        return true;
    }

    private async void OnStepChanged(EntityUid uid, TutorialPlayerComponent comp, TutorialStepChangedEvent args)
    {
        try
        {
            if (!_player.TryGetSessionByEntity(uid, out var session))
                return;

            if (!TryGetCurrentStep((uid, comp), out var step))
                return;

            if (!_proto.TryIndex(step.VoiceId, out var voice))
                return;

            var message = $"{Loc.GetString(step.Sender)} {Loc.GetString(step.ChatMessage)}";
            _chat.ChatMessageToOne(ChatChannel.Emotes, message, message, EntityUid.Invalid, false, session.Channel);

            RemComp<TutorialDistanceTrackerComponent>(uid);

            var tts = await GenerateTtsForTutorial(step.TtsMessage, voice);
            if (tts == null)
                return;

            var ev = new PlayTTSEvent(tts);
            RaiseNetworkEvent(ev, Filter.SinglePlayer(session));
        }
        catch (Exception e)
        {
            Log.Error($"Error in OnStepChanged: {e}");
        }
    }

    private async Task<byte[]?> GenerateTtsForTutorial(string text, TTSVoicePrototype voicePrototype)
    {
        try
        {
            return await _tts.GenerateTTS(text, voicePrototype, null);
        }
        catch (Exception e)
        {
            Log.Error($"TTS System error in tutorial generation: {e.Message}");
        }
        return null;
    }
    protected override void UpdateTimeCounter(Entity<TutorialPlayerComponent> ent, TimeSpan? endTime)
    {
        base.UpdateTimeCounter(ent, endTime);

        if (endTime == null)
        {
            RemComp<TimeCounterComponent>(ent);
            return;
        }

        var counter = EnsureComp<TimeCounterComponent>(ent);
        counter.EndTime = endTime;
        Dirty(ent, counter);
    }

    private EntityUid? TryCreateMap()
    {
        if (Exists(_tutorialMap))
            return _tutorialMap;

        var mapUid = _mapSystem.CreateMap();

        var comp = EnsureComp<TutorialMapComponent>(mapUid);
        _meta.SetEntityName(mapUid, comp.MapName);
        _tutorialMap = mapUid;

        return mapUid;
    }

    private EntityUid LoadLocation(ResPath gridPath)
    {
        if (!TryComp<MapComponent>(_tutorialMap, out var mapComp) ||
            !TryComp<TutorialMapComponent>(_tutorialMap, out var tutorialMap))
            return EntityUid.Invalid;

        CleanupDeletedGrids(tutorialMap);

        var offset = Vector2.Zero;

        if (tutorialMap.LoadedGrids.Count != 0)
        {
            var lastGrid = tutorialMap.LoadedGrids[^1];
            offset = tutorialMap.GridOffsets[lastGrid] + tutorialMap.CoordinateStep;
        }

        if (!_mapLoader.TryLoadGrid(mapComp.MapId, gridPath, out var grid, null, offset))
            return EntityUid.Invalid;

        tutorialMap.LoadedGrids.Add(grid.Value);
        tutorialMap.GridOffsets.Add(grid.Value, offset);

        return grid.Value;
    }

    private EntityUid GetSpawnPoint(EntityUid grid)
    {
        var query = EntityQueryEnumerator<TutorialSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawn, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            return uid;
        }

        return EntityUid.Invalid;
    }

    private void CleanupDeletedGrids(TutorialMapComponent comp)
    {
        if (comp.LoadedGrids.Count == 0)
            return;

        for (var i = comp.LoadedGrids.Count - 1; i >= 0; i--)
        {
            var grid = comp.LoadedGrids[i];

            if (Exists(grid))
                continue;

            comp.LoadedGrids.RemoveAt(i);
            comp.GridOffsets.Remove(grid);
        }
    }
}
