using System.Linq;
using System.Text.RegularExpressions;
using Content.Server._Sunrise.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Speech.EntitySystems;

public sealed partial class AddWordsAccentSystem : EntitySystem
{
    private static readonly Regex RegexFirstWord = new(@"^(\S+)");
    private static readonly Regex RegexLastWord = new(@"(\S+)$");
    private static readonly Regex RegexLastPunctuation = new(@"([.!?]+$)(?!.*[.!?])|(?<![.!?])$");
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddWordsAccentComponent, AccentGetEvent>(OnAccentGet, after: [typeof(OwOAccentSystem)]);
    }

    private void OnAccentGet(Entity<AddWordsAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(ent, args.Message);
    }

    public string Accentuate(Entity<AddWordsAccentComponent> ent, string message)
    {
        if (_random.Prob(ent.Comp.PrefixChance))
        {
            var match = RegexFirstWord.Match(message);
            // More than 2 letters and all caps
            var firstWordAllCaps = match.Success &&
                                !match.Value.Any(char.IsLower) &&
                                match.Value.Length > 2;

            var prefix = Loc.GetString(_random.Pick(ent.Comp.Words));

            if (!firstWordAllCaps)
                message = message[0].ToString().ToLower() + message[1..];
            else
                prefix = prefix.ToUpper();

            message = prefix + ", " + message;
        }

        // Sanitize capital again, in case we substituted a word that should be capitalized
        message = message[0].ToString().ToUpper() + message[1..];

        if (_random.Prob(ent.Comp.SuffixChance))
        {
            var lastWordAllCaps = !RegexLastWord.Match(message).Value.Any(char.IsLower);

            var suffix = Loc.GetString(_random.Pick(ent.Comp.Words));
            suffix = ", " + suffix;

            if (lastWordAllCaps)
                suffix = suffix.ToUpper();
            message = RegexLastPunctuation.Replace(message, suffix);
        }

        return message;
    }
}
