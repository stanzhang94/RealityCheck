using RealityCheck.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;

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
        // V0.5 第一版先占位。
        // 出货箱收入后面单独接。
        this.monitor.Log("Day ending checked by Reality Check.", LogLevel.Trace);
    }
}