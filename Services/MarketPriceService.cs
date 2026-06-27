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

    public List<MarketPriceTableEntry> GetSellableObjectMarketPriceTable()
    {
        List<MarketPriceTableEntry> entries = new();

        if (!Context.IsWorldReady)
            return entries;

        foreach (var pair in Game1.objectData)
        {
            string objectId = pair.Key;
            var objectData = pair.Value;

            if (string.IsNullOrWhiteSpace(objectId))
                continue;

            if (!this.ShouldIncludeInMarketPriceTable(objectData.Category, objectData.Name))
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

            if (baseUnitPrice <= 0)
                continue;

            double multiplier = this.GetShadowPriceMultiplier();
            double marketUnitPrice = Math.Max(0.0, baseUnitPrice * multiplier);

            entries.Add(
                new MarketPriceTableEntry
                {
                    ItemId = item.QualifiedItemId,
                    ItemName = item.DisplayName,
                    BaseUnitPrice = baseUnitPrice,
                    MarketMultiplier = multiplier,
                    MarketUnitPrice = marketUnitPrice,
                    Difference = marketUnitPrice - baseUnitPrice,
                    IconItem = item
                }
            );
        }

        return entries
            .OrderByDescending(e => Math.Abs(e.Difference))
            .ThenBy(e => e.ItemName)
            .ToList();
    }

    private bool ShouldIncludeInMarketPriceTable(int category, string? internalName)
    {
        if (!MarketPriceCategoryWhitelist.Contains(category))
            return false;

        // Slime eggs are technically sellable, but they are special monster/slime-hutch items,
        // not normal farm-market commodities. Keep them out even if the game categorizes them like eggs.
        if (!string.IsNullOrWhiteSpace(internalName)
            && internalName.Contains("Slime Egg", StringComparison.OrdinalIgnoreCase))
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
        double multiplier = this.GetShadowPriceMultiplier();
        int baseTotal = Math.Max(0, baseUnitPrice) * Math.Max(0, quantity);
        int marketTotal = this.CalculateMarketTotal(baseTotal, multiplier);

        return new MarketPriceResult
        {
            ItemName = item.DisplayName,
            ItemId = item.QualifiedItemId,
            Quantity = quantity,
            BaseUnitPrice = baseUnitPrice,
            BaseTotal = baseTotal,
            MarketMultiplier = multiplier,
            MarketTotal = marketTotal,
            MarketUnitPrice = quantity > 0
                ? (double)marketTotal / quantity
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
