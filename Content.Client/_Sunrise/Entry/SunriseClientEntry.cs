using Content.Shared._Sunrise.Localization;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client._Sunrise.Entry;

public sealed class SunriseClientEntry
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

        var input = IoCManager.Resolve<IInputManager>();
        KeybindLocalization.ResolveKeybind = functionName =>
        {
            var function = new BoundKeyFunction(functionName);
            return input.TryGetKeyBinding(function, out var binding)
                ? binding.GetKeyString()
                : null;
        };
    }

    public static void PostInit()
    {
    }
}
