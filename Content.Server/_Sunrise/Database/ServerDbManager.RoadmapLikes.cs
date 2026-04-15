using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<List<UiLikeData>> GetUiLikesAsync(Guid player, string scopeId, IReadOnlyList<string> itemIds);
    Task<bool> ToggleUiLikeAsync(Guid player, string scopeId, string itemId);

    Task<List<RoadmapLikeData>> GetRoadmapLikesAsync(Guid player, string roadmapId, IReadOnlyList<string> itemIds);
    Task<bool> ToggleRoadmapLikeAsync(Guid player, string roadmapId, string itemId);
}

public sealed partial class ServerDbManager
{
    public Task<List<UiLikeData>> GetUiLikesAsync(Guid player, string scopeId, IReadOnlyList<string> itemIds)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetUiLikesAsync(player, scopeId, itemIds));
    }

    public Task<bool> ToggleUiLikeAsync(Guid player, string scopeId, string itemId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.ToggleUiLikeAsync(player, scopeId, itemId));
    }

    public async Task<List<RoadmapLikeData>> GetRoadmapLikesAsync(Guid player, string roadmapId, IReadOnlyList<string> itemIds)
    {
        if (string.IsNullOrWhiteSpace(roadmapId))
            return [];

        var likes = await GetUiLikesAsync(player, ToRoadmapLikesScope(roadmapId), itemIds);
        return likes.Select(like => new RoadmapLikeData(like.ItemId, like.LikeCount, like.LikedByPlayer)).ToList();
    }

    public Task<bool> ToggleRoadmapLikeAsync(Guid player, string roadmapId, string itemId)
    {
        if (string.IsNullOrWhiteSpace(roadmapId))
            return Task.FromResult(false);

        return ToggleUiLikeAsync(player, ToRoadmapLikesScope(roadmapId), itemId);
    }

    private static string ToRoadmapLikesScope(string roadmapId)
    {
        return $"roadmap:{roadmapId.Trim()}";
    }
}

public sealed record UiLikeData(string ItemId, int LikeCount, bool LikedByPlayer);
public sealed record RoadmapLikeData(string ItemId, int LikeCount, bool LikedByPlayer);
