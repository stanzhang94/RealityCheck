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

        int totalShippingIncome = 0;
        int totalShadowMarketIncome = 0;

        foreach (var item in shippingBin)
        {
            if (item == null)
                continue;

            int quantity = item.Stack;
            int unitPrice = item.sellToStorePrice(-1L);
            int totalAmount = unitPrice * quantity;

            if (totalAmount <= 0)
                continue;

            if (this.marketPriceService.IsShippingBinShadowPriceTestEnabled())
            {
                var marketPrice = this.marketPriceService.GetShippingBinShadowSellPrice(
                    item,
                    quantity,
                    unitPrice
                );

                totalShadowMarketIncome += marketPrice.MarketTotal;

                this.monitor.Log(
                    $"[Market Shadow] Shipping Bin {marketPrice.ItemName} x{marketPrice.Quantity}: base {marketPrice.BaseUnitPrice}g x {marketPrice.MarketMultiplier:0.###} -> market total {marketPrice.MarketTotal}g (vanilla total {marketPrice.BaseTotal}g, diff {marketPrice.Difference:+#;-#;0}g). Not applied.",
                    LogLevel.Info
                );
            }

            string transactionId = $"shipping_{Game1.year}_{Game1.currentSeason}_{Game1.dayOfMonth}_{Guid.NewGuid():N}";

            this.ledgerService.AddIncome(
                "Shipping Bin",
                item.DisplayName,
                quantity,
                totalAmount,
                item.QualifiedItemId,
                dataOrigin: "CalculatedFromShippingBin",
                transactionId: transactionId
            );

            totalShippingIncome += totalAmount;

            this.monitor.Log(
                $"Shipping income recorded: {item.DisplayName} x{quantity} = {totalAmount}g",
                LogLevel.Info
            );
        }

        if (this.marketPriceService.IsShippingBinShadowPriceTestEnabled() && totalShippingIncome > 0)
        {
            int totalDifference = totalShadowMarketIncome - totalShippingIncome;

            this.monitor.Log(
                $"[Market Shadow] Shipping Bin total: vanilla {totalShippingIncome}g -> shadow market {totalShadowMarketIncome}g (diff {totalDifference:+#;-#;0}g). Ledger/tax/money unchanged.",
                LogLevel.Info
            );
        }

        if (totalShippingIncome > 0)
        {
            this.ledgerService.SuppressNextIncomeAmount(
                totalShippingIncome,
                reason: "KnownShippingBinIncome",
                source: "Shipping Bin",
                transactionId: $"shipping_total_{Game1.year}_{Game1.currentSeason}_{Game1.dayOfMonth}"
            );

            this.monitor.Log(
                $"Suppressed next income fallback for pending shipping payout: {totalShippingIncome}g",
                LogLevel.Info
            );
        }
    }
}