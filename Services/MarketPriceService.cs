using System;
using System.Collections.Generic;
using System.Linq;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class MarketPriceService
{
    private const int MinimumMarketManagedBaseUnitPrice = 10;

    private readonly ConfigService configService;
    private readonly IMonitor monitor;

    public MarketPriceService(
        ConfigService configService,
        IMonitor monitor
    )
    {
        this.configService = configService;
        this.monitor = monitor;
    }

    public bool IsShippingBinShadowPriceTestEnabled()
    {
        return this.configService.Config.Market.EnableShippingBinShadowPriceTest;
    }

    public bool IsShippingSettlementTraceEnabled()
    {
        return this.configService.Config.Market.EnableShippingSettlementVerboseTrace;
    }

    public bool IsShippingBinMarketSettlementEnabled()
    {
        return this.configService.Config.Market.EnableShippingBinMarketSettlement;
    }


    private static readonly HashSet<int> MarketPriceCategoryWhitelist = new()
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

    public List<MarketPriceTableEntry> GetSellableObjectMarketPriceTable(
        IEnumerable<LedgerEntry>? ledgerEntries = null
    )
    {
        List<MarketPriceTableEntry> entries = new();
        HashSet<string> entryKeys = new(StringComparer.OrdinalIgnoreCase);

        if (!Context.IsWorldReady)
            return entries;

        foreach (var pair in Game1.objectData)
        {
            string objectId = pair.Key;

            if (string.IsNullOrWhiteSpace(objectId))
                continue;

            string qualifiedItemId = objectId.StartsWith("(O)", StringComparison.Ordinal)
                ? objectId
                : $"(O){objectId}";

            Item item;

            try
            {
                item = ItemRegistry.Create(qualifiedItemId);
            }
            catch
            {
                continue;
            }

            int baseUnitPrice;

            try
            {
                baseUnitPrice = item.sellToStorePrice(-1L);
            }
            catch
            {
                continue;
            }

            if (!this.ShouldApplyMarketPricing(item, baseUnitPrice))
                continue;

            if (this.IsGenericFlavoredArtisanTemplate(item))
                continue;

            double multiplier = this.GetShadowPriceMultiplier();
            int marketUnitPrice = this.CalculateMarketUnitPrice(baseUnitPrice, multiplier);

            var entry = new MarketPriceTableEntry
            {
                ItemId = item.QualifiedItemId,
                MarketCommodityKey = item.QualifiedItemId,
                ItemName = item.DisplayName,
                BaseUnitPrice = baseUnitPrice,
                MarketMultiplier = multiplier,
                MarketUnitPrice = marketUnitPrice,
                Difference = marketUnitPrice - baseUnitPrice,
                IconItem = item
            };

            entries.Add(entry);
            entryKeys.Add(entry.MarketCommodityKey);
        }

        this.AddDiscoveredFlavoredArtisanEntries(
            entries,
            entryKeys,
            ledgerEntries
        );

        return entries
            .OrderByDescending(e => Math.Abs(e.Difference))
            .ThenBy(e => e.ItemName)
            .ToList();
    }



    private void AddDiscoveredFlavoredArtisanEntries(
        List<MarketPriceTableEntry> entries,
        HashSet<string> entryKeys,
        IEnumerable<LedgerEntry>? ledgerEntries
    )
    {
        if (ledgerEntries is null)
            return;

        double multiplier = this.GetShadowPriceMultiplier();
        HashSet<string> added = new(StringComparer.OrdinalIgnoreCase);

        foreach (LedgerEntry entry in ledgerEntries.Reverse())
        {
            if (!this.IsDiscoveredFlavoredArtisanLedgerEntry(entry))
                continue;

            if (!added.Add(entry.MarketCommodityKey))
                continue;

            if (entryKeys.Contains(entry.MarketCommodityKey))
                continue;

            int baseUnitPrice = Math.Max(0, entry.BaseUnitPrice);

            if (baseUnitPrice < MinimumMarketManagedBaseUnitPrice)
                continue;

            int marketUnitPrice = this.CalculateMarketUnitPrice(
                baseUnitPrice,
                multiplier
            );

            Item? iconItem = this.TryCreateIconItem(entry.ItemId);

            entries.Add(
                new MarketPriceTableEntry
                {
                    ItemId = entry.ItemId,
                    MarketCommodityKey = entry.MarketCommodityKey,
                    ParentItemId = entry.ParentItemId,
                    IsDiscoveredArtisan = true,
                    ItemName = entry.ItemName,
                    BaseUnitPrice = baseUnitPrice,
                    MarketMultiplier = multiplier,
                    MarketUnitPrice = marketUnitPrice,
                    Difference = marketUnitPrice - baseUnitPrice,
                    IconItem = iconItem
                }
            );

            entryKeys.Add(entry.MarketCommodityKey);
        }
    }

    private bool IsDiscoveredFlavoredArtisanLedgerEntry(LedgerEntry entry)
    {
        if (!string.Equals(entry.Type, "Income", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(entry.MarketCommodityKey))
            return false;

        if (!entry.MarketCommodityKey.StartsWith("Artisan:", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(entry.ParentItemId))
            return false;

        if (entry.BaseUnitPrice <= 0)
            return false;

        return true;
    }

    private bool IsGenericFlavoredArtisanTemplate(Item item)
    {
        string itemId = item.QualifiedItemId ?? string.Empty;

        return string.Equals(itemId, "(O)342", StringComparison.OrdinalIgnoreCase)  // Pickles
            || string.Equals(itemId, "(O)344", StringComparison.OrdinalIgnoreCase)  // Jelly
            || string.Equals(itemId, "(O)348", StringComparison.OrdinalIgnoreCase)  // Wine
            || string.Equals(itemId, "(O)350", StringComparison.OrdinalIgnoreCase)  // Juice
            || string.Equals(itemId, "(O)SmokedFish", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemId, "(O)DriedMushrooms", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemId, "(O)DriedFruit", StringComparison.OrdinalIgnoreCase);
    }

    private Item? TryCreateIconItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        try
        {
            return ItemRegistry.Create(itemId);
        }
        catch
        {
            return null;
        }
    }


    public bool ShouldApplyMarketPricing(
        Item? item,
        int baseUnitPrice
    )
    {
        if (item is null)
            return false;

        if (baseUnitPrice < MinimumMarketManagedBaseUnitPrice)
            return false;

        if (!MarketPriceCategoryWhitelist.Contains(item.Category))
            return false;

        // Slime eggs are technically sellable, but they are special monster/slime-hutch items,
        // not normal farm-market commodities. Keep them out even if the game categorizes them like eggs.
        if (!string.IsNullOrWhiteSpace(item.Name)
            && item.Name.Contains("Slime Egg", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public MarketPriceResult GetShippingBinMarketSellPrice(
        Item item,
        int quantity,
        int baseUnitPrice
    )
    {
        int safeQuantity = Math.Max(0, quantity);
        int safeBaseUnitPrice = Math.Max(0, baseUnitPrice);
        int baseTotal = safeBaseUnitPrice * safeQuantity;

        if (!this.ShouldApplyMarketPricing(item, safeBaseUnitPrice))
        {
            return new MarketPriceResult
            {
                ItemName = item.DisplayName,
                ItemId = item.QualifiedItemId,
                Quantity = safeQuantity,
                BaseUnitPrice = safeBaseUnitPrice,
                BaseTotal = baseTotal,
                MarketMultiplier = 1.0,
                MarketTotal = baseTotal,
                MarketUnitPrice = safeBaseUnitPrice
            };
        }

        double multiplier = this.GetShadowPriceMultiplier();
        int marketTotal = this.CalculateMarketTotal(baseTotal, multiplier);

        return new MarketPriceResult
        {
            ItemName = item.DisplayName,
            ItemId = item.QualifiedItemId,
            Quantity = safeQuantity,
            BaseUnitPrice = safeBaseUnitPrice,
            BaseTotal = baseTotal,
            MarketMultiplier = multiplier,
            MarketTotal = marketTotal,
            MarketUnitPrice = safeQuantity > 0
                ? (double)marketTotal / safeQuantity
                : 0.0
        };
    }


    public MarketPriceResult GetShippingBinShadowSellPrice(
        Item item,
        int quantity,
        int baseUnitPrice
    )
    {
        return this.GetShippingBinMarketSellPrice(
            item,
            quantity,
            baseUnitPrice
        );
    }

    public int GetShopSaleMarketUnitPrice(
        Item item,
        int baseUnitPrice
    )
    {
        int safeBaseUnitPrice = Math.Max(0, baseUnitPrice);

        if (!this.ShouldApplyMarketPricing(item, safeBaseUnitPrice))
            return safeBaseUnitPrice;

        return this.CalculateMarketUnitPrice(
            safeBaseUnitPrice,
            this.GetShadowPriceMultiplier()
        );
    }

    public int CalculateMarketUnitPrice(
        int baseUnitPrice,
        double multiplier
    )
    {
        if (baseUnitPrice <= 0)
            return 0;

        double safeMultiplier = Math.Max(0.0, multiplier);

        return Math.Max(
            0,
            (int)Math.Round(
                baseUnitPrice * safeMultiplier,
                MidpointRounding.AwayFromZero
            )
        );
    }

    public int CalculateMarketTotal(
        int baseTotal,
        double multiplier
    )
    {
        if (baseTotal <= 0)
            return 0;

        double safeMultiplier = Math.Max(0.0, multiplier);

        return Math.Max(
            0,
            (int)Math.Round(
                baseTotal * safeMultiplier,
                MidpointRounding.AwayFromZero
            )
        );
    }

    public double GetShadowPriceMultiplier()
    {
        double multiplier = this.configService.Config.Market.ShadowPriceMultiplier;

        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier <= 0)
        {
            this.monitor.Log(
                $"Invalid shadow market multiplier {multiplier:0.###}; falling back to 1.0.",
                LogLevel.Warn
            );

            return 1.0;
        }

        return multiplier;
    }
}
