using Content.Client._Sunrise.PirateSale.UI;
using Content.Shared._Sunrise.PirateSale.Components;
using Content.Shared._Sunrise.PirateSale.Events;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.PirateSale.BUI;

public sealed class PiratePalletConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PiratePalletMenu? _menu;

    public PiratePalletConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<PiratePalletMenu>();
        _menu.AppraiseRequested += OnAppraise;
        _menu.SellRequested += OnSell;
        Update();
    }

    private void OnAppraise()
    {
        SendMessage(new PiratePalletAppraiseMessage());
    }

    private void OnSell()
    {
        SendMessage(new PiratePalletSellMessage());
    }

    public override void Update()
    {
        base.Update();

        if (_menu is null || !EntMan.TryGetComponent(Owner, out PiratePalletConsoleComponent? console))
            return;

        _menu.SetEnabled(console.UiEnabled);
        _menu.SetAppraisalCredits(console.UiAppraisalCredits);
        _menu.SetDoubloons(console.UiDoubloons);
        _menu.SetCount(console.UiCount);
    }
}
