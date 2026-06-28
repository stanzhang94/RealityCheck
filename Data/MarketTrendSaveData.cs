using System.Collections.Generic;
using RealityCheck.Models;

namespace RealityCheck.Data;

public class MarketTrendSaveData
{
    public Dictionary<string, MarketTrendItemState> TrendStates { get; set; } = new();

    public Dictionary<string, List<MarketPriceHistoryPoint>> PriceHistory { get; set; } = new();
}
