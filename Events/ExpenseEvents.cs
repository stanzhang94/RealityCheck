using Microsoft.Xna.Framework.Input;
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

    private int suppressedExpenseAmount = 0;

    private bool wasHKeyDown = false;

    public ExpenseEvents(
        LedgerService ledgerService,
        IMonitor monitor
    )
    {
        this.ledgerService = ledgerService;
        this.monitor = monitor;
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.lastMoney = Game1.player.Money;
        this.suppressedExpenseAmount = 0;

        this.monitor.Log(
            $"Expense tracker initialized. Current money: {this.lastMoney}g",
            LogLevel.Trace
        );
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.lastMoney = Game1.player.Money;
        this.suppressedExpenseAmount = 0;

        this.monitor.Log(
            $"Expense tracker reset for new day. Current money: {this.lastMoney}g",
            LogLevel.Trace
        );
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.TrackMoneyChanges();

        this.HandleTestHealthInsuranceKey();
    }

    private void TrackMoneyChanges()
    {
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

            if (this.suppressedExpenseAmount > 0)
            {
                int suppressedAmount = Math.Min(
                    expenseAmount,
                    this.suppressedExpenseAmount
                );

                expenseAmount -= suppressedAmount;
                this.suppressedExpenseAmount -= suppressedAmount;

                this.monitor.Log(
                    $"Suppressed {suppressedAmount}g from Base Game Expenses.",
                    LogLevel.Trace
                );
            }

            if (expenseAmount > 0)
            {
                this.ledgerService.AddExpense(
                    "Base Game",
                    "Base Game Expenses",
                    1,
                    expenseAmount
                );

                this.monitor.Log(
                    $"Base Game expense detected: {expenseAmount}g",
                    LogLevel.Info
                );
            }
        }

        this.lastMoney = currentMoney;
    }

    private void HandleTestHealthInsuranceKey()
    {
        bool isHKeyDown = Keyboard.GetState().IsKeyDown(Keys.H);

        if (isHKeyDown && !this.wasHKeyDown)
        {
            if (Game1.activeClickableMenu == null)
            {
                this.ChargeModExpense(
                    "Health Insurance",
                    20
                );
            }
        }

        this.wasHKeyDown = isHKeyDown;
    }

    public void ChargeModExpense(
        string expenseName,
        int amount
    )
    {
        if (!Context.IsWorldReady)
            return;

        if (amount <= 0)
            return;

        if (Game1.player.Money < amount)
        {
            this.monitor.Log(
                $"Skipped mod-created expense '{expenseName}' because player does not have enough money.",
                LogLevel.Warn
            );

            return;
        }

        this.suppressedExpenseAmount += amount;

        Game1.player.Money -= amount;

        this.ledgerService.AddExpense(
            "Reality Check",
            expenseName,
            1,
            amount
        );

        this.monitor.Log(
            $"Mod-created expense charged: {expenseName} = {amount}g",
            LogLevel.Info
        );
    }
}