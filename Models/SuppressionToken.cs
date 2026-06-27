namespace RealityCheck.Models;

public class SuppressionToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int Amount { get; set; }

    public int RemainingAmount { get; set; }

    public string Type { get; set; } = "Income";

    public string Reason { get; set; } = "";

    public string Source { get; set; } = "";

    public string TransactionId { get; set; } = "";

    public int Year { get; set; }

    public string Season { get; set; } = "";

    public int Day { get; set; }

    public int TimeOfDay { get; set; }
}
