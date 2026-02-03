using System.Linq;
using Content.Shared._Sunrise.MarkingEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Represents a marking ID and its colors.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct Marking
{
    /// <summary>
    /// The <see cref="MarkingPrototype"/> referred to by this marking.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> MarkingId;

    [DataField("markingColor")]
    private List<Color> _markingColors;

    // Sunrise gradient edit start
    [DataField("markingEffects")]
    public List<MarkingEffect> MarkingEffects = new();
    // Sunrise gradient edit end

    /// <summary>
    /// The colors taken on by the marking.
    /// </summary>
    public IReadOnlyList<Color> MarkingColors => _markingColors;

    /// <summary>
    /// Whether the marking is forced regardless of points.
    /// </summary>
    public bool Forced;

    [DataField]
    [ViewVariables]
    public bool Visible = true;

    public Marking()
    {
        _markingColors = new();
    }

    public Marking(ProtoId<MarkingPrototype> markingId,
        IEnumerable<Color> colors,
        IEnumerable<MarkingEffect>? markingEffects = null)
    {
        MarkingId = markingId;
        _markingColors = colors.ToList();
        MarkingEffects = CloneEffects(markingEffects);
    }

    public Marking(ProtoId<MarkingPrototype> markingId, int colorsCount) : this(
        markingId,
        Enumerable.Repeat(Color.White, colorsCount),
        Enumerable.Repeat<MarkingEffect>(ColorMarkingEffect.White, colorsCount))
    {
    }

    public Marking(Marking other) : this(other.MarkingId, other.MarkingColors, other.MarkingEffects)
    {
        Visible = other.Visible;
        Forced = other.Forced;
    }

    public bool Equals(Marking other)
    {
        return MarkingId.Equals(other.MarkingId)
            && _markingColors.SequenceEqual(other._markingColors)
            && MarkingEffects.Select(effect => effect.ToString()).SequenceEqual(other.MarkingEffects.Select(effect => effect.ToString()))
            && Visible.Equals(other.Visible)
            && Forced.Equals(other.Forced);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MarkingId, MarkingColors, MarkingEffects, Visible, Forced);
    }

    public Marking WithColor(Color color) =>
        this with
        {
            _markingColors = Enumerable.Repeat(color, MarkingColors.Count).ToList(),
            MarkingEffects = CloneEffects(MarkingEffects),
        };

    public Marking WithColorAt(int index, Color color)
    {
        var newColors = _markingColors.ShallowClone();
        newColors[index] = color;
        return this with
        {
            _markingColors = newColors,
            MarkingEffects = CloneEffects(MarkingEffects),
        };
    }

    public void SetColor(int colorIndex, Color color) =>
        _markingColors[colorIndex] = color;

    public void SetColor(Color color)
    {
        for (var i = 0; i < _markingColors.Count; i++)
        {
            _markingColors[i] = color;
        }
    }

    public void SetMarkingEffect(int colorIndex, MarkingEffect effect)
    {
        if (MarkingEffects.Count > colorIndex && colorIndex >= 0)
            MarkingEffects[colorIndex] = effect.Clone();
    }

    public void SetMarkingEffect(MarkingEffect effect)
    {
        for (var i = 0; i < MarkingEffects.Count; i++)
        {
            MarkingEffects[i] = effect.Clone();
        }
    }

    public override string ToString()
    {
        return ToLegacyDbString();
    }

    public string ToLegacyDbString()
    {
        var sanitizedName = MarkingId.Id.Replace('@', '_');
        var colorString = string.Join(',', _markingColors.Select(c => c.ToHex()));

        if (MarkingEffects.Count == 0)
            return $"{sanitizedName}@{colorString}";

        var effectString = string.Join(";", MarkingEffects.Select(effect => effect.ToString()));
        return $"{sanitizedName}@{colorString}@{effectString}";
    }

    public static Marking? ParseFromDbString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var split = input.Split('@');
        if (split.Length < 2)
            return null;

        var colorList = new List<Color>();
        foreach (var color in split[1].Split(','))
        {
            colorList.Add(Color.FromHex(color));
        }

        if (split.Length == 2)
            return new Marking(split[0], colorList);

        var markingEffects = new List<MarkingEffect>();
        foreach (var effectString in split[2].Split(';'))
        {
            var parsed = MarkingEffect.Parse(effectString);
            if (parsed != null)
                markingEffects.Add(parsed);
        }

        return new Marking(split[0], colorList, markingEffects);
    }

    private static List<MarkingEffect> CloneEffects(IEnumerable<MarkingEffect>? effects)
    {
        return effects?.Select(effect => effect.Clone()).ToList() ?? new();
    }
}
