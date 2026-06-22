using RealityCheck.Models;

namespace RealityCheck.Data;

public class SaveData
{
    public List<LedgerEntry> Ledger { get; set; } = new();
}