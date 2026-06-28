using System;
using StardewValley;

namespace RealityCheck.Services;

public sealed class FestivalFactorService
{
    public double GetFestivalFactor(string marketCategory)
    {
        if (string.IsNullOrWhiteSpace(marketCategory))
            return 1.00;

        // Artisan goods are intentionally excluded from direct festival factors.
        // They will inherit relevant source-item effects through artisan transmission later.
        if (string.Equals(marketCategory, "ArtisanGoods", StringComparison.OrdinalIgnoreCase)
            || string.Equals(marketCategory, "FlavoredArtisan", StringComparison.OrdinalIgnoreCase))
            return 1.00;

        string season = GetCurrentSeasonKey();
        int day = Game1.dayOfMonth;

        return (season, day) switch
        {
            ("spring", 13) => this.GetSpring13Factor(marketCategory),
            ("spring", 24) => this.GetSpring24Factor(marketCategory),
            ("summer", 11) => this.GetSummer11Factor(marketCategory),
            ("summer", 28) => this.GetSummer28Factor(marketCategory),
            ("fall", 16) => this.GetFall16Factor(marketCategory),
            ("fall", 27) => this.GetFall27Factor(marketCategory),
            ("winter", 8) => this.GetWinter8Factor(marketCategory),
            ("winter", 15) => this.GetWinter15Factor(marketCategory),
            ("winter", 25) => this.GetWinter25Factor(marketCategory),
            _ => 1.00
        };
    }

    public string GetCurrentFestivalKey()
    {
        string season = GetCurrentSeasonKey();
        int day = Game1.dayOfMonth;

        return (season, day) switch
        {
            ("spring", 13) => "Spring13EggFestival",
            ("spring", 24) => "Spring24FlowerDance",
            ("summer", 11) => "Summer11Luau",
            ("summer", 28) => "Summer28DanceOfTheMoonlightJellies",
            ("fall", 16) => "Fall16StardewValleyFair",
            ("fall", 27) => "Fall27SpiritEve",
            ("winter", 8) => "Winter8FestivalOfIce",
            ("winter", 15) => "Winter15NightMarket",
            ("winter", 25) => "Winter25FeastOfTheWinterStar",
            _ => "None"
        };
    }

    private double GetSpring13Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Egg" => 1.05,
            "Milk" => 1.05,
            "Fish" => 0.95,
            _ => 1.00
        };
    }

    private double GetSpring24Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Flower" => 1.05,
            "Forage" => 1.05,
            "BuildingResource" => 1.05,
            _ => 1.00
        };
    }

    private double GetSummer11Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Fish" => 1.05,
            "Vegetable" => 1.05,
            "Gem" => 0.95,
            "Egg" => 0.95,
            "Milk" => 0.95,
            _ => 1.00
        };
    }

    private double GetSummer28Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Fish" => 1.05,
            "Flower" => 0.95,
            "Forage" => 0.95,
            _ => 1.00
        };
    }

    private double GetFall16Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Vegetable" => 1.05,
            "Fruit" => 1.05,
            "BuildingResource" => 0.95,
            "MetalResource" => 0.95,
            _ => 1.00
        };
    }

    private double GetFall27Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Flower" => 1.05,
            "Forage" => 1.05,
            "Fish" => 0.95,
            _ => 1.00
        };
    }

    private double GetWinter8Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Fish" => 1.05,
            "Flower" => 0.95,
            "Forage" => 0.95,
            _ => 1.00
        };
    }

    private double GetWinter15Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "MetalResource" => 1.05,
            "BuildingResource" => 1.05,
            "Vegetable" => 0.95,
            _ => 1.00
        };
    }

    private double GetWinter25Factor(string marketCategory)
    {
        return marketCategory switch
        {
            "Gem" => 1.05,
            "BuildingResource" => 0.95,
            "Fruit" => 0.95,
            _ => 1.00
        };
    }

    private static string GetCurrentSeasonKey()
    {
        return Game1.currentSeason.ToString().Trim().ToLowerInvariant();
    }
}
