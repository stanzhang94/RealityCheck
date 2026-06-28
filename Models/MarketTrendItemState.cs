namespace RealityCheck.Models;

public class MarketTrendItemState
{
    public string MarketCommodityKey { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public double LongTermFactor { get; set; } = 1.0;

    public double TodayTrendChange { get; set; } = 1.0;

    public MarketTrendMode TrendMode { get; set; } = MarketTrendMode.Flat;

    public int DaysRemaining { get; set; } = 0;

    public string LastUpdatedDateKey { get; set; } = string.Empty;

    public int LastUpdatedDateIndex { get; set; } = 0;

    public double LastFinalFactor { get; set; } = 1.0;
}
