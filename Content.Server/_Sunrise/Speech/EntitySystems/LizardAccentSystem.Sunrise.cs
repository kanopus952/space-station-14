using System.Text.RegularExpressions;
using Robust.Shared.Random;

#pragma warning disable IDE0130
namespace Content.Server.Speech.EntitySystems;

public sealed partial class LizardAccentSystem
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Regex LowerEsRegex = new("с+");
    private static readonly Regex UpperEsRegex = new("С+");
    private static readonly Regex LowerZeRegex = new("з+");
    private static readonly Regex UpperZeRegex = new("З+");
    private static readonly Regex LowerShaRegex = new("ш+");
    private static readonly Regex UpperShaRegex = new("Ш+");
    private static readonly Regex LowerCheRegex = new("ч+");
    private static readonly Regex UpperCheRegex = new("Ч+");

    private string AccentuateSunrise(string message)
    {
        message = LowerEsRegex.Replace(message, Pick("сс", "ссс"));
        message = UpperEsRegex.Replace(message, Pick("СС", "ССС"));
        message = LowerZeRegex.Replace(message, Pick("сс", "ссс"));
        message = UpperZeRegex.Replace(message, Pick("СС", "ССС"));
        message = LowerShaRegex.Replace(message, Pick("шш", "шшш"));
        message = UpperShaRegex.Replace(message, Pick("ШШ", "ШШШ"));
        message = LowerCheRegex.Replace(message, Pick("щщ", "щщщ"));
        return UpperCheRegex.Replace(message, Pick("ЩЩ", "ЩЩЩ"));
    }

    private string Pick(string first, string second) => _random.Prob(0.5f) ? first : second;
}
