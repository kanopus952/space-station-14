using System.Numerics;
using Content.Shared._Sunrise.Tutorial.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Tutorial;

/// <summary>
/// Draws an animated dotted trail from the local player toward the current tutorial bubble target.
/// </summary>
public sealed class TutorialPathOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _player;
    private readonly IGameTiming _timing;
    private readonly SharedTransformSystem _transform;

    private const float DotRadius = 0.07f;
    private const float DotSpacing = 0.9f;
    private const float MinPlayerDist = 1.2f;
    private const float MinTargetDist = 0.9f;
    private const float AnimSpeed = 2.5f;

    private static readonly Color PathColor = Color.FromHex("#FFD84D");

    public TutorialPathOverlay(
        IEntityManager entManager,
        IPlayerManager player,
        IGameTiming timing,
        SharedTransformSystem transform)
    {
        _entManager = entManager;
        _player = player;
        _timing = timing;
        _transform = transform;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;

        if (!_entManager.TryGetComponent<TutorialPlayerComponent>(player, out var tutComp))
            return;

        if (tutComp.CurrentBubbleTarget is not { } targetUid || !_entManager.EntityExists(targetUid))
            return;

        if (!_entManager.TryGetComponent<TransformComponent>(player, out var playerXform))
            return;

        var playerMap = _transform.GetMapCoordinates(player.Value, xform: playerXform);
        if (playerMap.MapId == MapId.Nullspace || playerMap.MapId != args.MapId)
            return;

        if (!_entManager.TryGetComponent<TransformComponent>(targetUid, out var targetXform))
            return;

        var targetMap = _transform.GetMapCoordinates(targetUid, xform: targetXform);
        if (targetMap.MapId != playerMap.MapId)
            return;

        var from = playerMap.Position;
        var to = targetMap.Position;

        var delta = to - from;
        var totalDist = delta.Length();
        if (totalDist < MinPlayerDist + MinTargetDist)
            return;

        var dir = delta / totalDist;
        var time = (float)_timing.CurTime.TotalSeconds;
        var handle = args.WorldHandle;

        for (var dist = MinPlayerDist; dist <= totalDist - MinTargetDist; dist += DotSpacing)
        {
            var phase = dist / DotSpacing - time * AnimSpeed;
            var wave = (MathF.Sin(phase * MathF.PI) + 1f) * 0.5f;
            handle.DrawCircle(
                from + dir * dist,
                DotRadius * (0.6f + 0.4f * wave),
                PathColor.WithAlpha(0.25f + 0.75f * wave));
        }
    }
}
