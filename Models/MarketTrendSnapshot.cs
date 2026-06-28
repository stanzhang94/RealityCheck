namespace RealityCheck.Models;

public class MarketTrendSnapshot
{
    public double LongTermFactor { get; set; } = 1.0;

    public double TodayTrendChange { get; set; } = 1.0;

    public MarketTrendMode TrendMode { get; set; } = MarketTrendMode.Flat;

    public int DaysRemaining { get; set; }
}
