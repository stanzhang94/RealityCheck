using System;
using RealityCheck.Patches;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace RealityCheck.Events;

public class IncomeEvents
{
    private readonly LedgerService ledgerService;
    private readonly MarketPriceService marketPriceService;
    private readonly IMonitor monitor;

    public IncomeEvents(
        LedgerService ledgerService,
        MarketPriceService marketPriceService,
        IMonitor monitor
    )
    {
        this.ledgerService = ledgerService;
        this.marketPriceService = marketPriceService;
        this.monitor = monitor;
    }

    public void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Game1.getFarm() is not Farm farm)
            return;

        var shippingBin = farm.getShippingBin(Game1.player);

        this.monitor.Log(
            $"Shipping bin contains {shippingBin.Count} items at day ending.",
            LogLevel.Info
        );

        bool applyMarketSettlement = this.marketPriceService.IsShippingBinMarketSettlementEnabled();
        bool runShadowPriceTest = this.marketPriceService.IsShippingBinShadowPriceTestEnabled()
            || applyMarketSettlement;

        int totalVanillaShippingIncome = 0;
        int totalMarketShippingIncome = 0;
        int totalLedgerShippingIncome = 0;

        foreach (var item in shippingBin)
        {
            if (item == null)
                continue;

            int quantity = item.Stack;
            int unitPrice = item.sellToStorePrice(-1L);
            int vanillaTotalAmount = unitPrice * quantity;

            if (vanillaTotalAmount <= 0)
                continue;

            var marketPrice = this.marketPriceService.GetShippingBinMarketSellPrice(
                item,
                quantity,
                unitPrice
            );

            int ledgerAmount = applyMarketSettlement
                ? marketPrice.MarketTotal
                : vanillaTotalAmount;

            string dataOrigin = applyMarketSettlement
                ? "MarketShippingBinSettlement"
                : "CalculatedFromShippingBin";

            if (runShadowPriceTest)
            {
                string logPrefix = applyMarketSettlement
                    ? "[Market Settlement]"
                    : "[Market Shadow]";

                string suffix = applyMarketSettlement
                    ? "Will be used for the known Shipping Bin settlement."
                    : "Not applied.";

                this.monitor.Log(
                    $"{logPrefix} Shipping Bin {marketPrice.ItemName} x{marketPrice.Quantity}: base {marketPrice.BaseUnitPrice}g x {marketPrice.MarketMultiplier:0.###} -> market total {marketPrice.MarketTotal}g (vanilla total {marketPrice.BaseTotal}g, diff {marketPrice.Difference:+#;-#;0}g). {suffix}",
                    LogLevel.Info
                );
            }

            string transactionId = $"shipping_{Game1.year}_{Game1.currentSeason}_{Game1.dayOfMonth}_{Guid.NewGuid():N}";

            this.ledgerService.AddIncome(
                "Shipping Bin",
                item.DisplayName,
                quantity,
                ledgerAmount,
                item.QualifiedItemId,
                dataOrigin: dataOrigin,
                transactionId: transactionId
            );

            totalVanillaShippingIncome += vanillaTotalAmount;
            totalMarketShippingIncome += marketPrice.MarketTotal;
            totalLedgerShippingIncome += ledgerAmount;

            if (applyMarketSettlement)
            {
                this.monitor.Log(
                    $"Shipping income recorded at market settlement: {item.DisplayName} x{quantity} = {ledgerAmount}g (vanilla {vanillaTotalAmount}g)",
                    LogLevel.Info
                );
            }
            else
            {
                this.monitor.Log(
                    $"Shipping income recorded: {item.DisplayName} x{quantity} = {ledgerAmount}g",
                    LogLevel.Info
                );
            }
        }

        if (runShadowPriceTest && totalVanillaShippingIncome > 0)
        {
            int totalDifference = totalMarketShippingIncome - totalVanillaShippingIncome;
            string logPrefix = applyMarketSettlement
                ? "[Market Settlement]"
                : "[Market Shadow]";

            string suffix = applyMarketSettlement
                ? "Ledger/tax use market total; original shipping payout interception is required."
                : "Ledger/tax/money unchanged.";

            this.monitor.Log(
                $"{logPrefix} Shipping Bin total: vanilla {totalVanillaShippingIncome}g -> market {totalMarketShippingIncome}g (diff {totalDifference:+#;-#;0}g). {suffix}",
                LogLevel.Info
            );
        }

        if (totalVanillaShippingIncome > 0
            && (applyMarketSettlement || this.marketPriceService.IsShippingSettlementTraceEnabled()))
        {
            ShippingSettlementTracePatch.BeginTraceWindow(
                totalVanillaShippingIncome,
                totalMarketShippingIncome > 0 ? totalMarketShippingIncome : totalVanillaShippingIncome
            );
        }

        if (totalLedgerShippingIncome > 0)
        {
            this.ledgerService.SuppressNextIncomeAmount(
                totalLedgerShippingIncome,
                reason: applyMarketSettlement
                    ? "KnownMarketShippingBinIncome"
                    : "KnownShippingBinIncome",
                source: "Shipping Bin",
                transactionId: $"shipping_total_{Game1.year}_{Game1.currentSeason}_{Game1.dayOfMonth}"
            );

            this.monitor.Log(
                applyMarketSettlement
                    ? $"Suppressed next income fallback for pending market shipping payout: {totalLedgerShippingIncome}g"
                    : $"Suppressed next income fallback for pending shipping payout: {totalLedgerShippingIncome}g",
                LogLevel.Info
            );
        }
    }
}
