using System.Collections.Generic;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class LedgerService
{
    private const string DataPath = "data/save-data.json";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private SaveData data;

    public LedgerService(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;

        this.data = new SaveData();

        this.Load();
    }

    public void Load()
    {
        this.data =
            this.helper.Data.ReadJsonFile<SaveData>(DataPath)
            ?? new SaveData();

        this.monitor.Log(
            "Ledger loaded from JSON.",
            LogLevel.Trace
        );
    }

    public List<LedgerEntry> GetEntries()
    {
        return this.data.Ledger;
    }

    public void AddIncome(
        string source,
        string itemName,
        int quantity,
        int amount,
        string itemId = ""
    )
    {
        if (amount <= 0)
            return;

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

        this.monitor.Log(
            $"Income recorded in memory: {itemName} x{quantity} = {amount}g from {source}",
            LogLevel.Trace
        );
    }

    public void AddExpense(
        string source,
        string itemName,
        int quantity,
        int amount,
        string itemId = ""
    )
    {
        if (amount <= 0)
            return;

        var entry = new LedgerEntry
        {
            Year = Game1.year,
            Season = Game1.currentSeason,
            Day = Game1.dayOfMonth,
            Type = "Expense",
            Source = source,
            ItemName = itemName,
            ItemId = itemId,
            Quantity = quantity,
            Amount = amount,
            TimeOfDay = Game1.timeOfDay
        };

        this.data.Ledger.Add(entry);

        this.monitor.Log(
            $"Expense recorded in memory: {itemName} x{quantity} = {amount}g from {source}",
            LogLevel.Trace
        );
    }

    public void AddExpenseOffset(
        string source,
        string itemName,
        int amount
    )
    {
        if (amount <= 0)
            return;

        var entry = new LedgerEntry
        {
            Year = Game1.year,
            Season = Game1.currentSeason,
            Day = Game1.dayOfMonth,
            Type = "ExpenseOffset",
            Source = source,
            ItemName = itemName,
            ItemId = "",
            Quantity = 1,
            Amount = amount,
            TimeOfDay = Game1.timeOfDay
        };

        this.data.Ledger.Add(entry);

        this.monitor.Log(
            $"Expense offset recorded in memory: {itemName} = +{amount}g from {source}",
            LogLevel.Trace
        );
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

    public void Save()
    {
        this.helper.Data.WriteJsonFile(
            DataPath,
            this.data
        );

        this.monitor.Log(
            "Ledger saved to JSON.",
            LogLevel.Trace
        );
    }
}