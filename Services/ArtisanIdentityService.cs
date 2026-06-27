using System;
using System.Collections.Generic;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public sealed class ArtisanIdentityService
{
    public ArtisanIdentityService(IMonitor monitor)
    {
    }

    public ArtisanIdentity Resolve(Item item)
    {
        if (item is null)
            return CreateFallbackIdentity(string.Empty);

        string itemId = item.QualifiedItemId ?? string.Empty;
        string fallbackKey = itemId;

        string? preserveType = this.TryReadPreserveType(item);
        int? parentIndex = this.TryReadPreservedParentSheetIndex(item)
            ?? this.TryReadPreservedParentSheetIndexFromContextTags(item);

        if (string.IsNullOrWhiteSpace(preserveType) || parentIndex is null || parentIndex.Value < 0)
            return CreateFallbackIdentity(fallbackKey);

        string parentItemId = $"(O){parentIndex.Value}";

        return new ArtisanIdentity(
            MarketCommodityKey: $"Artisan:{preserveType}:{parentItemId}",
            ParentItemId: parentItemId
        );
    }

    public static ArtisanIdentity CreateFallbackIdentity(string itemId)
    {
        return new ArtisanIdentity(
            MarketCommodityKey: itemId ?? string.Empty,
            ParentItemId: string.Empty
        );
    }

    private string? TryReadPreserveType(Item item)
    {
        object? value = TryReadMemberValue(item, "preserve");
        string? text = Normalize(value?.ToString());

        if (!string.IsNullOrWhiteSpace(text))
            return text;

        return this.TryInferPreserveTypeFromContextTags(item);
    }

    private int? TryReadPreservedParentSheetIndex(Item item)
    {
        object? value = TryReadMemberValue(item, "preservedParentSheetIndex");
        return TryConvertToInt(value);
    }

    private int? TryReadPreservedParentSheetIndexFromContextTags(Item item)
    {
        foreach (string tag in SafeGetContextTags(item))
        {
            const string prefix = "preserve_sheet_index_";

            if (!tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(tag[prefix.Length..], out int parsed))
                return parsed;
        }

        return null;
    }

    private string? TryInferPreserveTypeFromContextTags(Item item)
    {
        HashSet<string> tags = new(
            SafeGetContextTags(item),
            StringComparer.OrdinalIgnoreCase
        );

        if (tags.Contains("item_wine") || tags.Contains("wine_item"))
            return "Wine";

        if (tags.Contains("item_jelly") || tags.Contains("jelly_item"))
            return "Jelly";

        if (tags.Contains("item_pickles") || tags.Contains("pickle_item"))
            return "Pickle";

        if (tags.Contains("item_honey") || tags.Contains("honey_item"))
            return "Honey";

        if (tags.Contains("item_aged_roe"))
            return "AgedRoe";

        if (tags.Contains("item_smoked"))
            return "SmokedFish";

        if (tags.Contains("item_dried"))
        {
            if (string.Equals(item.QualifiedItemId, "(O)DriedMushrooms", StringComparison.OrdinalIgnoreCase))
                return "DriedMushroom";

            return "DriedFruit";
        }

        return null;
    }

    private static IEnumerable<string> SafeGetContextTags(Item item)
    {
        try
        {
            return item.GetContextTags();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static object? TryReadMemberValue(Item item, string memberName)
    {
        Type? type = item.GetType();

        while (type is not null)
        {
            FieldInfo? field = type.GetField(
                memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field is not null)
                return field.GetValue(item);

            PropertyInfo? property = type.GetProperty(
                memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                try
                {
                    return property.GetValue(item);
                }
                catch
                {
                    return null;
                }
            }

            type = type.BaseType;
        }

        return null;
    }

    private static int? TryConvertToInt(object? value)
    {
        if (value is null)
            return null;

        if (value is int intValue)
            return intValue;

        if (value is long longValue)
            return longValue > int.MaxValue || longValue < int.MinValue
                ? null
                : (int)longValue;

        string? text = Normalize(value.ToString());

        if (int.TryParse(text, out int parsed))
            return parsed;

        return null;
    }

    private static string? Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string trimmed = text.Trim();

        if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.Equals(trimmed, "-1", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        return trimmed;
    }
}

public readonly record struct ArtisanIdentity(
    string MarketCommodityKey,
    string ParentItemId
);
