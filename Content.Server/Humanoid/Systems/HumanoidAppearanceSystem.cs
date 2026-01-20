using System.Linq;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;

namespace Content.Server.Humanoid;

public sealed partial class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void LoadProfile(EntityUid uid, HumanoidCharacterProfile? profile, HumanoidAppearanceComponent? humanoid = null)
    {
        base.LoadProfile(uid, profile, humanoid);

        if (profile != null)
            _visualBody.ApplyProfileTo(uid, profile);
    }

    public override void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, bool verify = true, HumanoidAppearanceComponent? humanoid = null)
    {
        base.SetSkinColor(uid, skinColor, sync, verify, humanoid);

        if (!Resolve(uid, ref humanoid))
            return;

        _visualBody.ApplyProfile(uid, new OrganProfileData
        {
            Sex = humanoid.Sex,
            EyeColor = humanoid.EyeColor,
            SkinColor = humanoid.SkinColor,
        });
        SyncVisualBodyMarkings(uid, humanoid);
    }

    public new void AddMarking(EntityUid uid,
        string marking,
        Color? color = null,
        bool sync = true,
        bool forced = false,
        HumanoidAppearanceComponent? humanoid = null,
        MarkingEffect? markingEffect = null)
    {
        base.AddMarking(uid, marking, color, sync, forced, humanoid, markingEffect);

        if (Resolve(uid, ref humanoid))
            SyncVisualBodyMarkings(uid, humanoid);
    }

    public new void AddMarking(EntityUid uid,
        string marking,
        IReadOnlyList<Color> colors,
        bool sync = true,
        bool forced = false,
        HumanoidAppearanceComponent? humanoid = null)
    {
        base.AddMarking(uid, marking, colors, sync, forced, humanoid);

        if (Resolve(uid, ref humanoid))
            SyncVisualBodyMarkings(uid, humanoid);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by ID.
    /// </summary>
    public void RemoveMarking(EntityUid uid, string marking, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.MarkingSet.Remove(prototype.MarkingCategory, marking);

        if (sync)
            Dirty(uid, humanoid);

        SyncVisualBodyMarkings(uid, humanoid);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by category and index.
    /// </summary>
    public void RemoveMarking(EntityUid uid, MarkingCategories category, int index, HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        humanoid.MarkingSet.Remove(category, index);
        Dirty(uid, humanoid);
        SyncVisualBodyMarkings(uid, humanoid);
    }

    /// <summary>
    ///     Sets the marking ID of the humanoid in a category at an index in the category's list.
    /// </summary>
    public void SetMarkingId(EntityUid uid, MarkingCategories category, int index, string markingId, HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !_markingManager.MarkingsByCategory(category).TryGetValue(markingId, out var markingPrototype)
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        var marking = markingPrototype.AsMarking();
        for (var i = 0; i < marking.MarkingColors.Count && i < markings[index].MarkingColors.Count; i++)
        {
            marking.SetColor(i, markings[index].MarkingColors[i]);
        }

        humanoid.MarkingSet.Replace(category, index, marking);
        Dirty(uid, humanoid);
        SyncVisualBodyMarkings(uid, humanoid);
    }

    /// <summary>
    ///     Sets the marking colors of the humanoid in a category at an index in the category's list.
    /// </summary>
    public void SetMarkingColor(EntityUid uid, MarkingCategories category, int index, List<Color> colors,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        for (var i = 0; i < markings[index].MarkingColors.Count && i < colors.Count; i++)
        {
            markings[index].SetColor(i, colors[i]);
        }

        Dirty(uid, humanoid);
        SyncVisualBodyMarkings(uid, humanoid);
    }

    private void SyncVisualBodyMarkings(EntityUid uid, HumanoidAppearanceComponent humanoid)
    {
        if (!HasComp<VisualBodyComponent>(uid))
            return;

        var markings = _markingManager.ConvertMarkings(humanoid.MarkingSet.GetForwardEnumerator().ToList(), humanoid.Species);
        _visualBody.ApplyMarkings(uid, markings);
    }
}
