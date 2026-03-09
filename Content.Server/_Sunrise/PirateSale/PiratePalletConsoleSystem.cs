using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared._Sunrise.Economy;
using Content.Shared._Sunrise.PirateSale;
using Content.Shared._Sunrise.PirateSale.Components;
using Content.Shared._Sunrise.PirateSale.Events;
using Content.Shared.Access.Systems;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.PirateSale;

public sealed class PiratePalletConsoleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly HashSet<EntityUid> _lookupSet = [];
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<MobStateComponent> _mobQuery;

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<PiratePalletConsoleComponent, BoundUIOpenedEvent>(OnUiOpen);
        SubscribeLocalEvent<PiratePalletConsoleComponent, PiratePalletAppraiseMessage>(OnAppraise);
        SubscribeLocalEvent<PiratePalletConsoleComponent, PiratePalletSellMessage>(OnSell);
    }

    private void OnUiOpen(Entity<PiratePalletConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateInterface(ent);
    }

    private void OnAppraise(Entity<PiratePalletConsoleComponent> ent, ref PiratePalletAppraiseMessage args)
    {
        UpdateInterface(ent);
    }

    private void OnSell(Entity<PiratePalletConsoleComponent> ent, ref PiratePalletSellMessage args)
    {
        if (!_access.IsAllowed(args.Actor, ent.Owner))
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-order-not-allowed"), args.Actor);
            PlayDenySound(ent);
            return;
        }

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid)
        {
            SetDisabledState(ent);
            return;
        }

        GetPalletGoods(gridUid, out var toSell, out var rawCreditsValue);

        if (toSell.Count == 0)
        {
            UpdateInterface(ent);
            return;
        }

        foreach (var sold in toSell)
        {
            Del(sold);
        }

        var appraisalCredits = (int)Math.Round(rawCreditsValue);
        var coefficient = Math.Max(1, ent.Comp.CreditsPerDoubloon);
        var totalCredits = appraisalCredits + ent.Comp.CreditRemainder;

        var doubloons = totalCredits / coefficient;
        ent.Comp.CreditRemainder = totalCredits % coefficient;

        if (doubloons > 0)
        {
            _stack.SpawnMultipleAtPosition(ent.Comp.DoubloonStack, doubloons, xform.Coordinates);
        }

        _audio.PlayPvs(ent.Comp.ApproveSound, ent);
        UpdateInterface(ent);
    }

    private void UpdateInterface(Entity<PiratePalletConsoleComponent> ent)
    {
        if (Transform(ent).GridUid is not { } gridUid)
        {
            SetDisabledState(ent);
            return;
        }

        GetPalletGoods(gridUid, out var toSell, out var rawCreditsValue);

        var appraisalCredits = (int)Math.Round(rawCreditsValue);
        var coefficient = Math.Max(1, ent.Comp.CreditsPerDoubloon);
        var estimatedDoubloons = (appraisalCredits + ent.Comp.CreditRemainder) / coefficient;

        SetUiState(ent, appraisalCredits, estimatedDoubloons, toSell.Count, true);
    }

    private void SetDisabledState(Entity<PiratePalletConsoleComponent> ent)
    {
        SetUiState(ent, 0, 0, 0, false);
    }

    private void SetUiState(Entity<PiratePalletConsoleComponent> ent, int appraisalCredits, int doubloons, int count, bool enabled)
    {
        if (ent.Comp.UiAppraisalCredits == appraisalCredits &&
            ent.Comp.UiDoubloons == doubloons &&
            ent.Comp.UiCount == count &&
            ent.Comp.UiEnabled == enabled)
        {
            return;
        }

        ent.Comp.UiAppraisalCredits = appraisalCredits;
        ent.Comp.UiDoubloons = doubloons;
        ent.Comp.UiCount = count;
        ent.Comp.UiEnabled = enabled;
        Dirty(ent);
    }

    private void PlayDenySound(Entity<PiratePalletConsoleComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.NextDenySoundTime)
            return;

        ent.Comp.NextDenySoundTime = _timing.CurTime + ent.Comp.DenySoundDelay;
        _audio.PlayPvs(_audio.ResolveSound(ent.Comp.ErrorSound), ent);
    }

    private void GetPalletGoods(EntityUid gridUid, out HashSet<EntityUid> toSell, out double totalCredits)
    {
        toSell = [];
        totalCredits = 0;

        var query = EntityQueryEnumerator<PirateSellPalletComponent, TransformComponent>();
        while (query.MoveNext(out var palletUid, out _, out var palletXform))
        {
            if (palletXform.ParentUid != gridUid || !palletXform.Anchored)
                continue;

            _lookupSet.Clear();
            _lookup.GetEntitiesIntersecting(
                palletUid,
                _lookupSet,
                LookupFlags.Dynamic | LookupFlags.Sundries);

            foreach (var candidate in _lookupSet)
            {
                if (toSell.Contains(candidate) ||
                    !_xformQuery.TryGetComponent(candidate, out var xform) ||
                    !CanSell(candidate, xform))
                {
                    continue;
                }
                var price = _pricing.GetPriceIgnoreDontSell(candidate);

                if (price <= 0)
                    continue;

                toSell.Add(candidate);
                totalCredits += price;
            }
        }
    }

    private bool CanSell(EntityUid uid, TransformComponent xform)
    {
        if (xform.Anchored || _mobQuery.HasComponent(uid))
            return false;

        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!_xformQuery.TryGetComponent(child, out var childXform))
                continue;

            if (!CanSell(child, childXform))
                return false;
        }

        return true;
    }
}
