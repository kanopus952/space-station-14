using Content.Client._Sunrise.Tutorial;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class TutorialBubbleSheetlet : Sheetlet<PalettedStylesheet>
{
    private static readonly ColorPalette KeybindPalette =
        ColorPalette.FromHexBase("#636363", lightnessShift: 0.08f, chromaShift: 0.01f);

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var bubbleFrameTexture = ResCache.GetTexture("/Textures/_Sunrise/Interface/Tutorial/border.svg.96dpi.png");
        var bubbleFrameBox = new StyleBoxTexture
        {
            Texture = bubbleFrameTexture,
            Modulate = Color.OrangeRed
        };
        bubbleFrameBox.SetPatchMargin(StyleBox.Margin.All, 2);

        var bubbleFillColor = new Color(0f, 0f, 0f, 0.8f);
        var bubbleFillBox = new StyleBoxFlat
        {
            BackgroundColor = bubbleFillColor,
        };

        var keybindBox = new StyleBoxFlat
        {
            BackgroundColor = KeybindPalette.Background.WithAlpha(0.8f),
            BorderColor = Color.Goldenrod,
            BorderThickness = new Thickness(1),
        };

        return
        [
            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialBubbleFrame)
                .Panel(bubbleFrameBox),
            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialBubbleFill)
                .Panel(bubbleFillBox),

            E<TutorialBubbleTail>()
                .Class(SunriseStyleClass.TutorialBubbleTail)
                .Prop(TutorialBubbleTail.StylePropertyFillColor, Color.Goldenrod)
                .Prop(TutorialBubbleTail.StylePropertyBorderThickness, 0f),

            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialKeybindFrame)
                .Panel(keybindBox),

            E<Label>()
                .Class(SunriseStyleClass.TutorialBubbleText)
                .FontColor(Color.Goldenrod)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold)),

            E<RichTextLabel>()
                .Class(SunriseStyleClass.TutorialBubbleText)
                .FontColor(Color.White)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold))
        ];
    }
}
