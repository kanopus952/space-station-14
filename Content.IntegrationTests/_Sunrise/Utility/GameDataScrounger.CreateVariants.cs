#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Utility;

public static partial class GameDataScrounger
{
    private const string CreateVariantsTag = "!type:CreateVariants";

    private static List<string> GetPrototypeIds(YamlNode idNode, string file)
    {
        if (idNode is YamlScalarNode scalar)
            return [scalar.AsString()];

        if (idNode is not YamlMappingNode mapping || !HasTag(mapping, CreateVariantsTag) ||
            !mapping.TryGetNode("values", out YamlSequenceNode? values))
        {
            throw new InvalidDataException($"Unsupported prototype id in {file}: {idNode}");
        }

        return values.Children.Select(node => node.AsString()).ToList();
    }

    private static List<string> GetVariantParents(
        YamlMappingNode entry,
        int variantIndex,
        int variantCount,
        string file)
    {
        if (!entry.TryGetNode("parent", out var parentNode))
            return [];

        if (parentNode is YamlScalarNode scalar)
            return [scalar.AsString()];

        if (parentNode is YamlSequenceNode sequence)
            return sequence.Children.Select(node => node.AsString()).ToList();

        if (parentNode is not YamlMappingNode mapping || !HasTag(mapping, CreateVariantsTag))
            throw new InvalidDataException($"Unsupported prototype parent in {file}: {parentNode}");

        if (mapping.TryGetNode("values", out YamlSequenceNode? values))
        {
            ValidateVariantCount(values, variantCount, file, "parent values");
            return [values.Children[variantIndex].AsString()];
        }

        if (mapping.TryGetNode("sequences", out YamlSequenceNode? sequences))
        {
            ValidateVariantCount(sequences, variantCount, file, "parent sequences");

            if (sequences.Children[variantIndex] is not YamlSequenceNode parents)
                throw new InvalidDataException($"Expected a parent sequence in {file}: {sequences.Children[variantIndex]}");

            return parents.Children.Select(node => node.AsString()).ToList();
        }

        throw new InvalidDataException($"CreateVariants parent in {file} has neither values nor sequences.");
    }

    private static void ValidateVariantCount(YamlSequenceNode variants, int expected, string file, string field)
    {
        if (variants.Children.Count != expected)
        {
            throw new InvalidDataException(
                $"CreateVariants {field} in {file} has {variants.Children.Count} entries, expected {expected}.");
        }
    }

    private static bool HasTag(YamlNode node, string tag)
    {
        return !node.Tag.IsEmpty && node.Tag.Value.Equals(tag, StringComparison.OrdinalIgnoreCase);
    }
}
