using System.Text.RegularExpressions;
using Robust.Shared.Random;

#pragma warning disable IDE0130
namespace Content.Server.Speech.EntitySystems;

public sealed partial class FrontalLispSystem
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Regex LowerEsRegex = new("с");
    private static readonly Regex UpperEsRegex = new("С");
    private static readonly Regex LowerCheRegex = new("ч");
    private static readonly Regex UpperCheRegex = new("Ч");
    private static readonly Regex LowerTseRegex = new("ц");
    private static readonly Regex UpperTseRegex = new("Ц");
    private static readonly Regex LowerTeRegex = new(@"\B[т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])");
    private static readonly Regex UpperTeRegex = new(@"\B[Т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])");
    private static readonly Regex LowerZeRegex = new("з");
    private static readonly Regex UpperZeRegex = new("З");

    private string AccentuateSunrise(string message)
    {
        message = LowerEsRegex.Replace(message, _random.Prob(0.9f) ? "ш" : "с");
        message = UpperEsRegex.Replace(message, _random.Prob(0.9f) ? "Ш" : "С");
        message = LowerCheRegex.Replace(message, _random.Prob(0.9f) ? "ш" : "ч");
        message = UpperCheRegex.Replace(message, _random.Prob(0.9f) ? "Ш" : "Ч");
        message = LowerTseRegex.Replace(message, _random.Prob(0.9f) ? "ч" : "ц");
        message = UpperTseRegex.Replace(message, _random.Prob(0.9f) ? "Ч" : "Ц");
        message = LowerTeRegex.Replace(message, _random.Prob(0.9f) ? "ч" : "т");
        message = UpperTeRegex.Replace(message, _random.Prob(0.9f) ? "Ч" : "Т");
        message = LowerZeRegex.Replace(message, _random.Prob(0.9f) ? "ж" : "з");
        return UpperZeRegex.Replace(message, _random.Prob(0.9f) ? "Ж" : "З");
    }
}
