using System;
using System.Collections.Generic;

namespace Content.Client._Sunrise.Sponsor;

internal static class SponsorUiHelpers
{
    private const string Ellipsis = "...";

    public static string WrapText(string text, int maxLineLength, int maxLines)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var lines = new List<string>();
        var currentLine = string.Empty;

        foreach (var rawWord in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = rawWord.Trim();

            while (word.Length > maxLineLength)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = string.Empty;
                }

                var splitIndex = GetSplitIndex(word, maxLineLength);
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

            if (currentLine.Length + 1 + word.Length <= maxLineLength)
            {
                currentLine += " " + word;
                continue;
            }

            lines.Add(currentLine);
            currentLine = word;
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine);

        if (lines.Count <= maxLines)
            return string.Join('\n', lines);

        lines[maxLines - 1] = TruncateLineWithEllipsis(lines[maxLines - 1], maxLineLength);
        return string.Join('\n', lines.GetRange(0, maxLines));
    }

    public static List<string> WrapLines(string text, int maxLineLength)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return lines;

        var currentLine = string.Empty;
        foreach (var rawWord in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = rawWord.Trim();
            while (word.Length > maxLineLength)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = string.Empty;
                }

                var splitIndex = GetSplitIndex(word, maxLineLength);
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

            if (currentLine.Length + 1 + word.Length <= maxLineLength)
            {
                currentLine += " " + word;
                continue;
            }

            lines.Add(currentLine);
            currentLine = word;
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine);

        return lines;
    }

    public static string ToRomanNumeral(int value)
    {
        return value switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            6 => "VI",
            7 => "VII",
            8 => "VIII",
            9 => "IX",
            10 => "X",
            _ => value.ToString(),
        };
    }

    private static int GetSplitIndex(string word, int maxLineLength)
    {
        var hyphenIndex = word.LastIndexOf('-', maxLineLength - 1, maxLineLength);
        if (hyphenIndex > 0)
            return hyphenIndex + 1;

        return maxLineLength;
    }

    private static string TruncateLineWithEllipsis(string line, int maxLineLength)
    {
        if (line.Length + Ellipsis.Length <= maxLineLength)
            return line + Ellipsis;

        return line[..(maxLineLength - Ellipsis.Length)] + Ellipsis;
    }
}
