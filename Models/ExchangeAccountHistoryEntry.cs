namespace RealityCheck.Models;

public class ExchangeAccountHistoryEntry
{
    public string Id { get; set; } = string.Empty;

    public int Year { get; set; } = 0;

    public string Season { get; set; } = string.Empty;

    public int Day { get; set; } = 0;

    public int TimeOfDay { get; set; } = 0;

    public string Type { get; set; } = string.Empty;

    public string ContractId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Amount { get; set; } = 0;

    public int CashBalanceAfter { get; set; } = 0;

    public int DebtAfter { get; set; } = 0;
}
