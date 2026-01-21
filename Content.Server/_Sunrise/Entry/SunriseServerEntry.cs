using Content.Shared._Sunrise.Localization;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server._Sunrise.Entry;

public sealed class SunriseServerEntry
{
    public static void Init()
    {
        var loc = IoCManager.Resolve<ILocalizationManager>();
        foreach (var culture in loc.GetFoundCultures())
        {
            if (!loc.HasCulture(culture))
                continue;

            loc.AddFunction(culture, "KEYBIND", KeybindLocalization.FormatKeybind);
        }

        KeybindLocalization.ResolveKeybind = null;
    }

    public static void PostInit()
    {
    }
}
