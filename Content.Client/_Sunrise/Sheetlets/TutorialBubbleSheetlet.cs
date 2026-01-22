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
using YamlDotNet.Core.Tokens;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class TutorialBubbleSheetlet : Sheetlet<PalettedStylesheet>
{
    private static readonly ColorPalette KeybindPalette =
        ColorPalette.FromHexBase("#636363", lightnessShift: 0.08f, chromaShift: 0.01f);

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var frameColor = Color.DarkGoldenrod;
        var fillColor = new Color(0f, 0f, 0f, 0.8f);
        var keybindBorderColor = Color.Goldenrod;

        var bubbleFrameTexture = ResCache.GetTexture("/Textures/_Sunrise/Interface/Tutorial/border.svg.96dpi.png");
        var bubbleFrameBox = new StyleBoxTexture
        {
            Texture = bubbleFrameTexture,
            Modulate = frameColor,
        };
        bubbleFrameBox.SetPatchMargin(StyleBox.Margin.All, 2);

        var bubbleFillBox = new StyleBoxFlat
        {
            BackgroundColor = fillColor,
        };

        var keybindBox = new StyleBoxFlat
        {
            BackgroundColor = KeybindPalette.Background.WithAlpha(0.8f),
            BorderColor = keybindBorderColor,
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
                .Prop(TutorialBubbleTail.StylePropertyFillColor, frameColor)
                .Prop(TutorialBubbleTail.StylePropertyBorderThickness, 0f),

            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialKeybindFrame)
                .Panel(keybindBox)
                .Margin(new Thickness(5, 0)),

            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialKeybindFrame)
                .ParentOf(E<Label>().Class(StyleClass.LabelKeyText))
                .FontColor(keybindBorderColor)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold)),

            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialBubbleFill)
                .ParentOf(E<RichTextLabel>().Class(StyleClass.LabelKeyText))
                .FontColor(Color.White)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold))
        ];
    }
}
