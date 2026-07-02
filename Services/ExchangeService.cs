using System;
using System.Collections.Generic;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class ExchangeService
{
    private const string SaveDataKey = "exchange-data";

    public const double InitialMarginRate = 0.20;
    public const double MaintenanceMarginRate = 0.12;
    public const int DefaultQuantityPerLot = 100;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly LedgerService ledgerService;

    private ExchangeSaveData data;
    private string? loadedSaveId = null;

    public ExchangeService(
        IModHelper helper,
        IMonitor monitor,
        LedgerService ledgerService
    )
    {
        this.helper = helper;
        this.monitor = monitor;
        this.ledgerService = ledgerService;
        this.data = new ExchangeSaveData();

        this.Load();
    }

    public void Load()
    {
        if (!Context.IsWorldReady)
        {
            this.data = new ExchangeSaveData();
            this.loadedSaveId = null;

            this.monitor.Log(
                "Exchange initialized before save data was ready.",
                LogLevel.Trace
            );

            return;
        }

        this.data =
            this.helper.Data.ReadSaveData<ExchangeSaveData>(SaveDataKey)
            ?? new ExchangeSaveData();

        this.EnsureCollections();
        this.loadedSaveId = this.GetCurrentSaveId();

        this.monitor.Log(
            $"Exchange loaded from save data for save {this.loadedSaveId}.",
            LogLevel.Trace
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
            $"Exchange saved to save data for save {this.loadedSaveId}.",
            LogLevel.Trace
        );
    }

    public ExchangeAccount GetAccount()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.Account;
    }

    public IReadOnlyList<ExchangePosition> GetPositions()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.Account.Positions;
    }

    public IReadOnlyList<ExchangeAccountHistoryEntry> GetAccountHistory()
    {
        this.EnsureLoadedForCurrentSave();

        return this.data.Account.AccountHistory;
    }



    public bool TryDeposit(
        int amount,
        out string message
    )
    {
        this.EnsureLoadedForCurrentSave();

        if (!Context.IsWorldReady)
        {
            message = "A save must be loaded before using the exchange.";
            return false;
        }

        if (amount <= 0)
        {
            message = "Deposit amount must be greater than 0g.";
            return false;
        }

        if (Game1.player.Money < amount)
        {
            message = $"Not enough farm money. Current farm money: {Game1.player.Money}g.";
            return false;
        }

        string transactionId = this.CreateTransactionId("exchange_deposit");

        this.ledgerService.SuppressNextExpenseAmount(
            amount,
            reason: "ExchangeDeposit",
            source: "Exchange Transfer",
            transactionId: transactionId
        );

        this.ledgerService.AddExpense(
            "Exchange Transfer",
            "Transfer to Exchange Account",
            1,
            amount,
            itemId: "",
            dataOrigin: "ExchangeDeposit",
            transactionId: transactionId
        );

        Game1.player.Money -= amount;
        this.data.Account.CashBalance += amount;

        this.AddHistory(
            "Deposit",
            $"Transfer to Exchange Account: {amount}g",
            amount,
            transactionId
        );

        this.Save();

        message = $"Deposited {amount}g. Exchange cash: {this.data.Account.CashBalance}g, available: {this.data.Account.AvailableBalance}g.";
        return true;
    }

    public bool TryWithdraw(
        int amount,
        out string message
    )
    {
        this.EnsureLoadedForCurrentSave();

        if (!Context.IsWorldReady)
        {
            message = "A save must be loaded before using the exchange.";
            return false;
        }

        if (amount <= 0)
        {
            message = "Withdraw amount must be greater than 0g.";
            return false;
        }

        if (this.data.Account.AvailableBalance < amount)
        {
            message = $"Not enough exchange available balance. Available: {this.data.Account.AvailableBalance}g.";
            return false;
        }

        string transactionId = this.CreateTransactionId("exchange_withdraw");

        this.ledgerService.SuppressNextIncomeAmount(
            amount,
            reason: "ExchangeWithdraw",
            source: "Exchange Transfer",
            transactionId: transactionId
        );

        this.ledgerService.AddIncome(
            "Exchange Transfer",
            "Transfer from Exchange Account",
            1,
            amount,
            itemId: "",
            dataOrigin: "ExchangeWithdraw",
            transactionId: transactionId
        );

        this.data.Account.CashBalance -= amount;
        Game1.player.Money += amount;

        this.AddHistory(
            "Withdraw",
            $"Transfer from Exchange Account: {amount}g",
            -amount,
            transactionId
        );

        this.Save();

        message = $"Withdrew {amount}g. Exchange cash: {this.data.Account.CashBalance}g, available: {this.data.Account.AvailableBalance}g.";
        return true;
    }

    public string GetDebugStatus()
    {
        this.EnsureLoadedForCurrentSave();

        ExchangeAccount account = this.data.Account;

        return
            $"Exchange Account | Cash: {account.CashBalance}g | Locked Margin: {account.LockedMargin}g | Available: {account.AvailableBalance}g | Debt: {account.Debt}g | Positions: {account.Positions.Count}";
    }

    public void ProcessDailySettlement()
    {
        if (!Context.IsWorldReady)
            return;

        this.EnsureLoadedForCurrentSave();

        // Step 1 / Step 2 only: the daily lifecycle is now wired.
        // Actual mark-to-market, Margin Call, forced liquidation, and debt transfer
        // will be implemented in the next development steps.
        this.monitor.Log(
            "Exchange daily settlement skipped: trading engine not implemented yet.",
            LogLevel.Trace
        );
    }

    public void Clear()
    {
        this.EnsureLoadedForCurrentSave();

        this.data = new ExchangeSaveData();
        this.Save();

        this.monitor.Log(
            "Exchange save data cleared for current save.",
            LogLevel.Info
        );
    }

    private void AddHistory(
        string type,
        string description,
        int amount = 0,
        string contractId = ""
    )
    {
        this.EnsureLoadedForCurrentSave();

        this.data.Account.AccountHistory.Add(
            new ExchangeAccountHistoryEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Year = Context.IsWorldReady ? Game1.year : 0,
                Season = Context.IsWorldReady ? Game1.currentSeason : string.Empty,
                Day = Context.IsWorldReady ? Game1.dayOfMonth : 0,
                TimeOfDay = Context.IsWorldReady ? Game1.timeOfDay : 0,
                Type = type,
                ContractId = contractId,
                Description = description,
                Amount = amount,
                CashBalanceAfter = this.data.Account.CashBalance,
                DebtAfter = this.data.Account.Debt
            }
        );
    }

    private void EnsureLoadedForCurrentSave()
    {
        if (!Context.IsWorldReady)
            return;

        string currentSaveId = this.GetCurrentSaveId();

        if (this.loadedSaveId == currentSaveId)
        {
            this.EnsureCollections();
            return;
        }

        this.Load();
    }

    private void EnsureCollections()
    {
        this.data.Account ??= new ExchangeAccount();
        this.data.Account.Positions ??= new List<ExchangePosition>();
        this.data.Account.DeliveryStorage ??= new List<ExchangeDeliveryStorageEntry>();
        this.data.Account.AccountHistory ??= new List<ExchangeAccountHistoryEntry>();
    }



    private string CreateTransactionId(string prefix)
    {
        if (!Context.IsWorldReady)
            return $"{prefix}_{Guid.NewGuid():N}";

        return $"{prefix}_{Game1.year}_{Game1.currentSeason}_{Game1.dayOfMonth}_{Guid.NewGuid():N}";
    }

    private string GetCurrentSaveId()
    {
        return Game1.uniqueIDForThisGame.ToString();
    }
}
