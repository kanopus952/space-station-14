using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// System that gives the speaker a faux-French accent.
/// </summary>
public sealed partial class FrenchAccentSystem : RelayAccentSystem<FrenchAccentComponent>
{
    [Dependency] private ReplacementAccentSystem _replacement = default!;

private static readonly Regex RegexKR = new(@"[кКрР]", RegexOptions.Compiled | RegexOptions.NonBacktracking);
    private static readonly Regex RegexSpacePunctuation = new(@"(?<=\w\w)[!?;:](?!\w)", RegexOptions.IgnoreCase);

    public override string Accentuate(string message, Entity<FrenchAccentComponent>? ent = null)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "french");
// Sunrise-Edit Start
        // replaces 'к/К' with 'кх/КХ' and 'р/Р' with 'х/Х' globally (preserves case) in a single pass.
        msg = RegexKR.Replace(msg, static m => m.Value[0] switch
        {
            'К' => "КХ",
            'к' => "кх",
            'Р' => "Х",
            'р' => "х",
            _ => m.Value
        });
// Sunrise -Edit End
        // spaces out ! ? : and ;.
        msg = RegexSpacePunctuation.Replace(msg, " $&");

        return msg;
    }
}
