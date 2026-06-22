namespace RealityCheck.Models;

public class LedgerEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public int Year { get; set; }

    public string Season { get; set; } = "";

    public int Day { get; set; }

    public string Type { get; set; } = "Income";

    public string Source { get; set; } = "";

    public string ItemName { get; set; } = "";

    public int Quantity { get; set; }

    public int Amount { get; set; }

    public int TimeOfDay { get; set; }
}