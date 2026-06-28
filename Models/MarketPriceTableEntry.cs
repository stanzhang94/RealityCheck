using StardewValley;

namespace RealityCheck.Models;

public class MarketPriceTableEntry
{
    public string ItemId { get; set; } = string.Empty;

    public string MarketCommodityKey { get; set; } = string.Empty;

    public string ParentItemId { get; set; } = string.Empty;

    public bool IsDiscoveredArtisan { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int BaseUnitPrice { get; set; }

    public double MarketMultiplier { get; set; }

    public double DailyMultiplier { get; set; } = 1.0;

    public double TotalMultiplier { get; set; } = 1.0;

    public double MarketUnitPrice { get; set; }

    public double Difference { get; set; }

    public Item? IconItem { get; set; }
}
