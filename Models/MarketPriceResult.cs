namespace RealityCheck.Models;

public class MarketPriceResult
{
    public string ItemName { get; set; } = "";

    public string ItemId { get; set; } = "";

    public int Quantity { get; set; }

    public int BaseUnitPrice { get; set; }

    public int BaseTotal { get; set; }

    public double MarketMultiplier { get; set; } = 1.0;

    public double DailyMultiplier { get; set; } = 1.0;

    public double TotalMultiplier { get; set; } = 1.0;

    public int MarketTotal { get; set; }

    public double MarketUnitPrice { get; set; }

    public int Difference => this.MarketTotal - this.BaseTotal;

    public string Direction
    {
        get
        {
            if (this.Difference > 0)
                return "Up";

            if (this.Difference < 0)
                return "Down";

            return "Flat";
        }
    }
}
