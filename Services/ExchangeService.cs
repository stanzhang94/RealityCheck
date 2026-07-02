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
            message = I18n.Get("exchange.error_save_required");
            return false;
        }

        if (amount <= 0)
        {
            message = I18n.Get("exchange.error_deposit_positive");
            return false;
        }

        if (Game1.player.Money < amount)
        {
            message = I18n.Get("exchange.error_not_enough_farm_money", new { amount = Game1.player.Money });
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

        message = I18n.Get("exchange.deposit_success", new { amount, cash = this.data.Account.CashBalance, available = this.data.Account.AvailableBalance });
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
            message = I18n.Get("exchange.error_save_required");
            return false;
        }

        if (amount <= 0)
        {
            message = I18n.Get("exchange.error_withdraw_positive");
            return false;
        }

        if (this.data.Account.AvailableBalance < amount)
        {
            message = I18n.Get("exchange.error_not_enough_exchange_available", new { amount = this.data.Account.AvailableBalance });
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

        message = I18n.Get("exchange.withdraw_success", new { amount, cash = this.data.Account.CashBalance, available = this.data.Account.AvailableBalance });
        return true;
    }


    public bool TryOpenPosition(
        ExchangeContractSpec spec,
        string direction,
        int termDays,
        int lots,
        out ExchangePosition? position,
        out string message
    )
    {
        this.EnsureLoadedForCurrentSave();
        position = null;

        if (!Context.IsWorldReady)
        {
            message = I18n.Get("exchange.error_save_required");
            return false;
        }

        if (spec is null || string.IsNullOrWhiteSpace(spec.MarketCommodityKey))
        {
            message = I18n.Get("exchange.error_invalid_contract");
            return false;
        }

        if (direction != ExchangePosition.DirectionLong && direction != ExchangePosition.DirectionShort)
        {
            message = I18n.Get("exchange.error_invalid_direction");
            return false;
        }

        if (lots <= 0)
        {
            message = I18n.Get("exchange.error_invalid_lots");
            return false;
        }

        if (!spec.SupportsTerm(termDays))
        {
            message = I18n.Get("exchange.error_unsupported_term");
            return false;
        }

        int totalQuantity = spec.QuantityPerLot * lots;
        int openPrice = Math.Max(0, spec.MarketUnitPrice);
        int openNotionalValue = openPrice * totalQuantity;
        int initialMarginRequired = (int)Math.Ceiling(openNotionalValue * InitialMarginRate);
        int maintenanceMarginRequired = (int)Math.Ceiling(openNotionalValue * MaintenanceMarginRate);

        if (initialMarginRequired <= 0)
        {
            message = I18n.Get("exchange.error_invalid_margin");
            return false;
        }

        if (this.data.Account.AvailableBalance < initialMarginRequired)
        {
            message = I18n.Get("exchange.error_not_enough_available", new
            {
                available = this.data.Account.AvailableBalance,
                required = initialMarginRequired
            });
            return false;
        }

        string contractId = this.CreateContractId();
        int openDateIndex = GetCurrentDateIndex();

        position = new ExchangePosition
        {
            ContractId = contractId,
            MarketCommodityKey = spec.MarketCommodityKey,
            ItemId = spec.ItemId,
            ParentItemId = spec.ParentItemId,
            DisplayName = spec.DisplayName,
            Direction = direction,
            QuantityPerLot = spec.QuantityPerLot,
            Lots = lots,
            TotalQuantity = totalQuantity,
            TermDays = termDays,
            OpenPrice = openPrice,
            LastSettlementPrice = openPrice,
            CurrentPrice = openPrice,
            OpenNotionalValue = openNotionalValue,
            InitialMarginRequired = initialMarginRequired,
            MaintenanceMarginRequired = maintenanceMarginRequired,
            PositionMargin = initialMarginRequired,
            Status = ExchangePosition.StatusOpen,
            OpenYear = Game1.year,
            OpenSeason = Game1.currentSeason,
            OpenDay = Game1.dayOfMonth,
            OpenDateIndex = openDateIndex,
            ExpiryDateIndex = openDateIndex + termDays
        };

        this.data.Account.Positions.Add(position);

        this.AddHistory(
            "Open Position",
            $"Open {direction} {spec.DisplayName} x{lots}: margin {initialMarginRequired}g",
            -initialMarginRequired,
            contractId
        );

        this.Save();

        message = I18n.Get("exchange.position_created", new
        {
            name = spec.DisplayName,
            direction = direction == ExchangePosition.DirectionLong ? I18n.Get("exchange.long") : I18n.Get("exchange.short"),
            lots,
            margin = initialMarginRequired
        });
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




    private string CreateContractId()
    {
        if (!Context.IsWorldReady)
            return $"EX-{Guid.NewGuid():N}";

        return $"EX-Y{Game1.year}-{NormalizeSeasonCode(Game1.currentSeason)}-{Game1.dayOfMonth:00}-{this.data.NextContractSerial++:0000}";
    }

    private static string NormalizeSeasonCode(string season)
    {
        return season switch
        {
            "spring" => "SP",
            "summer" => "SU",
            "fall" => "FA",
            "winter" => "WI",
            _ => "??"
        };
    }

    private static int GetCurrentDateIndex()
    {
        int seasonIndex = Game1.currentSeason switch
        {
            "spring" => 0,
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };

        return ((Game1.year - 1) * 112) + (seasonIndex * 28) + Math.Max(1, Game1.dayOfMonth);
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
