using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using StardewValley;

namespace RealityCheck.Services;

public sealed class OffSeasonFactorService
{
    private const double MinimumOffSeasonFactor = 1.00;
    private const double MaximumOffSeasonFactor = 1.10;

    private static readonly HashSet<string> OffSeasonEligibleCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vegetable",
        "Fruit",
        "Flower",
        "Forage"
    };

    private static readonly Dictionary<string, string[]> ManualSeasonMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Spring fruit trees / forage.
        ["(O)634"] = new[] { "spring" }, // Apricot
        ["(O)638"] = new[] { "spring" }, // Cherry
        ["(O)16"] = new[] { "spring" },  // Wild Horseradish
        ["(O)18"] = new[] { "spring" },  // Daffodil
        ["(O)20"] = new[] { "spring" },  // Leek
        ["(O)22"] = new[] { "spring" },  // Dandelion

        // Summer fruit trees / forage.
        ["(O)91"] = new[] { "summer" },  // Banana
        ["(O)834"] = new[] { "summer" }, // Mango
        ["(O)635"] = new[] { "summer" }, // Orange
        ["(O)636"] = new[] { "summer" }, // Peach
        ["(O)259"] = new[] { "summer" }, // Fiddlehead Fern
        ["(O)396"] = new[] { "summer" }, // Spice Berry
        ["(O)398"] = new[] { "summer" }, // Grape
        ["(O)402"] = new[] { "summer" }, // Sweet Pea

        // Fall fruit trees / forage.
        ["(O)613"] = new[] { "fall" },   // Apple
        ["(O)637"] = new[] { "fall" },   // Pomegranate
        ["(O)404"] = new[] { "fall" },   // Common Mushroom
        ["(O)406"] = new[] { "fall" },   // Wild Plum
        ["(O)408"] = new[] { "fall" },   // Hazelnut
        ["(O)410"] = new[] { "fall" },   // Blackberry
        ["(O)281"] = new[] { "fall" },   // Chanterelle

        // Winter forage.
        ["(O)412"] = new[] { "winter" }, // Winter Root
        ["(O)414"] = new[] { "winter" }, // Crystal Fruit
        ["(O)416"] = new[] { "winter" }, // Snow Yam
        ["(O)418"] = new[] { "winter" }, // Crocus
        ["(O)283"] = new[] { "winter" }  // Holly
    };

    public double GetOffSeasonFactor(
        MarketCategoryResult category,
        Item? item,
        string marketCommodityKey
    )
    {
        if (item is null)
            return 1.00;

        if (!OffSeasonEligibleCategories.Contains(category.Category))
            return 1.00;

        HashSet<string> seasons = this.ResolveSeasons(item);

        if (seasons.Count == 0)
            return 1.00;

        string currentSeason = GetCurrentSeasonKey();

        if (seasons.Contains(currentSeason))
            return 1.00;

        return this.GetDeterministicOffSeasonFactor(marketCommodityKey);
    }

    private HashSet<string> ResolveSeasons(Item item)
    {
        HashSet<string> seasons = new(StringComparer.OrdinalIgnoreCase);

        this.AddSeasonsFromContextTags(item, seasons);
        this.AddSeasonsFromCropData(item, seasons);
        this.AddSeasonsFromManualMap(item, seasons);

        return seasons;
    }

    private void AddSeasonsFromContextTags(Item item, HashSet<string> seasons)
    {
        try
        {
            foreach (string tag in item.GetContextTags())
            {
                string normalized = tag.Trim().ToLowerInvariant();

                if (normalized.Contains("spring", StringComparison.OrdinalIgnoreCase))
                    seasons.Add("spring");
                if (normalized.Contains("summer", StringComparison.OrdinalIgnoreCase))
                    seasons.Add("summer");
                if (normalized.Contains("fall", StringComparison.OrdinalIgnoreCase)
                    || normalized.Contains("autumn", StringComparison.OrdinalIgnoreCase))
                    seasons.Add("fall");
                if (normalized.Contains("winter", StringComparison.OrdinalIgnoreCase))
                    seasons.Add("winter");
            }
        }
        catch
        {
            // Some item types may not expose context tags safely in every phase.
        }
    }

    private void AddSeasonsFromCropData(Item item, HashSet<string> seasons)
    {
        object? cropData = GetStaticMemberValue(typeof(Game1), "cropData");

        if (cropData is not IEnumerable enumerable)
            return;

        foreach (object? entry in enumerable)
        {
            if (entry is null)
                continue;

            object? value = GetMemberValue(entry, "Value") ?? entry;
            object? harvestItemId = GetMemberValue(value, "HarvestItemId")
                ?? GetMemberValue(value, "HarvestItem")
                ?? GetMemberValue(value, "HarvestItemID");

            if (harvestItemId is null)
                continue;

            if (!ItemIdsMatch(item.QualifiedItemId, harvestItemId.ToString()))
                continue;

            object? cropSeasons = GetMemberValue(value, "Seasons")
                ?? GetMemberValue(value, "seasons");

            AddSeasonValues(cropSeasons, seasons);
        }
    }

    private void AddSeasonsFromManualMap(Item item, HashSet<string> seasons)
    {
        string itemId = item.QualifiedItemId ?? string.Empty;

        if (!ManualSeasonMap.TryGetValue(itemId, out string[]? mappedSeasons))
            return;

        foreach (string season in mappedSeasons)
            seasons.Add(season);
    }

    private double GetDeterministicOffSeasonFactor(string marketCommodityKey)
    {
        if (string.IsNullOrWhiteSpace(marketCommodityKey))
            return 1.00;

        string seed = $"{Game1.uniqueIDForThisGame}|{Game1.year}|{Game1.currentSeason}|OffSeason|{marketCommodityKey}";
        ulong hash = ComputeStableHash(seed);
        double normalized = hash / (double)ulong.MaxValue;

        return MinimumOffSeasonFactor
            + normalized * (MaximumOffSeasonFactor - MinimumOffSeasonFactor);
    }

    private static void AddSeasonValues(object? source, HashSet<string> seasons)
    {
        if (source is null)
            return;

        if (source is string singleSeason)
        {
            AddSeasonValue(singleSeason, seasons);
            return;
        }

        if (source is IEnumerable enumerable)
        {
            foreach (object? value in enumerable)
                AddSeasonValue(value?.ToString(), seasons);

            return;
        }

        AddSeasonValue(source.ToString(), seasons);
    }

    private static void AddSeasonValue(string? value, HashSet<string> seasons)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Contains("spring", StringComparison.OrdinalIgnoreCase))
            seasons.Add("spring");
        else if (normalized.Contains("summer", StringComparison.OrdinalIgnoreCase))
            seasons.Add("summer");
        else if (normalized.Contains("fall", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("autumn", StringComparison.OrdinalIgnoreCase))
            seasons.Add("fall");
        else if (normalized.Contains("winter", StringComparison.OrdinalIgnoreCase))
            seasons.Add("winter");
    }

    private static bool ItemIdsMatch(string? left, string? right)
    {
        string normalizedLeft = NormalizeItemId(left);
        string normalizedRight = NormalizeItemId(right);

        return !string.IsNullOrWhiteSpace(normalizedLeft)
            && string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeItemId(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        string normalized = itemId.Trim();

        if (normalized.StartsWith("(O)", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[3..];

        return normalized;
    }

    private static object? GetStaticMemberValue(Type type, string name)
    {
        try
        {
            FieldInfo? field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            if (field is not null)
                return field.GetValue(null);
        }
        catch
        {
            // Ignore reflection failure and fall back.
        }

        try
        {
            PropertyInfo? property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            if (property is not null)
                return property.GetValue(null);
        }
        catch
        {
            // Ignore reflection failure and fall back.
        }

        return null;
    }

    private static object? GetMemberValue(object source, string name)
    {
        Type type = source.GetType();

        try
        {
            PropertyInfo? property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (property is not null)
                return property.GetValue(source);
        }
        catch
        {
            // Ignore and try field.
        }

        try
        {
            FieldInfo? field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field is not null)
                return field.GetValue(source);
        }
        catch
        {
            // Ignore and fall back.
        }

        return null;
    }

    private static string GetCurrentSeasonKey()
    {
        return Game1.currentSeason.ToString().Trim().ToLowerInvariant();
    }

    private static ulong ComputeStableHash(string text)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        unchecked
        {
            ulong hash = offsetBasis;

            foreach (byte value in Encoding.UTF8.GetBytes(text))
            {
                hash ^= value;
                hash *= prime;
            }

            return hash;
        }
    }
}
