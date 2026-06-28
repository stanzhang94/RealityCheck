using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class MarketPriceService
{
    public const double MinimumItemDailyFactor = 0.90;
    public const double MaximumItemDailyFactor = 1.10;

    private readonly ConfigService configService;
    private readonly MarketCategoryResolver marketCategoryResolver;
    private readonly WeatherFactorService weatherFactorService;
    private readonly IMonitor monitor;

    public MarketPriceService(
        ConfigService configService,
        MarketCategoryResolver marketCategoryResolver,
        WeatherFactorService weatherFactorService,
        IMonitor monitor
    )
    {
        this.configService = configService;
        this.marketCategoryResolver = marketCategoryResolver;
        this.weatherFactorService = weatherFactorService;
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

    public string GetItemDailyFactorMinimumLabel()
    {
        return MinimumItemDailyFactor.ToString("0.000");
    }

    public string GetItemDailyFactorMaximumLabel()
    {
        return MaximumItemDailyFactor.ToString("0.000");
    }

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

            MarketCategoryResult category = this.marketCategoryResolver.Resolve(
                item,
                baseUnitPrice
            );

            if (!category.IsMarketManaged)
                continue;

            if (category.IsGenericFlavoredArtisanTemplate)
                continue;

            string marketCommodityKey = string.IsNullOrWhiteSpace(category.MarketCommodityKey)
                ? item.QualifiedItemId
                : category.MarketCommodityKey;

            double multiplier = this.GetCurrentMarketMultiplier(category, marketCommodityKey);
            int marketUnitPrice = this.CalculateMarketUnitPrice(baseUnitPrice, multiplier);

            var entry = new MarketPriceTableEntry
            {
                ItemId = item.QualifiedItemId,
                MarketCommodityKey = marketCommodityKey,
                ParentItemId = category.ParentItemId,
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

            if (baseUnitPrice < MarketCategoryResolver.MinimumMarketManagedBaseUnitPrice)
                continue;

            double multiplier = this.GetCurrentMarketMultiplier(
                this.marketCategoryResolver.Resolve(
                    this.TryCreateIconItem(entry.ItemId),
                    baseUnitPrice
                ),
                entry.MarketCommodityKey
            );
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
        return this.marketCategoryResolver.ShouldApplyMarketPricing(
            item,
            baseUnitPrice
        );
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

        MarketCategoryResult category = this.marketCategoryResolver.Resolve(
            item,
            safeBaseUnitPrice
        );

        if (!category.IsMarketManaged)
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

        string marketCommodityKey = string.IsNullOrWhiteSpace(category.MarketCommodityKey)
            ? item.QualifiedItemId
            : category.MarketCommodityKey;

        double multiplier = this.GetCurrentMarketMultiplier(category, marketCommodityKey);
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

        MarketCategoryResult category = this.marketCategoryResolver.Resolve(
            item,
            safeBaseUnitPrice
        );

        if (!category.IsMarketManaged)
            return safeBaseUnitPrice;

        string marketCommodityKey = string.IsNullOrWhiteSpace(category.MarketCommodityKey)
            ? item.QualifiedItemId
            : category.MarketCommodityKey;

        return this.CalculateMarketUnitPrice(
            safeBaseUnitPrice,
            this.GetCurrentMarketMultiplier(category, marketCommodityKey)
        );
    }

    public double GetCurrentMarketMultiplier(
        MarketCategoryResult category,
        string marketCommodityKey
    )
    {
        double itemDailyFactor = this.GetItemDailyFactor(marketCommodityKey);
        double weatherFactor = this.weatherFactorService.GetWeatherFactor(category.Category);

        return CombineFactorDeltas(
            itemDailyFactor,
            weatherFactor
        );
    }

    private static double CombineFactorDeltas(params double[] factors)
    {
        double total = 1.0;

        foreach (double factor in factors)
        {
            if (double.IsNaN(factor) || double.IsInfinity(factor))
                continue;

            total += Math.Max(0.0, factor) - 1.0;
        }

        return Math.Max(0.0, total);
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

    public double GetItemDailyFactor(string marketCommodityKey)
    {
        if (string.IsNullOrWhiteSpace(marketCommodityKey))
            return 1.0;

        string seed = $"{Game1.uniqueIDForThisGame}|{Game1.year}|{Game1.currentSeason}|{Game1.dayOfMonth}|{marketCommodityKey}";
        ulong hash = ComputeStableHash(seed);
        double normalized = hash / (double)ulong.MaxValue;

        return MinimumItemDailyFactor
            + normalized * (MaximumItemDailyFactor - MinimumItemDailyFactor);
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
