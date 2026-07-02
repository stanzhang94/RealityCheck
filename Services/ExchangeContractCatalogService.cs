using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class ExchangeContractCatalogService
{
    private static readonly HashSet<int> TradableCategories = new()
    {
        -26, // Artisan Goods
        -75, // Vegetable
        -79  // Fruit
    };

    private static readonly HashSet<string> ExcludedRawCommodityItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(O)268", // Starfruit raw crop; Starfruit Wine may still be traded.
        "(O)454", // Ancient Fruit raw crop; Ancient Fruit Wine may still be traded.
        "(O)417", // Sweet Gem Berry
        "(O)90",  // Cactus Fruit
        "(O)832", // Pineapple raw crop; Pineapple Wine may still be traded.
        "(O)830"  // Taro Root
    };

    private static readonly HashSet<string> SyntheticFruitArtisanParentIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(O)268", // Starfruit
        "(O)454", // Ancient Fruit
        "(O)832"  // Pineapple
    };

    private static readonly HashSet<string> NakedFlavoredArtisanTemplateNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dried",
        "Smoked",
        "Wine",
        "Jelly",
        "Juice",
        "Pickles",
        "Pickle"
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

        List<ExchangeContractSpec> contracts = marketEntries
            .Where(this.IsTradable)
            .Select(this.ToContractSpec)
            .ToList();

        this.AddMissingRawCommodityContracts(contracts, marketEntries);

        return contracts
            .GroupBy(c => c.MarketCommodityKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(c => c.Category == "FlavoredArtisan").First())
            .OrderBy(e => GetCategorySortOrder(e.Category))
            .ThenBy(e => e.DisplayName)
            .ToList();
    }


    public IReadOnlyList<MarketPriceHistoryPoint> GetMarketPriceHistory(string marketCommodityKey)
    {
        return this.marketPriceService.GetMarketPriceHistory(marketCommodityKey);
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

    private bool IsTradable(MarketPriceTableEntry entry)
    {
        if (entry is null)
            return false;

        if (string.IsNullOrWhiteSpace(entry.MarketCommodityKey))
            return false;

        if (entry.BaseUnitPrice < MarketCategoryResolver.MinimumMarketManagedBaseUnitPrice)
            return false;

        if (IsNakedFlavoredArtisanTemplate(entry.ItemName, entry.ParentItemId))
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
            SupportsSevenDayContract = isRawCrop ? SupportsSevenDayRawCrop(entry.ItemId) : true,
            SupportsFourteenDayContract = true,
            SupportsTwentyEightDayContract = true
        };
    }

    private void AddMissingRawCommodityContracts(
        List<ExchangeContractSpec> contracts,
        List<MarketPriceTableEntry> marketEntries
    )
    {
        HashSet<string> existingKeys = new(
            contracts.Select(c => c.MarketCommodityKey)
                .Concat(marketEntries.Select(e => e.MarketCommodityKey)),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var pair in Game1.objectData)
        {
            string rawObjectId = pair.Key;
            string itemId = rawObjectId.StartsWith("(O)", StringComparison.Ordinal)
                ? rawObjectId
                : $"(O){rawObjectId}";

            if (ExcludedRawCommodityItemIds.Contains(itemId))
                continue;

            Item? item = TryCreateItem(itemId);
            if (item is null)
                continue;

            string category = item.Category switch
            {
                -26 => "ArtisanGoods",
                -75 => "Vegetable",
                -79 => "Fruit",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(category))
                continue;

            if (category == "ArtisanGoods" && IsNakedFlavoredArtisanTemplate(item.DisplayName, string.Empty))
                continue;

            int baseUnitPrice;
            try
            {
                baseUnitPrice = Math.Max(0, item.sellToStorePrice(-1L));
            }
            catch
            {
                continue;
            }

            if (baseUnitPrice < MarketCategoryResolver.MinimumMarketManagedBaseUnitPrice)
                continue;

            string marketCommodityKey = item.QualifiedItemId;
            if (!existingKeys.Add(marketCommodityKey))
                continue;

            MarketCategoryResult marketCategory = new()
            {
                Category = category,
                IsMarketManaged = true,
                IsFlavoredArtisan = false,
                IsGenericFlavoredArtisanTemplate = false,
                MarketCommodityKey = marketCommodityKey,
                ParentItemId = string.Empty,
                ExclusionReason = string.Empty
            };

            MarketPriceFactorBreakdown factors = this.marketPriceService.GetCurrentMarketFactorBreakdown(
                marketCategory,
                marketCommodityKey,
                item
            );

            int marketUnitPrice = this.marketPriceService.CalculateMarketUnitPrice(
                baseUnitPrice,
                factors.DailyMultiplier
            );

            int contractValue = marketUnitPrice * ExchangeService.DefaultQuantityPerLot;
            bool isRawCrop = category is "Fruit" or "Vegetable";

            contracts.Add(
                new ExchangeContractSpec
                {
                    MarketCommodityKey = marketCommodityKey,
                    ItemId = item.QualifiedItemId,
                    ParentItemId = string.Empty,
                    DisplayName = item.DisplayName,
                    Category = category,
                    MarketUnitPrice = marketUnitPrice,
                    QuantityPerLot = ExchangeService.DefaultQuantityPerLot,
                    ContractValuePerLot = contractValue,
                    InitialMarginRequiredPerLot = (int)Math.Ceiling(contractValue * ExchangeService.InitialMarginRate),
                    MaintenanceMarginRequiredPerLot = (int)Math.Ceiling(contractValue * ExchangeService.MaintenanceMarginRate),
                    SupportsSevenDayContract = isRawCrop ? SupportsSevenDayRawCrop(item.QualifiedItemId) : true,
                    SupportsFourteenDayContract = true,
                    SupportsTwentyEightDayContract = true
                }
            );
        }
    }

    private void AddSyntheticFlavoredArtisanContracts(
        List<ExchangeContractSpec> contracts,
        List<MarketPriceTableEntry> marketEntries
    )
    {
        HashSet<string> existingKeys = new(
            contracts.Select(c => c.MarketCommodityKey)
                .Concat(marketEntries.Select(e => e.MarketCommodityKey)),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var pair in Game1.objectData)
        {
            string rawObjectId = pair.Key;
            string parentItemId = rawObjectId.StartsWith("(O)", StringComparison.Ordinal)
                ? rawObjectId
                : $"(O){rawObjectId}";

            Item? parentItem = TryCreateItem(parentItemId);
            if (parentItem is null)
                continue;

            int parentBasePrice;
            try
            {
                parentBasePrice = Math.Max(0, parentItem.sellToStorePrice(-1L));
            }
            catch
            {
                continue;
            }

            if (parentBasePrice < MarketCategoryResolver.MinimumMarketManagedBaseUnitPrice)
                continue;

            if (parentItem.Category == -79)
            {
                if (!ExcludedRawCommodityItemIds.Contains(parentItemId) || SyntheticFruitArtisanParentIds.Contains(parentItemId))
                {
                    this.TryAddSyntheticArtisanContract(
                        contracts,
                        existingKeys,
                        parentItem,
                        parentItemId,
                        preserveType: "Wine",
                        artisanItemId: "(O)348",
                        baseUnitPrice: parentBasePrice * 3
                    );

                    if (!ExcludedRawCommodityItemIds.Contains(parentItemId))
                    {
                        this.TryAddSyntheticArtisanContract(
                            contracts,
                            existingKeys,
                            parentItem,
                            parentItemId,
                            preserveType: "Jelly",
                            artisanItemId: "(O)344",
                            baseUnitPrice: parentBasePrice * 2 + 50
                        );
                    }
                }
            }
            else if (parentItem.Category == -75 && !ExcludedRawCommodityItemIds.Contains(parentItemId))
            {
                this.TryAddSyntheticArtisanContract(
                    contracts,
                    existingKeys,
                    parentItem,
                    parentItemId,
                    preserveType: "Juice",
                    artisanItemId: "(O)350",
                    baseUnitPrice: (int)Math.Round(parentBasePrice * 2.25, MidpointRounding.AwayFromZero)
                );

                this.TryAddSyntheticArtisanContract(
                    contracts,
                    existingKeys,
                    parentItem,
                    parentItemId,
                    preserveType: "Pickle",
                    artisanItemId: "(O)342",
                    baseUnitPrice: parentBasePrice * 2 + 50
                );
            }
        }
    }

    private void TryAddSyntheticArtisanContract(
        List<ExchangeContractSpec> contracts,
        HashSet<string> existingKeys,
        Item parentItem,
        string parentItemId,
        string preserveType,
        string artisanItemId,
        int baseUnitPrice
    )
    {
        string marketCommodityKey = $"Artisan:{preserveType}:{parentItemId}";

        if (!existingKeys.Add(marketCommodityKey))
            return;

        if (baseUnitPrice < MarketCategoryResolver.MinimumMarketManagedBaseUnitPrice)
            return;

        Item? artisanItem = TryCreateItem(artisanItemId);
        string displayName = BuildFlavoredArtisanDisplayName(parentItem.DisplayName, artisanItem?.DisplayName ?? preserveType);

        MarketCategoryResult category = new()
        {
            Category = "FlavoredArtisan",
            IsMarketManaged = true,
            IsFlavoredArtisan = true,
            IsGenericFlavoredArtisanTemplate = false,
            MarketCommodityKey = marketCommodityKey,
            ParentItemId = parentItemId,
            ExclusionReason = string.Empty
        };

        MarketPriceFactorBreakdown factors = this.marketPriceService.GetCurrentMarketFactorBreakdown(
            category,
            marketCommodityKey,
            artisanItem
        );

        int marketUnitPrice = this.marketPriceService.CalculateMarketUnitPrice(
            baseUnitPrice,
            factors.DailyMultiplier
        );
        int contractValue = marketUnitPrice * ExchangeService.DefaultQuantityPerLot;

        contracts.Add(
            new ExchangeContractSpec
            {
                MarketCommodityKey = marketCommodityKey,
                ItemId = artisanItemId,
                ParentItemId = parentItemId,
                DisplayName = displayName,
                Category = "FlavoredArtisan",
                MarketUnitPrice = marketUnitPrice,
                QuantityPerLot = ExchangeService.DefaultQuantityPerLot,
                ContractValuePerLot = contractValue,
                InitialMarginRequiredPerLot = (int)Math.Ceiling(contractValue * ExchangeService.InitialMarginRate),
                MaintenanceMarginRequiredPerLot = (int)Math.Ceiling(contractValue * ExchangeService.MaintenanceMarginRate),
                SupportsSevenDayContract = true,
                SupportsFourteenDayContract = true,
                SupportsTwentyEightDayContract = true
            }
        );
    }

    private static string ResolveExchangeCategory(MarketPriceTableEntry entry, Item? item)
    {
        if (IsFlavoredArtisan(entry))
            return "FlavoredArtisan";

        if (item is null)
            return "Unknown";

        return item.Category switch
        {
            -26 => "ArtisanGoods",
            -75 => "Vegetable",
            -79 => "Fruit",
            _ => "Unknown"
        };
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

    private static bool IsNakedFlavoredArtisanTemplate(string displayName, string? parentItemId)
    {
        return string.IsNullOrWhiteSpace(parentItemId)
            && !string.IsNullOrWhiteSpace(displayName)
            && NakedFlavoredArtisanTemplateNames.Contains(displayName.Trim());
    }

    private static bool SupportsSevenDayRawCrop(string itemId)
    {
        int? growthDays = TryGetCropGrowthDaysForHarvestItem(itemId);
        return growthDays.HasValue && growthDays.Value <= 7;
    }

    private static int? TryGetCropGrowthDaysForHarvestItem(string itemId)
    {
        string normalizedItemId = NormalizeObjectId(itemId);

        object? cropData = typeof(Game1).GetField("cropData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null)
            ?? typeof(Game1).GetProperty("cropData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);

        if (cropData is not IEnumerable enumerable)
            return null;

        foreach (object? entry in enumerable)
        {
            if (entry is null)
                continue;

            object? crop = entry.GetType().GetProperty("Value")?.GetValue(entry) ?? entry;
            if (crop is null)
                continue;

            object? harvestValue = crop.GetType().GetProperty("HarvestItemId")?.GetValue(crop)
                ?? crop.GetType().GetField("HarvestItemId")?.GetValue(crop);

            string? harvestItemId = harvestValue?.ToString();
            if (!string.Equals(NormalizeObjectId(harvestItemId), normalizedItemId, StringComparison.OrdinalIgnoreCase))
                continue;

            object? daysValue = crop.GetType().GetProperty("DaysInPhase")?.GetValue(crop)
                ?? crop.GetType().GetField("DaysInPhase")?.GetValue(crop);

            if (daysValue is not IEnumerable days)
                return null;

            int total = 0;
            foreach (object? day in days)
            {
                if (day is null)
                    continue;

                if (int.TryParse(day.ToString(), out int value))
                    total += value;
            }

            return total > 0 ? total : null;
        }

        return null;
    }

    private static string NormalizeObjectId(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        string trimmed = itemId.Trim();
        return trimmed.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : "(O)" + trimmed;
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

    private static int GetCategorySortOrder(string category)
    {
        return category switch
        {
            "FlavoredArtisan" => 0,
            "ArtisanGoods" => 1,
            "Vegetable" => 2,
            "Fruit" => 3,
            _ => 9
        };
    }

    private static string BuildFlavoredArtisanDisplayName(string parentName, string artisanName)
    {
        if (string.IsNullOrWhiteSpace(parentName))
            return artisanName;

        if (string.IsNullOrWhiteSpace(artisanName))
            return parentName;

        bool parentLooksCjk = parentName.Any(c => c >= '\u3400' && c <= '\u9fff');
        bool artisanLooksCjk = artisanName.Any(c => c >= '\u3400' && c <= '\u9fff');

        return parentLooksCjk || artisanLooksCjk
            ? parentName + artisanName
            : parentName + " " + artisanName;
    }
}
