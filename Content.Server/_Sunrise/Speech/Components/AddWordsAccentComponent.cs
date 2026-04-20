namespace Content.Server._Sunrise.Speech.Components;

/// <summary>
/// Makes this entity add a word at the end or the beginning of the sentence.
/// </summary>
[RegisterComponent]
public sealed partial class AddWordsAccentComponent : Component
{
    /// <summary>
    /// Chance that a message will have a word added before the first character.
    /// </summary>
    [DataField]
    public float PrefixChance = 0.3f;

    /// <summary>
    /// Chance that a message will have a word added after last word.
    /// </summary>
    [DataField]
    public float SuffixChance = 0.3f;

    [DataField]
    public string[] Words = [];

}
