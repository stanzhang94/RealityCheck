using System;
using RealityCheck.Models;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck.Events;

public class ExpenseEvents
{
    private const int DailyHealthInsurancePremium = 20;
    private const double HealthInsuranceCoverageRate = 0.50;

    private const int CollapseDetectionWindowTicks = 3600;

    private readonly LedgerService ledgerService;
    private readonly HealthInsuranceNoticeService healthInsuranceNoticeService;
    private readonly IMonitor monitor;

    private int? lastMoney;

    private int recentlyCollapsedTicks = 0;

    public ExpenseEvents(
        LedgerService ledgerService,
        HealthInsuranceNoticeService healthInsuranceNoticeService,
        IMonitor monitor
    )
    {
        this.ledgerService = ledgerService;
        this.healthInsuranceNoticeService = healthInsuranceNoticeService;
        this.monitor = monitor;
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.lastMoney = Game1.player.Money;
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
        this.recentlyCollapsedTicks = 0;

        this.ProcessPendingHealthInsuranceClaims();

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
            this.HandleMoneyDecrease(-difference);
        }
        else if (difference > 0)
        {
            this.HandleMoneyIncrease(difference);
        }

        this.lastMoney = Game1.player.Money;
    }

    private void HandleMoneyDecrease(int rawExpenseAmount)
    {
        int expenseAmount = rawExpenseAmount;

        int suppressedAmount = this.ledgerService.ConsumeSuppressedExpenseAmount(
            expenseAmount
        );

        if (suppressedAmount > 0)
        {
            expenseAmount -= suppressedAmount;

            this.monitor.Log(
                $"Suppressed {suppressedAmount}g from Base Game Expenses.",
                LogLevel.Info
            );
        }

        if (expenseAmount <= 0)
            return;

        if (this.IsMedicalCollapseExpense())
        {
            this.ledgerService.AddExpense(
                "Harvey Medical Clinic",
                "Medical Expenses",
                1,
                expenseAmount
            );

            this.monitor.Log(
                $"Medical expense recorded: {expenseAmount}g",
                LogLevel.Info
            );

            this.CreatePendingHealthInsuranceClaim(expenseAmount);

            this.recentlyCollapsedTicks = 0;

            return;
        }

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
    }

    private void HandleMoneyIncrease(int rawIncomeAmount)
    {
        int unclassifiedIncomeAmount = rawIncomeAmount;

        int suppressedAmount = this.ledgerService.ConsumeSuppressedIncomeAmount(
            unclassifiedIncomeAmount
        );

        if (suppressedAmount > 0)
        {
            unclassifiedIncomeAmount -= suppressedAmount;

            this.monitor.Log(
                $"Suppressed {suppressedAmount}g from Unclassified Income.",
                LogLevel.Info
            );
        }

        if (unclassifiedIncomeAmount > 0)
        {
            this.ledgerService.AddUnclassifiedIncome(unclassifiedIncomeAmount);

            this.monitor.Log(
                $"Unclassified income recorded: {unclassifiedIncomeAmount}g",
                LogLevel.Info
            );
        }

        int outstandingBalance = this.ledgerService.GetOutstandingBalance();

        if (outstandingBalance <= 0)
            return;

        int currentMoney = Game1.player.Money;

        int recoveryAmount = Math.Min(
            Math.Min(rawIncomeAmount, currentMoney),
            outstandingBalance
        );

        if (recoveryAmount <= 0)
            return;

        Game1.player.Money -= recoveryAmount;

        int actualRecovered = this.ledgerService.ReduceOutstandingBalance(
            recoveryAmount
        );

        this.monitor.Log(
            $"Outstanding balance recovered: {actualRecovered}g",
            LogLevel.Info
        );

        Game1.showGlobalMessage(
            $"Outstanding balance recovered: -{actualRecovered}g"
        );
    }

    private bool IsMedicalCollapseExpense()
    {
        return this.recentlyCollapsedTicks > 0;
    }

    private void CreatePendingHealthInsuranceClaim(int medicalExpenseAmount)
    {
        int coverageAmount = this.CalculateHealthInsuranceCoverage(
            medicalExpenseAmount
        );

        if (coverageAmount <= 0)
            return;

        var claim = new HealthInsuranceClaim
        {
            MedicalExpenseYear = Game1.year,
            MedicalExpenseSeason = Game1.currentSeason,
            MedicalExpenseDay = Game1.dayOfMonth,
            MedicalExpenseAmount = medicalExpenseAmount,
            CoverageAmount = coverageAmount,
            Processed = false
        };

        this.ledgerService.AddHealthInsuranceClaim(claim);

        this.monitor.Log(
            $"Health insurance claim created: medical expense {medicalExpenseAmount}g, pending coverage {coverageAmount}g",
            LogLevel.Info
        );
    }

    private void ProcessPendingHealthInsuranceClaims()
    {
        foreach (HealthInsuranceClaim claim in this.ledgerService.GetPendingHealthInsuranceClaims())
        {
            if (!this.ShouldProcessHealthInsuranceClaimToday(claim))
                continue;

            this.ApplyHealthInsuranceCoverage(claim);
        }
    }

    private bool ShouldProcessHealthInsuranceClaimToday(
        HealthInsuranceClaim claim
    )
    {
        if (claim.MedicalExpenseYear != Game1.year)
            return true;

        if (claim.MedicalExpenseSeason != Game1.currentSeason)
            return true;

        return claim.MedicalExpenseDay != Game1.dayOfMonth;
    }

    private void ApplyHealthInsuranceCoverage(
        HealthInsuranceClaim claim
    )
    {
        if (claim.CoverageAmount <= 0)
            return;

        this.ledgerService.SuppressNextIncomeAmount(claim.CoverageAmount);

        Game1.player.Money += claim.CoverageAmount;

        this.ledgerService.AddExpenseOffset(
            "Harvey Medical Clinic",
            "Health Insurance Coverage",
            claim.CoverageAmount
        );

        this.ledgerService.MarkHealthInsuranceClaimProcessed(
            claim.Id,
            Game1.year,
            Game1.currentSeason,
            Game1.dayOfMonth
        );

        this.healthInsuranceNoticeService.DeliverHealthInsuranceClaimNotice(
            claim
        );

        this.monitor.Log(
            $"Health Insurance Coverage processed: +{claim.CoverageAmount}g for medical expense {claim.MedicalExpenseAmount}g",
            LogLevel.Info
        );
    }

    private int CalculateHealthInsuranceCoverage(
        int medicalExpenseAmount
    )
    {
        return (int)Math.Floor(
            medicalExpenseAmount * HealthInsuranceCoverageRate
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

        this.ledgerService.SuppressNextExpenseAmount(amount);

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
