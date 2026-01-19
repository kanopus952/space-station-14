using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Eui;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using JetBrains.Annotations;
using NetCord;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Tutorial;

/// <summary>
///     Admin Eui for spawning and preview-ing explosions
/// </summary>
[UsedImplicitly]
public sealed class TutorialWindowEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTutorialSystem _tutorial;
    private readonly SharedMindSystem _mind;
    private readonly MetaDataSystem _meta;
    private readonly MapLoaderSystem _mapLoader;
    private readonly SharedTransformSystem _transform;
    private readonly ISawmill _sawmill;
    private EntityUid? _tutorialMap;
    private readonly List<EntityUid> _loadedGrids = new();
    private readonly Dictionary<EntityUid, Vector2> _gridOffsets = new();
    private static readonly string MapName = "Tutorial Map (DO NOT TOUCH)";
    private static readonly Vector2 CoordinateStep = new Vector2(0, 200);
    private List<string> _completedTutorials = new();

    public TutorialWindowEui()
    {
        IoCManager.InjectDependencies(this);
        _mind = _entSys.GetEntitySystem<SharedMindSystem>();
        _mapSystem = _entSys.GetEntitySystem<SharedMapSystem>();
        _meta = _entSys.GetEntitySystem<MetaDataSystem>();
        _transform = _entSys.GetEntitySystem<SharedTransformSystem>();
        _mapLoader = _entSys.GetEntitySystem<MapLoaderSystem>();
        _tutorial = _entSys.GetEntitySystem<SharedTutorialSystem>();
        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("explosion");
    }

    public override void Opened()
    {
        base.Opened();
        _ = RefreshCompletedTutorials();
    }

    public override EuiStateBase GetNewState()
    {
        return new TutorialWindowEuiState(_completedTutorials);
    }

    private async Task RefreshCompletedTutorials()
    {
        _completedTutorials = await _db.GetTutorial(Player.UserId.UserId);
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is TutorialQuitButtonPressedMessage)
        {
            if (!_entity.TryGetComponent<TutorialPlayerComponent>(Player.AttachedEntity, out var comp))
                return;

            _tutorial.EndTutorial((Player.AttachedEntity.Value, comp));
        }

        if (msg is not TutorialButtonPressedEuiMessage request)
            return;

        if (request.Grid == null)
            return;

        TryCreateMap();
        var gridUid = LoadLocation(request.Grid.Value);
        var spawnPoint = GetSpawnPoint(gridUid);

        if (!_entity.TrySpawnNextTo(request.PlayerEntity, spawnPoint, out var uid))
            return;

        if (!_entity.TryGetComponent<TutorialPlayerComponent>(uid, out var tutorial))
            return;

        tutorial.Grid = gridUid;
        var (mindId, _) = _mind.CreateMind(Player.UserId);
        _mind.SetUserId(mindId, Player.UserId);
        _mind.TransferTo(mindId, uid);
    }

    private void TryCreateMap()
    {
        if (_entity.EntityExists(_tutorialMap))
            return;

        var mapUid = _mapSystem.CreateMap();
        _meta.SetEntityName(mapUid, MapName);
        _tutorialMap = mapUid;
    }
    private EntityUid LoadLocation(ResPath gridPath)
    {
        if (!_entity.TryGetComponent<MapComponent>(_tutorialMap, out var mapComp))
            return EntityUid.Invalid;

        CleanupDeletedGrids();

        Vector2 offset;

        if (_loadedGrids.Count == 0)
        {
            offset = Vector2.Zero;
        }
        else
        {
            var lastGrid = _loadedGrids[^1];
            offset = _gridOffsets[lastGrid] + CoordinateStep;
        }

        if (!_mapLoader.TryLoadGrid(mapComp.MapId, gridPath, out var grid, null, offset))
            return EntityUid.Invalid;

        _loadedGrids.Add(grid.Value);
        _gridOffsets.Add(grid.Value, offset);

        return grid.Value;
    }
    private EntityUid GetSpawnPoint(EntityUid grid)
    {
        var query = _entity.EntityQueryEnumerator<TutorialSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawn, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            return uid;
        }

        return EntityUid.Invalid;
    }

    private void CleanupDeletedGrids()
    {
        for (var i = _loadedGrids.Count - 1; i >= 0; i--)
        {
            var grid = _loadedGrids[i];
            if (!_entity.EntityExists(grid))
            {
                _loadedGrids.RemoveAt(i);
                _gridOffsets.Remove(grid);
            }
        }
    }
}
