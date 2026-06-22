using System;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck.Events;

public class ExpenseEvents
{
    private const int DailyHealthInsurancePremium = 20;
    private const double HealthInsuranceCoverageRate = 0.50;

    // About 60 seconds at 60 ticks/second.
    // This gives the game enough time to run collapse animation,
    // rescue dialogue, and money penalty.
    private const int CollapseDetectionWindowTicks = 3600;

    private readonly LedgerService ledgerService;
    private readonly IMonitor monitor;

    private int? lastMoney;

    private int suppressedExpenseAmount = 0;

    private int recentlyCollapsedTicks = 0;

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
        this.recentlyCollapsedTicks = 0;

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
        this.recentlyCollapsedTicks = 0;

        this.monitor.Log(
            $"Expense tracker reset for new day. Current money: {this.lastMoney}g",
            LogLevel.Trace
        );
    }

    public void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.ChargeDailyHealthInsurancePremium();
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.TrackCollapseFromLowHealth();

        this.TrackMoneyChanges();

        this.TickCollapseWindow();
    }

private void TrackCollapseFromLowHealth()
{
    if (Game1.player.health <= 0)
    {
        if (this.recentlyCollapsedTicks <= 0)
        {
            this.monitor.Log(
                "Low-health collapse detected. Medical expense window opened.",
                LogLevel.Info
            );
        }

        this.recentlyCollapsedTicks = CollapseDetectionWindowTicks;
    }
}
    private void TickCollapseWindow()
    {
        if (this.recentlyCollapsedTicks > 0)
            this.recentlyCollapsedTicks--;
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
            int originalExpenseAmount = -difference;
            int expenseAmount = originalExpenseAmount;

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
                    LogLevel.Info
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
                    $"Base Game expense recorded: {expenseAmount}g",
                    LogLevel.Info
                );

                if (this.IsMedicalCollapseExpense())
                {
                    this.ApplyHealthInsuranceCoverage(expenseAmount);

                    this.recentlyCollapsedTicks = 0;
                }
            }
        }

        this.lastMoney = Game1.player.Money;
    }

    private bool IsMedicalCollapseExpense()
    {
        return this.recentlyCollapsedTicks > 0;
    }

    private void ApplyHealthInsuranceCoverage(int medicalExpenseAmount)
    {
        int coverageAmount = (int)Math.Floor(
            medicalExpenseAmount * HealthInsuranceCoverageRate
        );

        if (coverageAmount <= 0)
            return;

        Game1.player.Money += coverageAmount;

        this.ledgerService.AddExpenseOffset(
            "Reality Check",
            "Health Insurance Coverage",
            coverageAmount
        );

        this.ShowHealthInsuranceCoverageMessage(
            coverageAmount,
            medicalExpenseAmount
        );

        this.monitor.Log(
            $"Health Insurance Coverage applied: +{coverageAmount}g for medical expense {medicalExpenseAmount}g",
            LogLevel.Info
        );
    }

private void ShowHealthInsuranceCoverageMessage(
    int coverageAmount,
    int medicalExpenseAmount
)
{
    int coveragePercent = (int)(HealthInsuranceCoverageRate * 100);

    Game1.showGlobalMessage(
        $"Dear member of the Harvey Medical Insurance Fund, your {coveragePercent}% coverage has been paid: +{coverageAmount}g"
    );
}

    private void ChargeDailyHealthInsurancePremium()
    {
        this.ChargeModExpense(
            "Health Insurance Premium",
            DailyHealthInsurancePremium
        );
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