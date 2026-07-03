using System;
using System.Collections.Generic;
using System.Linq;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class ExchangeContractCatalogService
{
    private static readonly HashSet<int> TradableCategories = new()
    {
        -4,  // Fish
        -5,  // Egg
        -6,  // Milk
        -15, // Metal Resource
        -26, // Artisan Goods
        -75, // Vegetable
        -79  // Fruit
    };

    private static readonly HashSet<string> ExcludedRawCommodityItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(O)268", // Starfruit
        "(O)454", // Ancient Fruit
        "(O)417", // Sweet Gem Berry
        "(O)90",  // Cactus Fruit
        "(O)832", // Pineapple
        "(O)830"  // Taro Root
    };

    private static readonly HashSet<string> ExcludedGenericArtisanItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(O)348", // Wine: generic wine without a parent fruit has no meaningful exchange chart.
        "348",
        "(O)350", // Juice: generic juice without a parent vegetable has no meaningful exchange chart.
        "350"
    };

    private readonly MarketPriceService marketPriceService;
    private readonly LedgerService ledgerService;
    private readonly IMonitor monitor;

    public ExchangeContractCatalogService(
        MarketPriceService marketPriceService,
        LedgerService ledgerService,
        IMonitor monitor
    )
    {
        this.marketPriceService = marketPriceService;
        this.ledgerService = ledgerService;
        this.monitor = monitor;
    }

    public List<ExchangeContractSpec> GetContractCatalog()
    {
        if (!Context.IsWorldReady)
            return new List<ExchangeContractSpec>();

        List<MarketPriceTableEntry> marketEntries;

        try
        {
            marketEntries = this.marketPriceService.GetSellableObjectMarketPriceTable(
                this.ledgerService.GetEntries()
            );
        }
        catch (Exception ex)
        {
            this.monitor.Log(
                $"Failed to build exchange contract catalog: {ex}",
                LogLevel.Warn
            );

            return new List<ExchangeContractSpec>();
        }

        return marketEntries
            .Where(this.IsTradable)
            .Select(this.ToContractSpec)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.DisplayName)
            .ToList();
    }

    public string GetDebugSummary(int maxRows = 12)
    {
        List<ExchangeContractSpec> contracts = this.GetContractCatalog();

        if (contracts.Count == 0)
            return "Exchange contract catalog is empty.";

        return string.Join(
            Environment.NewLine,
            contracts
                .Take(Math.Max(1, maxRows))
                .Select(c =>
                    $"{c.DisplayName} | {c.Category} | {c.MarketUnitPrice}g | Lot {c.QuantityPerLot} | Value {c.ContractValuePerLot}g | Margin {c.InitialMarginRequiredPerLot}g | Terms {c.GetSupportedTermsLabel()}"
                )
        );
    }

    public IReadOnlyList<MarketPriceHistoryPoint> GetMarketPriceHistory(string marketCommodityKey)
    {
        if (string.IsNullOrWhiteSpace(marketCommodityKey))
            return Array.Empty<MarketPriceHistoryPoint>();

        try
        {
            return this.marketPriceService.GetMarketPriceHistory(marketCommodityKey);
        }
        catch (Exception ex)
        {
            this.monitor.Log(
                $"Failed to read exchange price history for {marketCommodityKey}: {ex.Message}",
                LogLevel.Trace
            );

            return Array.Empty<MarketPriceHistoryPoint>();
        }
    }

    private bool IsTradable(MarketPriceTableEntry entry)
    {
        if (entry is null)
            return false;

        if (string.IsNullOrWhiteSpace(entry.MarketCommodityKey))
            return false;

        if (!this.HasUsablePriceHistory(entry))
            return false;

        if (entry.BaseUnitPrice < MarketCategoryResolver.MinimumMarketManagedBaseUnitPrice)
            return false;

        if (IsExcludedGenericArtisan(entry))
            return false;

        if (IsFlavoredArtisan(entry))
            return true;

        if (ExcludedRawCommodityItemIds.Contains(entry.ItemId))
            return false;

        Item? item = entry.IconItem ?? TryCreateItem(entry.ItemId);

        if (item is null)
            return false;

        return TradableCategories.Contains(item.Category);
    }

    private ExchangeContractSpec ToContractSpec(MarketPriceTableEntry entry)
    {
        Item? item = entry.IconItem ?? TryCreateItem(entry.ItemId);
        string category = ResolveExchangeCategory(entry, item);
        bool isRawCrop = category is "Fruit" or "Vegetable";
        int marketUnitPrice = Math.Max(0, (int)Math.Round(entry.MarketUnitPrice));
        int contractValue = marketUnitPrice * ExchangeService.DefaultQuantityPerLot;

        return new ExchangeContractSpec
        {
            MarketCommodityKey = entry.MarketCommodityKey,
            ItemId = entry.ItemId,
            ParentItemId = entry.ParentItemId,
            DisplayName = entry.ItemName,
            Category = category,
            MarketUnitPrice = marketUnitPrice,
            QuantityPerLot = ExchangeService.DefaultQuantityPerLot,
            ContractValuePerLot = contractValue,
            InitialMarginRequiredPerLot = (int)Math.Ceiling(contractValue * ExchangeService.InitialMarginRate),
            MaintenanceMarginRequiredPerLot = (int)Math.Ceiling(contractValue * ExchangeService.MaintenanceMarginRate),
            SupportsSevenDayContract = !isRawCrop,
            SupportsFourteenDayContract = true,
            SupportsTwentyEightDayContract = true
        };
    }

    private bool HasUsablePriceHistory(MarketPriceTableEntry entry)
    {
        try
        {
            return this.marketPriceService
                .GetMarketPriceHistory(entry.MarketCommodityKey)
                .Count(point => point.MarketUnitPrice > 0) >= 2;
        }
        catch (Exception ex)
        {
            this.monitor.Log(
                $"Skipping exchange commodity without usable price history: {entry.MarketCommodityKey}. {ex.Message}",
                LogLevel.Trace
            );

            return false;
        }
    }

    private static string ResolveExchangeCategory(MarketPriceTableEntry entry, Item? item)
    {
        if (IsFlavoredArtisan(entry))
            return "FlavoredArtisan";

        if (item is null)
            return "Unknown";

        return item.Category switch
        {
            -4 => "Fish",
            -5 => "Egg",
            -6 => "Milk",
            -15 => "MetalResource",
            -26 => "ArtisanGoods",
            -75 => "Vegetable",
            -79 => "Fruit",
            _ => "Unknown"
        };
    }


    private static bool IsExcludedGenericArtisan(MarketPriceTableEntry entry)
    {
        if (entry is null)
            return false;

        if (!string.IsNullOrWhiteSpace(entry.ParentItemId))
            return false;

        return ExcludedGenericArtisanItemIds.Contains(entry.ItemId);
    }

    private static bool IsFlavoredArtisan(MarketPriceTableEntry entry)
    {
        return entry.IsDiscoveredArtisan
            || (
                !string.IsNullOrWhiteSpace(entry.MarketCommodityKey)
                && entry.MarketCommodityKey.StartsWith("Artisan:", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(entry.ParentItemId)
            );
    }

    private static Item? TryCreateItem(string itemId)
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
}
