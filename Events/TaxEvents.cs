using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck.Events;

public class TaxEvents
{
    private readonly TaxService taxService;
    private readonly IMonitor monitor;

    public TaxEvents(
        LedgerService ledgerService,
        IMonitor monitor
    )
    {
        this.taxService = new TaxService(ledgerService);
        this.monitor = monitor;
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        bool settled = this.taxService.TrySettlePreviousTaxWeek(
            out string message
        );

        if (!string.IsNullOrWhiteSpace(message))
        {
            this.monitor.Log(
                message,
                settled ? LogLevel.Info : LogLevel.Trace
            );
        }

        if (settled)
        {
            Game1.showGlobalMessage(
                message
            );
        }
    }
}