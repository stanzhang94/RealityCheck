using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck.Events;

public class ExpenseEvents
{
    private readonly LedgerService ledgerService;
    private readonly IMonitor monitor;

    private int? lastMoney;

    public ExpenseEvents(LedgerService ledgerService, IMonitor monitor)
    {
        this.ledgerService = ledgerService;
        this.monitor = monitor;
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.lastMoney = Game1.player.Money;
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.lastMoney = Game1.player.Money;
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        int currentMoney = Game1.player.Money;

        if (!this.lastMoney.HasValue)
        {
            this.lastMoney = currentMoney;
            return;
        }

        int difference = currentMoney - this.lastMoney.Value;

        if (difference < 0)
        {
            int expenseAmount = -difference;

            this.ledgerService.AddExpense(
                "Base Game",
                "Base Game Expenses",
                1,
                expenseAmount
            );

            this.monitor.Log(
                $"Expense detected: {expenseAmount}g",
                LogLevel.Info
            );
        }

        this.lastMoney = currentMoney;
    }
}