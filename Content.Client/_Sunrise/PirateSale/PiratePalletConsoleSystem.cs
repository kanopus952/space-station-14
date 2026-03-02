using Content.Client._Sunrise.PirateSale.BUI;
using Content.Shared._Sunrise.PirateSale;
using Content.Shared._Sunrise.PirateSale.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.PirateSale;

public sealed class PiratePalletConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PiratePalletConsoleComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<PiratePalletConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<PiratePalletConsoleBoundUserInterface>(ent.Owner, PiratePalletConsoleUiKey.Sale, out var bui))
            bui.Update();
    }
}
