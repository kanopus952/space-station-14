using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client._Sunrise.Sheetlets;
using Content.Client.Stylesheets;
using Content.Client._Sunrise.UserInterface.RichText;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.SponsorInventory;

/// <summary>
/// Palette card used for both regular loadouts and sponsor inventory items.
/// </summary>
internal sealed class InventoryPaletteItemControl : ContainerButton
{
    private const int ControlWidth = 96;
    private const int ControlHeight = 120;
    private const int SpriteSize = 72;
    private const int LabelWidth = 76;
    private const int LabelHeight = 36;
    private const int MaxLabelLineLength = 9;
    private const int MaxLabelLines = 2;
    private const int TooltipMaxWidth = 300;
    private const string Ellipsis = "...";

    private static readonly Type[] TooltipTags =
    [
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(HeadingTag),
        typeof(ItalicTag),
        typeof(SponsorInventoryStatusTag),
    ];


    /// <summary>
    /// Source entry represented by this card.
    /// </summary>
    public InventoryPaletteEntry Entry { get; }

    public InventoryPaletteItemControl(InventoryPaletteEntry entry, bool selected, FormattedMessage tooltip)
    {
        Entry = entry;
        Disabled = !entry.Enabled;
        SetWidth = ControlWidth;
        SetHeight = ControlHeight;
        MinSize = new Vector2(ControlWidth, ControlHeight);
        RectClipContent = true;
        AddStyleClass(ContainerButton.StyleClassButton);
        AddStyleClass(StyleClass.ButtonSquare);
        AddStyleClass(SunriseStyleClass.SponsorInventoryPaletteItem);

        if (selected)
        {
            AddStyleClass(StyleClass.Positive);
            AddStyleClass(SunriseStyleClass.SponsorInventoryPaletteItemSelected);
        }

        if (!entry.Enabled)
            AddStyleClass(SunriseStyleClass.SponsorInventoryPaletteItemUnavailable);

        var icon = new EntityPrototypeView
        {
            SetSize = new Vector2(SpriteSize, SpriteSize),
            Scale = new Vector2(3, 3),
            OverrideDirection = Direction.South,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };
        icon.SetPrototype(entry.IconPrototype);

        var label = new Label
        {
            Text = WrapLabelText(entry.Name),
            ClipText = true,
            SetWidth = LabelWidth,
            SetHeight = LabelHeight,
            MaxWidth = LabelWidth,
            MaxHeight = LabelHeight,
            Align = Label.AlignMode.Center,
            VAlign = Label.VAlignMode.Top,
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { StyleClass.LabelSubText },
        };

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Children =
            {
                new Control
                {
                    SetHeight = SpriteSize,
                    HorizontalExpand = true,
                    Children = { icon },
                },
                new Control
                {
                    RectClipContent = true,
                    SetWidth = LabelWidth,
                    SetHeight = LabelHeight,
                    MaxWidth = LabelWidth,
                    MaxHeight = LabelHeight,
                    HorizontalAlignment = HAlignment.Center,
                    Children = { label },
                },
            },
        });

        TooltipSupplier = _ =>
        {
            var result = new PanelContainer
            {
                MaxWidth = TooltipMaxWidth,
                StyleClasses = { StyleClass.TooltipPanel },
            };

            var label = new RichTextLabel();
            label.SetMessage(tooltip, TooltipTags);

            result.AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                RectClipContent = true,
                Children = { label },
            });

            return result;
        };
    }

    private static string WrapLabelText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var lines = new List<string>();
        var currentLine = string.Empty;

        foreach (var rawWord in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = rawWord.Trim();

            while (word.Length > MaxLabelLineLength)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = string.Empty;
                }

                var splitIndex = GetSplitIndex(word);
                lines.Add(word[..splitIndex]);
                word = word[splitIndex..];
            }

            if (word.Length == 0)
                continue;

            if (currentLine.Length == 0)
            {
                currentLine = word;
                continue;
            }

            if (currentLine.Length + 1 + word.Length <= MaxLabelLineLength)
            {
                currentLine += " " + word;
                continue;
            }

            lines.Add(currentLine);
            currentLine = word;
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine);

        if (lines.Count <= MaxLabelLines)
            return string.Join('\n', lines);

        lines[MaxLabelLines - 1] = TruncateLineWithEllipsis(lines[MaxLabelLines - 1]);
        return string.Concat(lines[0], "\n", lines[MaxLabelLines - 1]);
    }

    private static int GetSplitIndex(string word)
    {
        var hyphenIndex = word.LastIndexOf('-', MaxLabelLineLength - 1, MaxLabelLineLength);
        if (hyphenIndex > 0)
            return hyphenIndex + 1;

        return MaxLabelLineLength;
    }

    private static string TruncateLineWithEllipsis(string line)
    {
        if (line.Length + Ellipsis.Length <= MaxLabelLineLength)
            return line + Ellipsis;

        if (MaxLabelLineLength <= Ellipsis.Length)
            return Ellipsis[..MaxLabelLineLength];

        return line[..(MaxLabelLineLength - Ellipsis.Length)] + Ellipsis;
    }
}
