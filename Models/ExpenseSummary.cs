namespace RealityCheck.Models;

public class ExpenseSummary
{
    public string Category { get; set; } = "";

    // Signed amount for UI:
    // Negative = expense
    // Positive = expense offset / coverage
    public int Amount { get; set; }
}