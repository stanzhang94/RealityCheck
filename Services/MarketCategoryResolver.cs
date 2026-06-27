using System;
using System.Collections.Generic;
using StardewValley;

namespace RealityCheck.Services;

public sealed class MarketCategoryResolver
{
    public const int MinimumMarketManagedBaseUnitPrice = 10;

    private readonly ArtisanIdentityService artisanIdentityService;

    private static readonly HashSet<int> MarketCategoryWhitelist = new()
    {
        -2,  // Gem
        -4,  // Fish
        -5,  // Egg
        -6,  // Milk
        -15, // Metal Resource
        -16, // Building Resource
        -26, // Artisan Goods
        -75, // Vegetable
        -79, // Fruit
        -80, // Flower
        -81  // Forage / Greens
    };

    private static readonly HashSet<string> GenericFlavoredArtisanTemplateIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(O)342",           // Pickles
        "(O)344",           // Jelly
        "(O)348",           // Wine
        "(O)350",           // Juice
        "(O)SmokedFish",
        "(O)DriedMushrooms",
        "(O)DriedFruit"
    };

    public MarketCategoryResolver(ArtisanIdentityService artisanIdentityService)
    {
        this.artisanIdentityService = artisanIdentityService;
    }

    public MarketCategoryResult Resolve(
        Item? item,
        int baseUnitPrice
    )
    {
        if (item is null)
            return MarketCategoryResult.NotManaged("MissingItem");

        int safeBaseUnitPrice = Math.Max(0, baseUnitPrice);
        string itemId = item.QualifiedItemId ?? string.Empty;
        string category = this.ResolveBaseCategory(item);
        ArtisanIdentity identity = this.artisanIdentityService.Resolve(item);
        bool isFlavoredArtisan = IsFlavoredArtisanIdentity(identity);
        bool isGenericTemplate = this.IsGenericFlavoredArtisanTemplate(item, identity);

        if (safeBaseUnitPrice < MinimumMarketManagedBaseUnitPrice)
        {
            return new MarketCategoryResult
            {
                Category = category,
                IsMarketManaged = false,
                IsFlavoredArtisan = isFlavoredArtisan,
                IsGenericFlavoredArtisanTemplate = isGenericTemplate,
                MarketCommodityKey = identity.MarketCommodityKey,
                ParentItemId = identity.ParentItemId,
                ExclusionReason = "BaseUnitPriceBelowMinimum"
            };
        }

        if (!MarketCategoryWhitelist.Contains(item.Category))
        {
            return new MarketCategoryResult
            {
                Category = category,
                IsMarketManaged = false,
                IsFlavoredArtisan = isFlavoredArtisan,
                IsGenericFlavoredArtisanTemplate = isGenericTemplate,
                MarketCommodityKey = identity.MarketCommodityKey,
                ParentItemId = identity.ParentItemId,
                ExclusionReason = "CategoryNotWhitelisted"
            };
        }

        if (this.IsSlimeEgg(item))
        {
            return new MarketCategoryResult
            {
                Category = category,
                IsMarketManaged = false,
                IsFlavoredArtisan = isFlavoredArtisan,
                IsGenericFlavoredArtisanTemplate = isGenericTemplate,
                MarketCommodityKey = identity.MarketCommodityKey,
                ParentItemId = identity.ParentItemId,
                ExclusionReason = "SlimeEgg"
            };
        }

        if (isGenericTemplate)
        {
            return new MarketCategoryResult
            {
                Category = category,
                IsMarketManaged = false,
                IsFlavoredArtisan = false,
                IsGenericFlavoredArtisanTemplate = true,
                MarketCommodityKey = identity.MarketCommodityKey,
                ParentItemId = identity.ParentItemId,
                ExclusionReason = "GenericFlavoredArtisanTemplate"
            };
        }

        return new MarketCategoryResult
        {
            Category = isFlavoredArtisan ? "FlavoredArtisan" : category,
            IsMarketManaged = true,
            IsFlavoredArtisan = isFlavoredArtisan,
            IsGenericFlavoredArtisanTemplate = false,
            MarketCommodityKey = identity.MarketCommodityKey,
            ParentItemId = identity.ParentItemId,
            ExclusionReason = string.Empty
        };
    }

    public bool ShouldApplyMarketPricing(
        Item? item,
        int baseUnitPrice
    )
    {
        return this.Resolve(item, baseUnitPrice).IsMarketManaged;
    }

    public bool IsGenericFlavoredArtisanTemplate(
        Item? item,
        ArtisanIdentity? identity = null
    )
    {
        if (item is null)
            return false;

        ArtisanIdentity resolvedIdentity = identity ?? this.artisanIdentityService.Resolve(item);

        if (IsFlavoredArtisanIdentity(resolvedIdentity))
            return false;

        string itemId = item.QualifiedItemId ?? string.Empty;
        return GenericFlavoredArtisanTemplateIds.Contains(itemId);
    }

    private string ResolveBaseCategory(Item item)
    {
        if (this.IsSlimeEgg(item))
            return "SlimeEgg";

        return item.Category switch
        {
            -2 => "Gem",
            -4 => "Fish",
            -5 => "Egg",
            -6 => "Milk",
            -15 => "MetalResource",
            -16 => "BuildingResource",
            -26 => "ArtisanGoods",
            -75 => "Vegetable",
            -79 => "Fruit",
            -80 => "Flower",
            -81 => "Forage",
            _ => "Unmanaged"
        };
    }

    private bool IsSlimeEgg(Item item)
    {
        return !string.IsNullOrWhiteSpace(item.Name)
            && item.Name.Contains("Slime Egg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFlavoredArtisanIdentity(ArtisanIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.MarketCommodityKey)
            && identity.MarketCommodityKey.StartsWith("Artisan:", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(identity.ParentItemId);
    }
}

public sealed class MarketCategoryResult
{
    public string Category { get; init; } = "Unmanaged";

    public bool IsMarketManaged { get; init; }

    public bool IsFlavoredArtisan { get; init; }

    public bool IsGenericFlavoredArtisanTemplate { get; init; }

    public string MarketCommodityKey { get; init; } = string.Empty;

    public string ParentItemId { get; init; } = string.Empty;

    public string ExclusionReason { get; init; } = string.Empty;

    public static MarketCategoryResult NotManaged(string reason)
    {
        return new MarketCategoryResult
        {
            IsMarketManaged = false,
            ExclusionReason = reason
        };
    }
}
