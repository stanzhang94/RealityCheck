using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class LedgerService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private SaveData data = new();

    private const string SaveFilePath = "data/reality-check.json";

    public LedgerService(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public void Load()
    {
        this.data = this.helper.Data.ReadJsonFile<SaveData>(SaveFilePath) ?? new SaveData();

        this.monitor.Log($"Loaded {this.data.Ledger.Count} ledger entries.", LogLevel.Trace);
    }

    public void Save()
    {
        this.helper.Data.WriteJsonFile(SaveFilePath, this.data);
    }

public void AddIncome(string source, string itemName, int quantity, int amount, string itemId = "")
    {
        var entry = new LedgerEntry
        {
            Year = Game1.year,
            Season = Game1.currentSeason,
            Day = Game1.dayOfMonth,
            Type = "Income",
            Source = source,
            ItemName = itemName,
            ItemId = itemId,
            Quantity = quantity,
            Amount = amount,
            TimeOfDay = Game1.timeOfDay
        };

        this.data.Ledger.Add(entry);
        this.Save();

        this.monitor.Log($"Income recorded: {itemName} x{quantity} = {amount}g from {source}", LogLevel.Trace);
    }

    public IReadOnlyList<LedgerEntry> GetEntries()
    {
        return this.data.Ledger;
    }
        public void Clear()
    {
        this.data.Ledger.Clear();
        this.Save();

        this.monitor.Log(
            "Ledger cleared.",
            LogLevel.Info
        );
    }
}