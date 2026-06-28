namespace RealityCheck.Models;

public class MarketPriceHistoryPoint
{
    public int DateIndex { get; set; }

    public int Year { get; set; }

    public string Season { get; set; } = string.Empty;

    public int Day { get; set; }

    public int BaseUnitPrice { get; set; }

    public int MarketUnitPrice { get; set; }

    public double DailyMultiplier { get; set; }

    public double TotalMultiplier { get; set; }
}
