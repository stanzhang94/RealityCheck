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
    public const double MinimumItemDailyFactor = 0.95;
    public const double MaximumItemDailyFactor = 1.05;

    private readonly ConfigService configService;
    private readonly MarketCategoryResolver marketCategoryResolver;
    private readonly WeatherFactorService weatherFactorService;
    private readonly FestivalFactorService festivalFactorService;
    private readonly OffSeasonFactorService offSeasonFactorService;
    private readonly MarketTrendService marketTrendService;
    private readonly IMonitor monitor;

    public MarketPriceService(
        ConfigService configService,
        MarketCategoryResolver marketCategoryResolver,
        WeatherFactorService weatherFactorService,
        FestivalFactorService festivalFactorService,
        OffSeasonFactorService offSeasonFactorService,
        MarketTrendService marketTrendService,
        IMonitor monitor
    )
    {
        this.configService = configService;
        this.marketCategoryResolver = marketCategoryResolver;
        this.weatherFactorService = weatherFactorService;
        this.festivalFactorService = festivalFactorService;
        this.offSeasonFactorService = offSeasonFactorService;
        this.marketTrendService = marketTrendService;
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

            MarketPriceFactorBreakdown factors = this.GetCurrentMarketFactorBreakdown(
                category,
                marketCommodityKey,
                item
            );

            int marketUnitPrice = this.CalculateRecursiveMarketUnitPrice(
                marketCommodityKey,
                baseUnitPrice,
                factors.DailyMultiplier
            );

            double displayDailyMultiplier = this.GetDisplayDailyMultiplier(
                marketCommodityKey,
                marketUnitPrice
            );
            double displayTotalMultiplier = GetDisplayTotalMultiplier(
                baseUnitPrice,
                marketUnitPrice
            );

            this.RecordMarketPriceHistory(
                marketCommodityKey,
                item.QualifiedItemId,
                item.DisplayName,
                baseUnitPrice,
                marketUnitPrice,
                displayDailyMultiplier,
                displayTotalMultiplier
            );

            var entry = new MarketPriceTableEntry
            {
                ItemId = item.QualifiedItemId,
                MarketCommodityKey = marketCommodityKey,
                ParentItemId = category.ParentItemId,
                ItemName = item.DisplayName,
                BaseUnitPrice = baseUnitPrice,
                DailyMultiplier = displayDailyMultiplier,
                TotalMultiplier = displayTotalMultiplier,
                MarketMultiplier = displayTotalMultiplier,
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

            Item? iconItem = this.TryCreateIconItem(entry.ItemId);
            MarketCategoryResult category = new()
            {
                Category = "FlavoredArtisan",
                IsMarketManaged = true,
                IsFlavoredArtisan = true,
                IsGenericFlavoredArtisanTemplate = false,
                MarketCommodityKey = entry.MarketCommodityKey,
                ParentItemId = entry.ParentItemId,
                ExclusionReason = string.Empty
            };

            MarketPriceFactorBreakdown factors = this.GetCurrentMarketFactorBreakdown(
                category,
                entry.MarketCommodityKey,
                iconItem
            );

            int marketUnitPrice = this.CalculateRecursiveMarketUnitPrice(
                entry.MarketCommodityKey,
                baseUnitPrice,
                factors.DailyMultiplier
            );

            double displayDailyMultiplier = this.GetDisplayDailyMultiplier(
                entry.MarketCommodityKey,
                marketUnitPrice
            );
            double displayTotalMultiplier = GetDisplayTotalMultiplier(
                baseUnitPrice,
                marketUnitPrice
            );

            this.RecordMarketPriceHistory(
                entry.MarketCommodityKey,
                entry.ItemId,
                entry.ItemName,
                baseUnitPrice,
                marketUnitPrice,
                displayDailyMultiplier,
                displayTotalMultiplier
            );

            entries.Add(
                new MarketPriceTableEntry
                {
                    ItemId = entry.ItemId,
                    MarketCommodityKey = entry.MarketCommodityKey,
                    ParentItemId = entry.ParentItemId,
                    IsDiscoveredArtisan = true,
                    ItemName = entry.ItemName,
                    BaseUnitPrice = baseUnitPrice,
                    DailyMultiplier = displayDailyMultiplier,
                    TotalMultiplier = displayTotalMultiplier,
                    MarketMultiplier = displayTotalMultiplier,
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

    public void UpdateAllMarketPricesForToday(
        IEnumerable<LedgerEntry>? ledgerEntries = null
    )
    {
        if (!Context.IsWorldReady)
            return;

        _ = this.GetSellableObjectMarketPriceTable(ledgerEntries);
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
                DailyMultiplier = 1.0,
                TotalMultiplier = 1.0,
                MarketTotal = baseTotal,
                MarketUnitPrice = safeBaseUnitPrice
            };
        }

        string marketCommodityKey = string.IsNullOrWhiteSpace(category.MarketCommodityKey)
            ? item.QualifiedItemId
            : category.MarketCommodityKey;

        MarketPriceFactorBreakdown factors = this.GetCurrentMarketFactorBreakdown(
            category,
            marketCommodityKey,
            item
        );

        int marketUnitPrice = this.CalculateRecursiveMarketUnitPrice(
            marketCommodityKey,
            safeBaseUnitPrice,
            factors.DailyMultiplier
        );
        int marketTotal = marketUnitPrice * safeQuantity;

        double displayDailyMultiplier = this.GetDisplayDailyMultiplier(
            marketCommodityKey,
            marketUnitPrice
        );
        double displayTotalMultiplier = GetDisplayTotalMultiplier(
            safeBaseUnitPrice,
            marketUnitPrice
        );

        this.RecordMarketPriceHistory(
            marketCommodityKey,
            item.QualifiedItemId,
            item.DisplayName,
            safeBaseUnitPrice,
            marketUnitPrice,
            displayDailyMultiplier,
            displayTotalMultiplier
        );

        return new MarketPriceResult
        {
            ItemName = item.DisplayName,
            ItemId = item.QualifiedItemId,
            Quantity = safeQuantity,
            BaseUnitPrice = safeBaseUnitPrice,
            BaseTotal = baseTotal,
            MarketMultiplier = displayTotalMultiplier,
            DailyMultiplier = displayDailyMultiplier,
            TotalMultiplier = displayTotalMultiplier,
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

        MarketPriceFactorBreakdown factors = this.GetCurrentMarketFactorBreakdown(
            category,
            marketCommodityKey,
            item
        );

        int marketUnitPrice = this.CalculateRecursiveMarketUnitPrice(
            marketCommodityKey,
            safeBaseUnitPrice,
            factors.DailyMultiplier
        );

        double displayDailyMultiplier = this.GetDisplayDailyMultiplier(
            marketCommodityKey,
            marketUnitPrice
        );
        double displayTotalMultiplier = GetDisplayTotalMultiplier(
            safeBaseUnitPrice,
            marketUnitPrice
        );

        this.RecordMarketPriceHistory(
            marketCommodityKey,
            item.QualifiedItemId,
            item.DisplayName,
            safeBaseUnitPrice,
            marketUnitPrice,
            displayDailyMultiplier,
            displayTotalMultiplier
        );

        return marketUnitPrice;
    }

    public MarketPriceFactorBreakdown GetCurrentMarketFactorBreakdown(
        MarketCategoryResult category,
        string marketCommodityKey,
        Item? item = null
    )
    {
        double itemDailyFactor = this.GetItemDailyFactor(marketCommodityKey);
        MarketTrendSnapshot trend = this.marketTrendService.GetTrendSnapshot(marketCommodityKey);

        if (category.IsFlavoredArtisan)
        {
            MarketPriceFactorBreakdown artisanTransmission = this.GetArtisanTransmissionFactor(category);

            return new MarketPriceFactorBreakdown
            {
                DailyMultiplier = CombineFactorDeltas(
                    trend.TodayTrendChange,
                    itemDailyFactor,
                    artisanTransmission.DailyMultiplier
                ),
                TotalMultiplier = CombineFactorDeltas(
                    trend.TodayTrendChange,
                    itemDailyFactor,
                    artisanTransmission.DailyMultiplier
                )
            };
        }

        double weatherFactor = this.weatherFactorService.GetWeatherFactor(category.Category);
        double festivalFactor = this.festivalFactorService.GetFestivalFactor(category.Category);
        double offSeasonFactor = this.offSeasonFactorService.GetOffSeasonFactor(
            category,
            item,
            marketCommodityKey
        );

        return new MarketPriceFactorBreakdown
        {
            DailyMultiplier = CombineFactorDeltas(
                trend.TodayTrendChange,
                itemDailyFactor,
                weatherFactor,
                festivalFactor,
                offSeasonFactor
            ),
            TotalMultiplier = CombineFactorDeltas(
                trend.TodayTrendChange,
                itemDailyFactor,
                weatherFactor,
                festivalFactor,
                offSeasonFactor
            )
        };
    }

    private MarketPriceFactorBreakdown GetArtisanTransmissionFactor(MarketCategoryResult artisanCategory)
    {
        if (!artisanCategory.IsFlavoredArtisan)
            return new MarketPriceFactorBreakdown();

        if (string.IsNullOrWhiteSpace(artisanCategory.ParentItemId))
            return new MarketPriceFactorBreakdown();

        Item? parentItem = this.TryCreateIconItem(artisanCategory.ParentItemId);

        if (parentItem is null)
            return new MarketPriceFactorBreakdown();

        int parentBaseUnitPrice;

        try
        {
            parentBaseUnitPrice = Math.Max(0, parentItem.sellToStorePrice(-1L));
        }
        catch
        {
            return new MarketPriceFactorBreakdown();
        }

        MarketCategoryResult parentCategory = this.marketCategoryResolver.Resolve(
            parentItem,
            parentBaseUnitPrice
        );

        if (!parentCategory.IsMarketManaged)
            return new MarketPriceFactorBreakdown();

        if (parentCategory.IsFlavoredArtisan)
            return new MarketPriceFactorBreakdown();

        string parentMarketCommodityKey = string.IsNullOrWhiteSpace(parentCategory.MarketCommodityKey)
            ? parentItem.QualifiedItemId
            : parentCategory.MarketCommodityKey;

        MarketPriceFactorBreakdown parentFactors = this.GetBaseMarketFactorBreakdownWithoutArtisanTransmission(
            parentCategory,
            parentMarketCommodityKey,
            parentItem
        );

        return new MarketPriceFactorBreakdown
        {
            DailyMultiplier = Math.Max(0.0, 1.0 + (parentFactors.DailyMultiplier - 1.0) * 0.5),
            TotalMultiplier = Math.Max(0.0, 1.0 + (parentFactors.TotalMultiplier - 1.0) * 0.5)
        };
    }

    private MarketPriceFactorBreakdown GetBaseMarketFactorBreakdownWithoutArtisanTransmission(
        MarketCategoryResult category,
        string marketCommodityKey,
        Item? item
    )
    {
        double itemDailyFactor = this.GetItemDailyFactor(marketCommodityKey);
        MarketTrendSnapshot trend = this.marketTrendService.GetTrendSnapshot(marketCommodityKey);
        double weatherFactor = this.weatherFactorService.GetWeatherFactor(category.Category);
        double festivalFactor = this.festivalFactorService.GetFestivalFactor(category.Category);
        double offSeasonFactor = this.offSeasonFactorService.GetOffSeasonFactor(
            category,
            item,
            marketCommodityKey
        );

        return new MarketPriceFactorBreakdown
        {
            DailyMultiplier = CombineFactorDeltas(
                trend.TodayTrendChange,
                itemDailyFactor,
                weatherFactor,
                festivalFactor,
                offSeasonFactor
            ),
            TotalMultiplier = CombineFactorDeltas(
                trend.TodayTrendChange,
                itemDailyFactor,
                weatherFactor,
                festivalFactor,
                offSeasonFactor
            )
        };
    }

    private void RecordMarketPriceHistory(
        string marketCommodityKey,
        string itemId,
        string itemName,
        int baseUnitPrice,
        int marketUnitPrice,
        double displayDailyMultiplier,
        double displayTotalMultiplier
    )
    {
        this.marketTrendService.RecordPrice(
            marketCommodityKey,
            itemId,
            itemName,
            baseUnitPrice,
            marketUnitPrice,
            displayDailyMultiplier,
            displayTotalMultiplier
        );
    }


    // Step 16.6 formula model:
    // Today market price = yesterday market price * today multiplier.
    // Today multiplier = 1 + trend offset today + item daily offset + weather offset + festival offset + off-season offset + artisan transmission offset.
    // Base price is only used when there is no previous market price, and for total multiplier display / boundary checks.
    private int CalculateRecursiveMarketUnitPrice(
        string marketCommodityKey,
        int baseUnitPrice,
        double dailyMultiplier
    )
    {
        int safeBaseUnitPrice = Math.Max(0, baseUnitPrice);

        if (safeBaseUnitPrice <= 0)
            return 0;

        int previousMarketUnitPrice = this.marketTrendService.GetPreviousMarketUnitPrice(
            marketCommodityKey,
            safeBaseUnitPrice
        );

        double safeDailyMultiplier = Math.Max(0.0, dailyMultiplier);

        return Math.Max(
            0,
            (int)Math.Round(
                previousMarketUnitPrice * safeDailyMultiplier,
                MidpointRounding.AwayFromZero
            )
        );
    }

    private double GetDisplayDailyMultiplier(
        string marketCommodityKey,
        int currentMarketUnitPrice
    )
    {
        return this.marketTrendService.GetDayOverDayMultiplier(
            marketCommodityKey,
            currentMarketUnitPrice
        );
    }

    private static double GetDisplayTotalMultiplier(
        int baseUnitPrice,
        int marketUnitPrice
    )
    {
        if (baseUnitPrice <= 0)
            return 1.0;

        return Math.Max(0.0, (double)Math.Max(0, marketUnitPrice) / baseUnitPrice);
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


    public IReadOnlyList<MarketPriceHistoryPoint> GetMarketPriceHistory(string marketCommodityKey)
    {
        return this.marketTrendService.GetHistory(marketCommodityKey);
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
