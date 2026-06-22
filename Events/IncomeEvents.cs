using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace RealityCheck.Events;

public class IncomeEvents
{
    private readonly LedgerService ledgerService;
    private readonly IMonitor monitor;

    public IncomeEvents(LedgerService ledgerService, IMonitor monitor)
    {
        this.ledgerService = ledgerService;
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

        foreach (var item in shippingBin)
        {
            if (item == null)
                continue;

            int quantity = item.Stack;
            int unitPrice = item.sellToStorePrice(-1L);
            int totalAmount = unitPrice * quantity;

            if (totalAmount <= 0)
                continue;

            this.ledgerService.AddIncome(
                "Shipping Bin",
                item.DisplayName,
                quantity,
                totalAmount
            );

            this.monitor.Log(
                $"Shipping income recorded: {item.DisplayName} x{quantity} = {totalAmount}g",
                LogLevel.Info
            );
        }
    }
}