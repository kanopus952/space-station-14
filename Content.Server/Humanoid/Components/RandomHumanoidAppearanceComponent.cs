namespace Content.Server.Humanoid.Components;

[RegisterComponent]
public sealed partial class RandomHumanoidAppearanceComponent : Component
{
    [DataField]
    public bool RandomizeName = true;

    /// <summary>
    /// After randomizing, sets the hair style to this, if possible.
    /// </summary>
    [DataField]
    public string? Hair;

    // Sunrise-Start
    /// <summary>
    /// Overrides randomized skin color.
    /// </summary>
    [DataField]
    public Color? SkinColor;

    /// <summary>
    /// Overrides randomized hair color.
    /// </summary>
    [DataField]
    public Color? HairColor;

    /// <summary>
    /// Overrides randomized facial hair color.
    /// </summary>
    [DataField]
    public Color? FacialHairColor;
    // Sunrise-End
}
