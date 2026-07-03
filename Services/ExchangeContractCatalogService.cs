using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        "(O)342", // Pickles: generic template without parent crop.
        "342",
        "(O)344", // Jelly: generic template without parent fruit.
        "344",
        "(O)348", // Wine: generic template without parent fruit.
        "348",
        "(O)350", // Juice: generic template without parent vegetable.
        "350",
        "(O)SmokedFish",
        "SmokedFish",
        "(O)DriedMushrooms",
        "DriedMushrooms",
        "(O)DriedFruit",
        "DriedFruit"
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
        Item? item = CreateDisplayItem(entry) ?? entry.IconItem ?? TryCreateItem(entry.ItemId);
        string category = ResolveExchangeCategory(entry, item);
        bool isRawCrop = category is "Fruit" or "Vegetable";
        int marketUnitPrice = Math.Max(0, (int)Math.Round(entry.MarketUnitPrice));
        int contractValue = marketUnitPrice * ExchangeService.DefaultQuantityPerLot;

        return new ExchangeContractSpec
        {
            MarketCommodityKey = entry.MarketCommodityKey,
            ItemId = entry.ItemId,
            ParentItemId = entry.ParentItemId,
            DisplayName = ResolveDisplayName(entry, item),
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

    private static string ResolveDisplayName(MarketPriceTableEntry entry, Item? item)
    {
        if (item is not null && !string.IsNullOrWhiteSpace(item.DisplayName))
            return item.DisplayName;

        return entry.ItemName;
    }

    private static Item? CreateDisplayItem(MarketPriceTableEntry entry)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.ItemId))
            return null;

        Item? item = TryCreateItem(entry.ItemId);
        if (item is null)
            return null;

        ApplyArtisanIdentityToItem(item, entry.MarketCommodityKey, entry.ParentItemId);
        return item;
    }

    private static void ApplyArtisanIdentityToItem(Item item, string marketCommodityKey, string parentItemId)
    {
        if (item is null || string.IsNullOrWhiteSpace(parentItemId) || string.IsNullOrWhiteSpace(marketCommodityKey))
            return;

        if (!marketCommodityKey.StartsWith("Artisan:", StringComparison.OrdinalIgnoreCase))
            return;

        string? preserveType = TryReadPreserveTypeFromMarketKey(marketCommodityKey);
        int? parentIndex = TryReadObjectIndex(parentItemId);

        if (string.IsNullOrWhiteSpace(preserveType) || parentIndex is null)
            return;

        TryWriteMemberValue(item, "preserve", preserveType);
        TryWriteMemberValue(item, "preservedParentSheetIndex", parentIndex.Value);
    }

    private static string? TryReadPreserveTypeFromMarketKey(string marketCommodityKey)
    {
        string[] parts = marketCommodityKey.Split(':');
        return parts.Length >= 3 ? parts[1] : null;
    }

    private static int? TryReadObjectIndex(string qualifiedItemId)
    {
        string digits = new((qualifiedItemId ?? string.Empty).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int parsed) ? parsed : null;
    }

    private static void TryWriteMemberValue(Item item, string memberName, object value)
    {
        Type? type = item.GetType();

        while (type is not null)
        {
            FieldInfo? field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
            {
                if (TryWriteNetFieldValue(field.GetValue(item), value))
                    return;

                object? converted = TryConvertMemberValue(field.FieldType, value);
                if (converted is not null)
                {
                    field.SetValue(item, converted);
                    return;
                }
            }

            PropertyInfo? property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                object? currentValue = null;
                try { currentValue = property.GetValue(item); } catch { }

                if (TryWriteNetFieldValue(currentValue, value))
                    return;

                if (property.CanWrite)
                {
                    object? converted = TryConvertMemberValue(property.PropertyType, value);
                    if (converted is not null)
                    {
                        property.SetValue(item, converted);
                        return;
                    }
                }
            }

            type = type.BaseType;
        }
    }

    private static bool TryWriteNetFieldValue(object? target, object value)
    {
        if (target is null)
            return false;

        PropertyInfo? valueProperty = target.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty is null || !valueProperty.CanWrite)
            return false;

        object? converted = TryConvertMemberValue(valueProperty.PropertyType, value);
        if (converted is null)
            return false;

        valueProperty.SetValue(target, converted);
        return true;
    }

    private static object? TryConvertMemberValue(Type targetType, object value)
    {
        Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (effectiveType == typeof(string))
                return value.ToString();

            if (effectiveType == typeof(int))
                return Convert.ToInt32(value);

            if (effectiveType.IsEnum)
            {
                string? text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text) && Enum.TryParse(effectiveType, text, ignoreCase: true, out object? parsed))
                    return parsed;
            }

            if (effectiveType.IsInstanceOfType(value))
                return value;
        }
        catch
        {
            return null;
        }

        return null;
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
