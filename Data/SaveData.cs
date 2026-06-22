using RealityCheck.Models;

namespace RealityCheck.Data;

public class SaveData
{
    public List<LedgerEntry> Ledger { get; set; } = new();

    // Positive internal value.
    // UI should display this as negative debt.
    public int OutstandingBalance { get; set; } = 0;
}