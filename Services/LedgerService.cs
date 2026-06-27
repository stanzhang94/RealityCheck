using System;
using System.Collections.Generic;
using System.Linq;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class LedgerService
{
    private const string SaveDataKey = "save-data";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private SaveData data;

    public const string UnclassifiedIncomeItemId = "RC.UnclassifiedIncome";

    private int suppressedExpenseAmount = 0;
    private int suppressedIncomeAmount = 0;

    private string? loadedSaveId = null;

    public LedgerService(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;

        this.data = new SaveData();

        this.Load();
    }

    public void Load()
    {
        if (!Context.IsWorldReady)
        {
            this.data = new SaveData();
            this.loadedSaveId = null;
            this.suppressedExpenseAmount = 0;
            this.suppressedIncomeAmount = 0;

            this.monitor.Log(
                "Ledger initialized before save data was ready.",
                LogLevel.Trace
            );

            return;
        }

        this.data =
            this.helper.Data.ReadSaveData<SaveData>(SaveDataKey)
            ?? new SaveData();

        this.EnsureCollections();

        this.loadedSaveId = this.GetCurrentSaveId();
        this.suppressedExpenseAmount = 0;
        this.suppressedIncomeAmount = 0;

        this.monitor.Log(
            $"Ledger loaded from save data for save {this.loadedSaveId}.",
            LogLevel.Trace
        );
    }

    public List<LedgerEntry> GetEntries()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.Ledger;
    }

    public List<TaxRecord> GetTaxRecords()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.TaxRecords;
    }

    public List<PropertyTaxDailyAssessment> GetPropertyTaxDailyAssessments()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.PropertyTaxDailyAssessments;
    }

    public List<BusinessPropertyTaxDailyAssessment> GetBusinessPropertyTaxDailyAssessments()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.BusinessPropertyTaxDailyAssessments;
    }

    public List<HealthInsuranceClaim> GetHealthInsuranceClaims()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.HealthInsuranceClaims;
    }

    public List<HealthInsuranceClaim> GetPendingHealthInsuranceClaims()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.HealthInsuranceClaims
            .Where(c => !c.Processed)
            .ToList();
    }

    public bool HasPropertyTaxDailyAssessment(
        int year,
        string season,
        int day
    )
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.PropertyTaxDailyAssessments.Any(a =>
            a.Year == year
            && a.Season == season
            && a.Day == day
        );
    }

    public bool HasBusinessPropertyTaxDailyAssessment(
        int year,
        string season,
        int day
    )
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.BusinessPropertyTaxDailyAssessments.Any(a =>
            a.Year == year
            && a.Season == season
            && a.Day == day
        );
    }

    public void AddPropertyTaxDailyAssessment(
        PropertyTaxDailyAssessment assessment
    )
    {
        this.EnsureLoadedForCurrentSave();

        if (this.HasPropertyTaxDailyAssessment(
            assessment.Year,
            assessment.Season,
            assessment.Day
        ))
        {
            return;
        }

        this.data.PropertyTaxDailyAssessments.Add(assessment);

        this.monitor.Log(
            $"Property Tax daily assessment added: Year {assessment.Year} {assessment.Season} {assessment.Day}, total {assessment.TotalPropertyTaxAmount:0.##}g",
            LogLevel.Trace
        );
    }

    public void AddBusinessPropertyTaxDailyAssessment(
        BusinessPropertyTaxDailyAssessment assessment
    )
    {
        this.EnsureLoadedForCurrentSave();

        if (this.HasBusinessPropertyTaxDailyAssessment(
            assessment.Year,
            assessment.Season,
            assessment.Day
        ))
        {
            return;
        }

        this.data.BusinessPropertyTaxDailyAssessments.Add(assessment);

        this.monitor.Log(
            $"Business Property Tax daily assessment added: Year {assessment.Year} {assessment.Season} {assessment.Day}, total {assessment.TotalBusinessPropertyTaxAmount}g",
            LogLevel.Trace
        );
    }

    public void AddTaxRecord(TaxRecord record)
    {
        this.EnsureLoadedForCurrentSave();

        if (record.TotalTaxAmount <= 0)
            return;

        this.data.TaxRecords.Add(record);

        this.monitor.Log(
            $"Tax record added in memory: Year {record.Year} {record.Season} Week {record.WeekNumber}, total tax {record.TotalTaxAmount}g",
            LogLevel.Trace
        );
    }

    public void AddHealthInsuranceClaim(HealthInsuranceClaim claim)
    {
        this.EnsureLoadedForCurrentSave();

        if (claim.MedicalExpenseAmount <= 0 || claim.CoverageAmount <= 0)
            return;

        if (string.IsNullOrWhiteSpace(claim.Id))
            claim.Id = Guid.NewGuid().ToString("N");

        this.data.HealthInsuranceClaims.Add(claim);

        this.monitor.Log(
            $"Health insurance claim added in memory: medical expense {claim.MedicalExpenseAmount}g, coverage {claim.CoverageAmount}g",
            LogLevel.Trace
        );
    }

    public void MarkHealthInsuranceClaimProcessed(
        string claimId,
        int processedYear,
        string processedSeason,
        int processedDay
    )
    {
        this.EnsureLoadedForCurrentSave();

        HealthInsuranceClaim? claim = this.data.HealthInsuranceClaims
            .FirstOrDefault(c => c.Id == claimId);

        if (claim == null)
            return;

        claim.Processed = true;
        claim.ProcessedYear = processedYear;
        claim.ProcessedSeason = processedSeason;
        claim.ProcessedDay = processedDay;
    }

    public int GetOutstandingBalance()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.OutstandingBalance;
    }

    public int GetEffectiveBalance()
    {
        this.EnsureLoadedForCurrentSave();

        return Game1.player.Money - this.data.OutstandingBalance;
    }

    public bool IsTaxNoticeSigned(string noticeId)
    {
        this.EnsureLoadedForCurrentSave();

        if (string.IsNullOrWhiteSpace(noticeId))
            return false;

        return this.data.SignedTaxNoticeIds.Contains(noticeId);
    }

    public void SignTaxNotice(string noticeId)
    {
        this.EnsureLoadedForCurrentSave();

        if (string.IsNullOrWhiteSpace(noticeId))
            return;

        if (this.data.SignedTaxNoticeIds.Contains(noticeId))
            return;

        this.data.SignedTaxNoticeIds.Add(noticeId);

        this.Save();

        this.monitor.Log(
            $"Tax notice signed: {noticeId}",
            LogLevel.Info
        );
    }

    public void AddIncome(
        string source,
        string itemName,
        int quantity,
        int amount,
        string itemId = ""
    )
    {
        this.EnsureLoadedForCurrentSave();

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

    public void AddUnclassifiedIncome(int amount)
    {
        this.AddIncome(
            "Unknown Channel",
            I18n.Get("income.unclassified_channel"),
            1,
            amount,
            UnclassifiedIncomeItemId
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
        this.EnsureLoadedForCurrentSave();

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
        this.EnsureLoadedForCurrentSave();

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

    public void ChargeObligation(
        string source,
        string category,
        int amount
    )
    {
        this.EnsureLoadedForCurrentSave();

        if (amount <= 0)
            return;

        this.AddExpense(
            source,
            category,
            1,
            amount
        );

        int availableMoney = Game1.player.Money;

        int paidAmount = Math.Min(
            availableMoney,
            amount
        );

        int unpaidAmount = amount - paidAmount;

        if (paidAmount > 0)
        {
            this.SuppressNextExpenseAmount(paidAmount);

            Game1.player.Money -= paidAmount;

            this.monitor.Log(
                $"Obligation paid immediately: {category} = {paidAmount}g",
                LogLevel.Info
            );
        }

        if (unpaidAmount > 0)
        {
            this.AddOutstandingBalance(unpaidAmount);

            this.monitor.Log(
                $"Outstanding balance increased by {unpaidAmount}g from {category}.",
                LogLevel.Info
            );

            Game1.showGlobalMessage(
                $"Unpaid obligation added: -{unpaidAmount}g"
            );
        }
    }

    public void AddOutstandingBalance(int amount)
    {
        this.EnsureLoadedForCurrentSave();

        if (amount <= 0)
            return;

        this.data.OutstandingBalance += amount;
    }

    public int ReduceOutstandingBalance(int amount)
    {
        this.EnsureLoadedForCurrentSave();

        if (amount <= 0)
            return 0;

        int paidAmount = Math.Min(
            amount,
            this.data.OutstandingBalance
        );

        this.data.OutstandingBalance -= paidAmount;

        return paidAmount;
    }

    public void SuppressNextExpenseAmount(int amount)
    {
        if (amount <= 0)
            return;

        this.suppressedExpenseAmount += amount;
    }

    public int ConsumeSuppressedExpenseAmount(int expenseAmount)
    {
        if (expenseAmount <= 0)
            return 0;

        if (this.suppressedExpenseAmount <= 0)
            return 0;

        int consumedAmount = Math.Min(
            expenseAmount,
            this.suppressedExpenseAmount
        );

        this.suppressedExpenseAmount -= consumedAmount;

        return consumedAmount;
    }

    public void SuppressNextIncomeAmount(int amount)
    {
        if (amount <= 0)
            return;

        this.suppressedIncomeAmount += amount;
    }

    public int ConsumeSuppressedIncomeAmount(int incomeAmount)
    {
        if (incomeAmount <= 0)
            return 0;

        if (this.suppressedIncomeAmount <= 0)
            return 0;

        int consumedAmount = Math.Min(
            incomeAmount,
            this.suppressedIncomeAmount
        );

        this.suppressedIncomeAmount -= consumedAmount;

        return consumedAmount;
    }

    public void Clear()
    {
        this.EnsureLoadedForCurrentSave();

        this.data.Ledger.Clear();
        this.data.TaxRecords.Clear();
        this.data.PropertyTaxDailyAssessments.Clear();
        this.data.BusinessPropertyTaxDailyAssessments.Clear();
        this.data.SignedTaxNoticeIds.Clear();
        this.data.HealthInsuranceClaims.Clear();
        this.data.OutstandingBalance = 0;
        this.suppressedExpenseAmount = 0;
        this.suppressedIncomeAmount = 0;

        this.Save();

        this.monitor.Log(
            "Ledger, tax records, tax assessments, signed tax notices, health insurance claims, and outstanding balance cleared for current save.",
            LogLevel.Info
        );
    }

    public void Save()
    {
        if (!Context.IsWorldReady)
            return;

        this.EnsureLoadedForCurrentSave();

        this.helper.Data.WriteSaveData(
            SaveDataKey,
            this.data
        );

        this.monitor.Log(
            $"Ledger saved to save data for save {this.loadedSaveId}.",
            LogLevel.Trace
        );
    }

    private void EnsureLoadedForCurrentSave()
    {
        if (!Context.IsWorldReady)
            return;

        string currentSaveId = this.GetCurrentSaveId();

        if (this.loadedSaveId == currentSaveId)
            return;

        this.Load();
    }

    private void EnsureCollections()
    {
        this.data.Ledger ??= new List<LedgerEntry>();
        this.data.TaxRecords ??= new List<TaxRecord>();
        this.data.PropertyTaxDailyAssessments ??= new List<PropertyTaxDailyAssessment>();
        this.data.BusinessPropertyTaxDailyAssessments ??= new List<BusinessPropertyTaxDailyAssessment>();
        this.data.SignedTaxNoticeIds ??= new List<string>();
        this.data.HealthInsuranceClaims ??= new List<HealthInsuranceClaim>();
    }

    private string GetCurrentSaveId()
    {
        return Game1.uniqueIDForThisGame.ToString();
    }
}
