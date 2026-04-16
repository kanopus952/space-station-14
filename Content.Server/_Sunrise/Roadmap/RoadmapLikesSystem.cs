using Content.Server.Database;
using Content.Shared._Sunrise.Roadmap;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server._Sunrise.Roadmap;

public sealed class RoadmapLikesSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestRoadmapLikesEvent>(OnRequestRoadmapLikes);
        SubscribeNetworkEvent<RequestRoadmapLikeEvent>(OnRequestRoadmapLike);
    }

    private async void OnRequestRoadmapLikes(RequestRoadmapLikesEvent msg, EntitySessionEventArgs args)
    {
        await SendRoadmapLikesStateAsync(args.SenderSession);
    }

    private async void OnRequestRoadmapLike(RequestRoadmapLikeEvent msg, EntitySessionEventArgs args)
    {
        await TryToggleRoadmapLikeAsync(args.SenderSession, msg.ItemId);
    }

    private async Task SendRoadmapLikesStateAsync(
        ICommonSession session,
        RoadmapVersionsPrototype? roadmap = null,
        List<string>? itemIds = null)
    {
        RoadmapVersionsPrototype? roadmapPrototype;
        if (roadmap == null)
        {
            if (!TryGetRoadmap(out roadmapPrototype))
                return;
        }
        else
        {
            roadmapPrototype = roadmap;
        }

        itemIds ??= CollectRoadmapItemIds(roadmapPrototype);
        var likes = await _db.GetRoadmapLikesAsync(session.UserId.UserId, roadmapPrototype.ID, itemIds);
        var state = new RoadmapLikesStateEvent(likes.Select(like => new RoadmapLikeState(like.ItemId, like.LikeCount, like.LikedByPlayer)).ToArray());
        RaiseNetworkEvent(state, session);
    }

    private async Task TryToggleRoadmapLikeAsync(ICommonSession session, string itemId)
    {
        if (!CanToggleRoadmapLike(itemId, out var roadmap, out var itemIds))
        {
            if (roadmap != null)
                await SendRoadmapLikesStateAsync(session, roadmap, itemIds);

            return;
        }

        await DoToggleRoadmapLikeAsync(session, roadmap, itemId, itemIds);
    }

    private bool CanToggleRoadmapLike(
        string itemId,
        [NotNullWhen(true)] out RoadmapVersionsPrototype? roadmap,
        out List<string> itemIds)
    {
        itemIds = [];
        if (string.IsNullOrWhiteSpace(itemId))
        {
            roadmap = null;
            return false;
        }

        if (!TryGetRoadmap(out roadmap))
            return false;

        itemIds = CollectRoadmapItemIds(roadmap);
        return itemIds.Contains(itemId);
    }

    private async Task DoToggleRoadmapLikeAsync(
        ICommonSession session,
        RoadmapVersionsPrototype roadmap,
        string itemId,
        List<string> itemIds)
    {
        await _db.ToggleRoadmapLikeAsync(session.UserId.UserId, roadmap.ID, itemId);
        await BroadcastRoadmapLikesStateAsync(roadmap, itemIds);
    }

    private async Task BroadcastRoadmapLikesStateAsync(RoadmapVersionsPrototype roadmap, List<string> itemIds)
    {
        foreach (var targetSession in _playerManager.NetworkedSessions)
        {
            await SendRoadmapLikesStateAsync(targetSession, roadmap, itemIds);
        }
    }

    private bool TryGetRoadmap([NotNullWhen(true)] out RoadmapVersionsPrototype? roadmap)
    {
        roadmap = null;
        var roadmapId = _cfg.GetCVar(SunriseCCVars.RoadmapId);
        if (string.IsNullOrWhiteSpace(roadmapId))
            return false;

        return _prototype.TryIndex(roadmapId, out roadmap);
    }

    private static List<string> CollectRoadmapItemIds(RoadmapVersionsPrototype roadmap)
    {
        var itemIds = new List<string>();
        var unique = new HashSet<string>();

        foreach (var version in roadmap.Versions)
        {
            foreach (var goal in version.Goals)
            {
                var itemId = goal.Id;
                if (string.IsNullOrWhiteSpace(itemId))
                    continue;

                if (!unique.Add(itemId))
                    continue;

                itemIds.Add(itemId);
            }
        }

        return itemIds;
    }
}
