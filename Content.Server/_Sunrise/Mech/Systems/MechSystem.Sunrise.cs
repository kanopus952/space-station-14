using Content.Server.Chat.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Tag;
using Content.Shared.Vehicle.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Mech.Systems;

public sealed partial class MechSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private static readonly ProtoId<TagPrototype> PowerCageTag = "PowerCage";

    private void InitializeSunrise()
    {
        SubscribeLocalEvent<MechComponent, MechSayEvent>(OnMechSay);
        SubscribeLocalEvent<VehicleOperatorComponent, OnVehicleExitedEvent>(OnSunriseVehicleExited);
    }

    private void OnMechSay(EntityUid uid, MechComponent component, MechSayEvent args)
    {
        _chat.TrySendInGameICMessage(uid,
            Loc.GetString(args.Message),
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal);
    }

    private bool TryPrepareSunriseEntry(Entity<MechComponent> ent, EntityUid user)
    {
        if (_whitelist.IsWhitelistPass(ent.Comp.PilotBlacklist, user))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", ent.Owner)), user);
            return false;
        }

        if (TryComp<AccessReaderComponent>(ent, out var accessReader) &&
            !_accessReader.IsAllowed(user, ent, accessReader))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-access", ("item", ent.Owner)), user);
            return false;
        }

        foreach (var hand in _hands.EnumerateHands(user))
        {
            _hands.DoDrop(user, hand);
        }

        _faction.Up(user, ent);
        return true;
    }

    private void OnSunriseVehicleExited(Entity<VehicleOperatorComponent> ent, ref OnVehicleExitedEvent args)
    {
        if (HasComp<MechComponent>(args.Vehicle))
            RemComp<NpcFactionMemberComponent>(args.Vehicle);
    }

    /// <summary>
    /// Переносит существующий порог критического состояния в новое поле прочности меха.
    /// </summary>
    private void SetSunriseMaxIntegrity(EntityUid uid, MechComponent component)
    {
        if (TryComp<MobThresholdsComponent>(uid, out var thresholds)
            && _mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var threshold, thresholds)
            && threshold is { } maxIntegrity)
        {
            component.MaxIntegrity = maxIntegrity;
        }
    }

    private void SaySunriseCritMessage(EntityUid uid,
        MechComponent component,
        FixedPoint2 totalDamage,
        bool damageIncreased)
    {
        if (!damageIncreased || component.MaxIntegrity <= 0)
            return;

        var damagePercentage = totalDamage / component.MaxIntegrity * 100;
        MechHealthState newState;

        if (damagePercentage >= 95)
            newState = MechHealthState.Critical;
        else if (damagePercentage >= 50)
            newState = MechHealthState.Damaged;
        else
            newState = MechHealthState.Healthy;

        if (newState == component.HealthState)
            return;

        component.HealthState = newState;
        Dirty(uid, component);

        var message = newState switch
        {
            MechHealthState.Critical => component.MessageAlert5,
            MechHealthState.Damaged => component.MessageAlert50,
            _ => string.Empty,
        };

        if (string.IsNullOrEmpty(message))
            return;

        var chatType = newState == MechHealthState.Critical
            ? InGameICChatType.Speak
            : InGameICChatType.Whisper;

        _chat.TrySendInGameICMessage(uid,
            Loc.GetString(message),
            chatType,
            ChatTransmitRange.Normal);
    }

}
