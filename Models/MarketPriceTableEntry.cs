using StardewValley;

namespace RealityCheck.Models;

public class MarketPriceTableEntry
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public int BaseUnitPrice { get; set; }

    public double MarketMultiplier { get; set; }

    public double MarketUnitPrice { get; set; }

    public double Difference { get; set; }

    public Item? IconItem { get; set; }
}
