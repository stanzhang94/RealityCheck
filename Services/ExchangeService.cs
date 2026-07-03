using System;
using System.Collections.Generic;
using System.Linq;
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
    public const double ForcedLiquidationSlippageRate = 0.02;
    public const int DefaultQuantityPerLot = 100;
    private const int MaxAccountHistoryEntries = 10;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly LedgerService ledgerService;
    private readonly MarketPriceService marketPriceService;

    private ExchangeSaveData data;
    private string? loadedSaveId = null;

    public ExchangeService(
        IModHelper helper,
        IMonitor monitor,
        LedgerService ledgerService,
        MarketPriceService marketPriceService
    )
    {
        this.helper = helper;
        this.monitor = monitor;
        this.ledgerService = ledgerService;
        this.marketPriceService = marketPriceService;
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
        this.SyncDebtFromAccountDeficit();

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
        this.SyncDebtFromAccountDeficit();

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

    public int GetRequiredMarginTopUp(ExchangePosition position)
    {
        if (position is null || !position.IsOpenLike())
            return 0;

        return Math.Max(0, position.InitialMarginRequired - position.PositionMargin);
    }

    public bool TryTopUpMargin(
        string contractId,
        out string message
    )
    {
        this.EnsureLoadedForCurrentSave();

        if (!Context.IsWorldReady)
        {
            message = I18n.Get("exchange.error_save_required");
            return false;
        }

        ExchangePosition? position = this.data.Account.Positions.FirstOrDefault(position =>
            string.Equals(position.ContractId, contractId, StringComparison.OrdinalIgnoreCase)
        );

        if (position is null || !position.IsOpenLike())
        {
            message = I18n.Get("exchange.margin_topup_error_no_position");
            return false;
        }

        bool isMarginCall = string.Equals(position.Status, ExchangePosition.StatusMarginCall, StringComparison.OrdinalIgnoreCase);
        if (!isMarginCall)
        {
            message = I18n.Get("exchange.margin_topup_not_required");
            return false;
        }

        int requiredTopUp = this.GetRequiredMarginTopUp(position);
        if (requiredTopUp <= 0)
        {
            this.ClearMarginCall(position);
            this.Save();

            message = I18n.Get("exchange.margin_topup_not_required");
            return false;
        }

        if (this.data.Account.AvailableBalance < requiredTopUp)
        {
            message = I18n.Get("exchange.margin_topup_error_not_enough", new
            {
                available = this.data.Account.AvailableBalance,
                required = requiredTopUp
            });
            return false;
        }

        this.ApplyMarginTopUp(
            position,
            requiredTopUp,
            "Manual Margin Top-Up",
            I18n.Get("exchange.history_manual_margin_topup", new
            {
                contract = position.ContractId,
                name = position.DisplayName,
                amount = requiredTopUp
            })
        );

        this.Save();

        message = I18n.Get("exchange.margin_topup_success", new
        {
            contract = position.ContractId,
            amount = requiredTopUp
        });
        return true;
    }

    public bool TryClosePosition(
        string contractId,
        out string message
    )
    {
        this.EnsureLoadedForCurrentSave();

        if (!Context.IsWorldReady)
        {
            message = I18n.Get("exchange.error_save_required");
            return false;
        }

        ExchangePosition? position = this.data.Account.Positions.FirstOrDefault(position =>
            string.Equals(position.ContractId, contractId, StringComparison.OrdinalIgnoreCase)
        );

        if (position is null || !position.IsOpenLike())
        {
            message = I18n.Get("exchange.close_error_no_position");
            return false;
        }

        int todayDateIndex = GetCurrentDateIndex();
        if (position.ExpiryDateIndex > 0 && todayDateIndex >= position.ExpiryDateIndex)
        {
            message = I18n.Get("exchange.close_error_expired");
            return false;
        }

        int marketPrice = this.GetLatestMarketUnitPrice(position, todayDateIndex);
        if (marketPrice <= 0)
            marketPrice = Math.Max(1, position.CurrentPrice > 0 ? position.CurrentPrice : position.OpenPrice);

        int previousSettlementPrice = position.LastSettlementPrice > 0
            ? position.LastSettlementPrice
            : Math.Max(1, position.OpenPrice);

        int settlementAmount = this.CalculateDailySettlementAmount(
            position,
            previousSettlementPrice,
            marketPrice
        );

        if (settlementAmount != 0)
        {
            this.ApplySettlementToPositionMargin(position, settlementAmount);
            this.data.Account.CashBalance += settlementAmount;
        }

        int releasedMargin = Math.Max(0, position.PositionMargin);

        position.Status = ExchangePosition.StatusClosed;
        position.PositionMargin = 0;
        position.CurrentPrice = marketPrice;
        position.LastSettlementPrice = marketPrice;
        position.MarginCallDateIndex = 0;
        position.MarginCallRequiredTopUp = 0;
        position.LastRiskMessage = I18n.Get("exchange.close_reason_player");

        this.SyncDebtFromAccountDeficit();

        this.AddHistory(
            "Close Position",
            I18n.Get("exchange.history_close_position", new
            {
                contract = position.ContractId,
                name = position.DisplayName,
                price = marketPrice,
                settlement = settlementAmount,
                released = releasedMargin
            }),
            settlementAmount,
            position.ContractId
        );

        this.Save();

        message = I18n.Get("exchange.close_success", new
        {
            contract = position.ContractId,
            price = marketPrice,
            settlement = settlementAmount,
            released = releasedMargin
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

        int todayDateIndex = GetCurrentDateIndex();
        if (this.data.LastSettlementDateIndex >= todayDateIndex)
        {
            this.monitor.Log(
                $"Exchange daily settlement already processed for date index {todayDateIndex}.",
                LogLevel.Trace
            );
            return;
        }

        int settledPositions = 0;
        int totalSettlement = 0;

        foreach (ExchangePosition position in this.data.Account.Positions.Where(position => position.IsOpenLike()))
        {
            // A contract opened today should not be marked to market until the next day.
            if (position.OpenDateIndex >= todayDateIndex)
                continue;

            // Once the delivery day has passed, later steps will handle delivery/default.
            // The delivery day itself is still marked before delivery checks.
            if (position.ExpiryDateIndex > 0 && todayDateIndex >= position.ExpiryDateIndex)
                continue;

            int currentPrice = this.GetLatestMarketUnitPrice(position, todayDateIndex);
            if (currentPrice <= 0)
                continue;

            int previousSettlementPrice = position.LastSettlementPrice > 0
                ? position.LastSettlementPrice
                : position.OpenPrice;

            if (previousSettlementPrice <= 0)
            {
                position.CurrentPrice = currentPrice;
                position.LastSettlementPrice = currentPrice;
                continue;
            }

            int settlementAmount = this.CalculateDailySettlementAmount(
                position,
                previousSettlementPrice,
                currentPrice
            );

            position.CurrentPrice = currentPrice;
            position.LastSettlementPrice = currentPrice;

            if (settlementAmount == 0)
                continue;

            SettlementAllocation allocation = this.ApplySettlementToPositionMargin(
                position,
                settlementAmount
            );

            this.data.Account.CashBalance += settlementAmount;
            totalSettlement += settlementAmount;
            settledPositions++;

            this.AddHistory(
                "Daily Settlement",
                $"{position.ContractId} {position.DisplayName}: {FormatSignedAmount(settlementAmount)} at {currentPrice}g; margin {FormatSignedAmount(allocation.MarginChange)}, available cash {FormatSignedAmount(allocation.AvailableCashChange)}",
                settlementAmount,
                position.ContractId
            );
        }

        this.ProcessPreDeliveryWarnings(todayDateIndex);
        this.ProcessExpiredPositions(todayDateIndex);

        MarginRiskResult marginRiskResult = this.ProcessMarginRiskAfterSettlement(todayDateIndex);
        this.SyncDebtFromAccountDeficit();

        this.data.LastSettlementDateIndex = todayDateIndex;
        this.Save();

        this.monitor.Log(
            $"Exchange daily settlement processed: positions={settledPositions}, total={totalSettlement}g, autoTopUps={marginRiskResult.AutoTopUps}, marginCalls={marginRiskResult.MarginCalls}, forcedLiquidations={marginRiskResult.ForcedLiquidations}, dateIndex={todayDateIndex}.",
            LogLevel.Trace
        );
    }

    private void ProcessPreDeliveryWarnings(int todayDateIndex)
    {
        foreach (ExchangePosition position in this.data.Account.Positions.Where(position => position.IsOpenLike()))
        {
            if (position.OpenDateIndex >= todayDateIndex)
                continue;

            if (position.ExpiryDateIndex <= 0 || position.ExpiryDateIndex - todayDateIndex != 1)
                continue;

            string description = I18n.Get("exchange.history_delivery_warning", new
            {
                contract = position.ContractId,
                name = position.DisplayName
            });

            this.AddHistory("Delivery Warning", description, 0, position.ContractId);

            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("exchange.delivery_warning_hud", new
                {
                    contract = position.ContractId
                }),
                HUDMessage.newQuest_type
            ));
        }
    }

    private void ProcessExpiredPositions(int todayDateIndex)
    {
        foreach (ExchangePosition position in this.data.Account.Positions.Where(position => position.IsOpenLike()).ToList())
        {
            if (position.ExpiryDateIndex <= 0 || todayDateIndex < position.ExpiryDateIndex)
                continue;

            position.Status = ExchangePosition.StatusPendingDelivery;
            position.MarginCallDateIndex = 0;
            position.MarginCallRequiredTopUp = 0;
            position.LastRiskMessage = I18n.Get("exchange.pending_delivery_note");

            this.AddHistory(
                "Pending Delivery",
                I18n.Get("exchange.history_pending_delivery", new
                {
                    contract = position.ContractId,
                    name = position.DisplayName
                }),
                0,
                position.ContractId
            );
        }
    }

    private MarginRiskResult ProcessMarginRiskAfterSettlement(int todayDateIndex)
    {
        int autoTopUps = 0;
        int marginCalls = 0;
        int forcedLiquidations = 0;

        foreach (ExchangePosition position in this.data.Account.Positions.Where(position => position.IsOpenLike()).ToList())
        {
            if (position.OpenDateIndex >= todayDateIndex)
                continue;

            if (position.ExpiryDateIndex > 0 && todayDateIndex >= position.ExpiryDateIndex)
                continue;

            int requiredTopUp = this.GetRequiredMarginTopUp(position);

            if (string.Equals(position.Status, ExchangePosition.StatusMarginCall, StringComparison.OrdinalIgnoreCase))
            {
                if (requiredTopUp <= 0)
                {
                    this.ClearMarginCall(position);
                    continue;
                }

                if (this.data.Account.AvailableBalance >= requiredTopUp)
                {
                    this.ApplyMarginTopUp(
                        position,
                        requiredTopUp,
                        "Auto Margin Top-Up",
                        I18n.Get("exchange.history_auto_margin_topup", new
                        {
                            contract = position.ContractId,
                            name = position.DisplayName,
                            amount = requiredTopUp
                        })
                    );
                    autoTopUps++;
                    continue;
                }

                if (position.MarginCallDateIndex > 0 && position.MarginCallDateIndex < todayDateIndex)
                {
                    this.ForceLiquidatePosition(
                        position,
                        todayDateIndex,
                        I18n.Get("exchange.liquidation_reason_unmet_margin_call", new
                        {
                            required = requiredTopUp,
                            available = this.data.Account.AvailableBalance
                        })
                    );
                    forcedLiquidations++;
                    continue;
                }

                position.MarginCallRequiredTopUp = requiredTopUp;
                continue;
            }

            if (position.PositionMargin > position.MaintenanceMarginRequired)
                continue;

            if (requiredTopUp <= 0)
                continue;

            if (this.data.Account.AvailableBalance >= requiredTopUp)
            {
                this.ApplyMarginTopUp(
                    position,
                    requiredTopUp,
                    "Auto Margin Top-Up",
                    I18n.Get("exchange.history_auto_margin_topup", new
                    {
                        contract = position.ContractId,
                        name = position.DisplayName,
                        amount = requiredTopUp
                    })
                );
                autoTopUps++;
                continue;
            }

            this.SetMarginCall(position, requiredTopUp, todayDateIndex, notifyPlayer: true);
            marginCalls++;
        }

        return new MarginRiskResult(autoTopUps, marginCalls, forcedLiquidations);
    }

    private void ApplyMarginTopUp(
        ExchangePosition position,
        int amount,
        string historyType,
        string historyDescription
    )
    {
        if (amount <= 0)
            return;

        position.PositionMargin += amount;
        this.ClearMarginCall(position);

        this.AddHistory(
            historyType,
            historyDescription,
            0,
            position.ContractId
        );
    }

    private void SetMarginCall(
        ExchangePosition position,
        int requiredTopUp,
        int todayDateIndex,
        bool notifyPlayer
    )
    {
        position.Status = ExchangePosition.StatusMarginCall;
        position.MarginCallDateIndex = todayDateIndex;
        position.MarginCallRequiredTopUp = requiredTopUp;
        position.LastRiskMessage = I18n.Get("exchange.margin_call_status_note", new
        {
            required = requiredTopUp
        });

        this.AddHistory(
            "Margin Call",
            I18n.Get("exchange.history_margin_call", new
            {
                contract = position.ContractId,
                name = position.DisplayName,
                amount = requiredTopUp
            }),
            0,
            position.ContractId
        );

        if (notifyPlayer)
        {
            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("exchange.margin_call_hud", new
                {
                    contract = position.ContractId,
                    amount = requiredTopUp
                }),
                HUDMessage.error_type
            ));
        }
    }

    private void ClearMarginCall(ExchangePosition position)
    {
        position.Status = ExchangePosition.StatusOpen;
        position.MarginCallDateIndex = 0;
        position.MarginCallRequiredTopUp = 0;
        position.LastRiskMessage = string.Empty;
    }

    private void ForceLiquidatePosition(
        ExchangePosition position,
        int todayDateIndex,
        string reason
    )
    {
        int marketPrice = position.CurrentPrice > 0
            ? position.CurrentPrice
            : this.GetLatestMarketUnitPrice(position, todayDateIndex);

        if (marketPrice <= 0)
            marketPrice = Math.Max(1, position.LastSettlementPrice > 0 ? position.LastSettlementPrice : position.OpenPrice);

        int previousSettlementPrice = position.LastSettlementPrice > 0
            ? position.LastSettlementPrice
            : marketPrice;

        int liquidationPrice = this.CalculateForcedLiquidationPrice(position, marketPrice);
        int liquidationSettlement = this.CalculateDailySettlementAmount(
            position,
            previousSettlementPrice,
            liquidationPrice
        );

        if (liquidationSettlement != 0)
        {
            this.ApplySettlementToPositionMargin(position, liquidationSettlement);
            this.data.Account.CashBalance += liquidationSettlement;
        }

        int releasedMargin = Math.Max(0, position.PositionMargin);

        position.Status = ExchangePosition.StatusForcedLiquidated;
        position.PositionMargin = 0;
        position.CurrentPrice = liquidationPrice;
        position.LastSettlementPrice = liquidationPrice;
        position.ForcedLiquidationDateIndex = todayDateIndex;
        position.ForcedLiquidationPrice = liquidationPrice;
        position.MarginCallDateIndex = 0;
        position.MarginCallRequiredTopUp = 0;
        position.LastRiskMessage = reason;

        this.SyncDebtFromAccountDeficit();

        this.AddHistory(
            "Forced Liquidation",
            I18n.Get("exchange.history_forced_liquidation", new
            {
                contract = position.ContractId,
                name = position.DisplayName,
                price = liquidationPrice,
                settlement = liquidationSettlement,
                released = releasedMargin,
                reason
            }),
            liquidationSettlement,
            position.ContractId
        );
    }

    private int CalculateForcedLiquidationPrice(
        ExchangePosition position,
        int marketPrice
    )
    {
        int safeMarketPrice = Math.Max(1, marketPrice);

        if (string.Equals(position.Direction, ExchangePosition.DirectionShort, StringComparison.OrdinalIgnoreCase))
            return Math.Max(1, (int)Math.Ceiling(safeMarketPrice * (1 + ForcedLiquidationSlippageRate)));

        return Math.Max(1, (int)Math.Floor(safeMarketPrice * (1 - ForcedLiquidationSlippageRate)));
    }

    private void SyncDebtFromAccountDeficit()
    {
        int availableDeficit = Math.Max(0, this.data.Account.LockedMargin - this.data.Account.CashBalance);
        int cashDeficit = Math.Max(0, -this.data.Account.CashBalance);
        this.data.Account.Debt = Math.Max(availableDeficit, cashDeficit);
    }

    private readonly record struct MarginRiskResult(
        int AutoTopUps,
        int MarginCalls,
        int ForcedLiquidations
    );


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

        while (this.data.Account.AccountHistory.Count > MaxAccountHistoryEntries)
            this.data.Account.AccountHistory.RemoveAt(0);
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




    private int GetLatestMarketUnitPrice(ExchangePosition position, int todayDateIndex)
    {
        if (position is null || string.IsNullOrWhiteSpace(position.MarketCommodityKey))
            return 0;

        MarketPriceHistoryPoint? latest = this.marketPriceService
            .GetMarketPriceHistory(position.MarketCommodityKey)
            .Where(point => point.DateIndex <= todayDateIndex && point.MarketUnitPrice > 0)
            .OrderByDescending(point => point.DateIndex)
            .FirstOrDefault();

        if (latest?.MarketUnitPrice > 0)
            return latest.MarketUnitPrice;

        if (position.CurrentPrice > 0)
            return position.CurrentPrice;

        if (position.LastSettlementPrice > 0)
            return position.LastSettlementPrice;

        return Math.Max(0, position.OpenPrice);
    }

    private SettlementAllocation ApplySettlementToPositionMargin(
        ExchangePosition position,
        int settlementAmount
    )
    {
        if (settlementAmount == 0)
            return new SettlementAllocation(0, 0);

        int initialMargin = Math.Max(0, position.InitialMarginRequired);
        int currentMargin = Math.Max(0, position.PositionMargin);

        if (settlementAmount < 0)
        {
            int loss = Math.Abs(settlementAmount);
            int marginLoss = Math.Min(currentMargin, loss);
            int excessLoss = loss - marginLoss;

            position.PositionMargin = Math.Max(0, currentMargin - marginLoss);

            return new SettlementAllocation(
                MarginChange: -marginLoss,
                AvailableCashChange: -excessLoss
            );
        }

        int roomToInitialMargin = Math.Max(0, initialMargin - currentMargin);
        int marginGain = Math.Min(roomToInitialMargin, settlementAmount);
        int excessGain = settlementAmount - marginGain;

        position.PositionMargin = currentMargin + marginGain;

        return new SettlementAllocation(
            MarginChange: marginGain,
            AvailableCashChange: excessGain
        );
    }

    private readonly record struct SettlementAllocation(
        int MarginChange,
        int AvailableCashChange
    );

    private int CalculateDailySettlementAmount(
        ExchangePosition position,
        int previousSettlementPrice,
        int currentPrice
    )
    {
        int quantity = Math.Max(0, position.TotalQuantity);
        int priceDelta = currentPrice - previousSettlementPrice;

        if (string.Equals(position.Direction, ExchangePosition.DirectionShort, StringComparison.OrdinalIgnoreCase))
            priceDelta = -priceDelta;

        return priceDelta * quantity;
    }

    private static string FormatSignedAmount(int amount)
    {
        return amount > 0
            ? $"+{amount}g"
            : $"{amount}g";
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
