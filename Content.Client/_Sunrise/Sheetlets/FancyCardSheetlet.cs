using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class FancyCardSheetlet : Sheetlet<PalettedStylesheet>
{
    private static readonly ColorPalette CardPalette =
        ColorPalette.FromHexBase("#2d3e46", lightnessShift: 0.07f, chromaShift: 0.004f);

    private static readonly ColorPalette AccentPalette =
        ColorPalette.FromHexBase("#455563", lightnessShift: 0.07f, chromaShift: 0.004f);
    private static readonly ColorPalette CardTextPalette =
        ColorPalette.FromHexBase("#babfc2", lightnessShift: 0.07f, chromaShift: 0.004f);

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var frameBox = new StyleBoxFlat
        {
            BackgroundColor = CardPalette.Background
        };

        var contentBox = new StyleBoxFlat
        {
            BackgroundColor = CardPalette.BackgroundDark
        };

        var titleBarBox = new StyleBoxFlat
        {
            BackgroundColor = CardPalette.BackgroundLight,
            BorderColor = AccentPalette.Element,
            BorderThickness = new Thickness(2)
        };

        var quoteOuterBox = new StyleBoxFlat
        {
            BackgroundColor = AccentPalette.Background
        };

        var quoteInnerBox = new StyleBoxFlat
        {
            BackgroundColor = CardPalette.Background,
            BorderColor = AccentPalette.Background,
            BorderThickness = new Thickness(1)
        };

        var actionPanelBox = new StyleBoxFlat
        {
            BackgroundColor = CardPalette.BackgroundLight
        };

        var actionButtonBox = new StyleBoxFlat
        {
            BackgroundColor = Color.White,
            Padding = new Thickness(6, 2)
        };

        return
        [
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardFrame)
                .Panel(frameBox),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardContent)
                .Panel(contentBox),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardTitleBar)
                .Panel(titleBarBox),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardQuoteOuter)
                .Panel(quoteOuterBox),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardDescInner)
                .Panel(quoteInnerBox),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardActionPanel)
                .Panel(actionPanelBox),

            E<Label>()
                .Class(SunriseStyleClass.FancyCardTitleLabel)
                .FontColor(CardTextPalette.Text),
            E<Label>()
                .Class(SunriseStyleClass.FancyCardDescLabel)
                .FontColor(CardTextPalette.Text),

            E<Button>()
                .Class(SunriseStyleClass.FancyCardActionButton)
                .Box(actionButtonBox)
                .FontColor(AccentPalette.Text),
            E<Button>()
                .Class(SunriseStyleClass.FancyCardActionButton)
                .ParentOf(E<Label>())
                .FontColor(CardTextPalette.Text),
            E<Button>()
                .Class(SunriseStyleClass.FancyCardActionButton)
                .PseudoNormal()
                .Modulate(AccentPalette.Element),
            E<Button>()
                .Class(SunriseStyleClass.FancyCardActionButton)
                .PseudoHovered()
                .Modulate(AccentPalette.HoveredElement),
            E<Button>()
                .Class(SunriseStyleClass.FancyCardActionButton)
                .PseudoPressed()
                .Modulate(AccentPalette.PressedElement),
            E<Button>()
                .Class(SunriseStyleClass.FancyCardActionButton)
                .PseudoDisabled()
                .Modulate(CardPalette.DisabledElement),
        ];
    }
}
