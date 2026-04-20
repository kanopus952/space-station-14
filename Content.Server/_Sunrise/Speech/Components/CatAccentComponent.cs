namespace Content.Server._Sunrise.Speech.Components;

/// <summary>
/// Makes this entity add a mew or purr sounds at the end or the beginning of the sentence.
/// </summary>
[RegisterComponent]
public sealed partial class CatAccentComponent : Component
{
    /// <summary>
    /// Chance that a message will have a mew sound added before the first character.
    /// </summary>
    [DataField]
    public float PrefixChance = 0.3f;

    /// <summary>
    /// Chance that a message will have a mew sound added after last word.
    /// </summary>
    [DataField]
    public float SuffixChance = 0.3f;

    public readonly string[] Mews = [
        "cat-accent-mew-1",
        "cat-accent-mew-2",
        "cat-accent-mew-3",
        "cat-accent-mew-4",
        "cat-accent-mew-5",
    ];

}
