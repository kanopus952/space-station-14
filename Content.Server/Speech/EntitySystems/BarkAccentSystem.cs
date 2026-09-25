using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class BarkAccentSystem : RelayAccentSystem<BarkAccentComponent>
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly IReadOnlyList<string> Barks = new List<string>{
        " Гав!", " ГАВ", " вуф-вуф"  // Russian-Localization
    }.AsReadOnly();

    private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
    {
        { "ah", "arf" },
        { "Ah", "Arf" },
        { "oh", "oof" },
        { "Oh", "Oof" },
        // Russian-Localization-Start
        { "ага", "гаф" },
        { "Ага", "Гаф" },
        { "угу", "вуф" },
        { "Угу", "Вуф" },
        // Russian-Localization-End
    };

    public override string Accentuate(string message, Entity<BarkAccentComponent>? ent = null)
    {
        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        return message.Replace("!", _random.Pick(Barks))
            // Russian-Localization-Start
            .Replace("l", "r").Replace("L", "R")
            .Replace("л", "р").Replace("Л", "Р");
            // Russian-Localization-End
    }
}
