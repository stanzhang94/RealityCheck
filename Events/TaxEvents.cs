using RealityCheck.Models;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace RealityCheck.Events;

public class TaxEvents
{
    private readonly TaxService taxService;
    private readonly TaxNoticeService taxNoticeService;
    private readonly IMonitor monitor;

    public TaxEvents(
        LedgerService ledgerService,
        IModHelper helper,
        IMonitor monitor
    )
    {
        this.taxService = new TaxService(
            ledgerService,
            monitor
        );

        this.taxNoticeService = new TaxNoticeService(
            ledgerService,
            helper,
            monitor
        );

        this.monitor = monitor;

        helper.Events.Content.AssetRequested += this.taxNoticeService.OnAssetRequested;
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

        if (settled && this.taxService.LastSettledTaxRecord != null)
        {
            TaxRecord record = this.taxService.LastSettledTaxRecord;

            this.taxNoticeService.DeliverWeeklyTaxNotice(
                record
            );
        }

        this.taxService.EnsureTodayPropertyTaxAssessment();

        this.monitor.Log(
            "Property Tax daily assessment checked.",
            LogLevel.Trace
        );

        this.taxService.EnsureTodayBusinessPropertyTaxAssessment();

        this.monitor.Log(
            "Business Property Tax daily assessment checked.",
            LogLevel.Trace
        );
    }
}