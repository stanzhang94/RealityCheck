using System;
using System.Collections.Generic;
using StardewValley;

namespace RealityCheck.Services;

public sealed class OffSeasonFactorService
{
    private const double WinterOffSeasonFactor = 1.03;

    private static readonly HashSet<string> OffSeasonEligibleCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vegetable",
        "Fruit",
        "Flower",
        "Forage"
    };

    public double GetOffSeasonFactor(
        MarketCategoryResult category,
        Item? item,
        string marketCommodityKey
    )
    {
        // RealityCheck 1.3.3 balance tuning:
        // The old rule treated every non-production season as off-season, which meant
        // many crop/forage categories could receive a positive factor for most of the year.
        // In a recursive market-price model that was too strong.
        // New rule: eligible crop/forage categories get a small winter-only premium.
        if (item is null)
            return 1.00;

        if (!OffSeasonEligibleCategories.Contains(category.Category))
            return 1.00;

        return IsWinter()
            ? WinterOffSeasonFactor
            : 1.00;
    }

    private static bool IsWinter()
    {
        return string.Equals(
            Game1.currentSeason.ToString(),
            "winter",
            StringComparison.OrdinalIgnoreCase
        );
    }
}
