using System.Text.RegularExpressions;
using Robust.Shared.Random;

#pragma warning disable IDE0130
namespace Content.Server.Speech.EntitySystems;

public sealed partial class MothAccentSystem
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Regex LowerZheRegex = new("ж+");
    private static readonly Regex UpperZheRegex = new("Ж+");
    private static readonly Regex LowerZeRegex = new("з+");
    private static readonly Regex UpperZeRegex = new("З+");

    private string AccentuateSunrise(string message)
    {
        message = LowerZheRegex.Replace(message, Pick("жж", "жжж"));
        message = UpperZheRegex.Replace(message, Pick("ЖЖ", "ЖЖЖ"));
        message = LowerZeRegex.Replace(message, Pick("зз", "ззз"));
        return UpperZeRegex.Replace(message, Pick("ЗЗ", "ЗЗЗ"));
    }

    private string Pick(string first, string second) => _random.Prob(0.5f) ? first : second;
}
