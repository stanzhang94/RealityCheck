using System;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class MarketPriceService
{
    private readonly ConfigService configService;
    private readonly IMonitor monitor;

    public MarketPriceService(
        ConfigService configService,
        IMonitor monitor
    )
    {
        this.configService = configService;
        this.monitor = monitor;
    }

    public bool IsShippingBinShadowPriceTestEnabled()
    {
        return this.configService.Config.Market.EnableShippingBinShadowPriceTest;
    }

    public MarketPriceResult GetShippingBinShadowSellPrice(
        Item item,
        int quantity,
        int baseUnitPrice
    )
    {
        double multiplier = this.GetShadowPriceMultiplier();
        int baseTotal = Math.Max(0, baseUnitPrice) * Math.Max(0, quantity);
        int marketTotal = this.CalculateMarketTotal(baseTotal, multiplier);

        return new MarketPriceResult
        {
            ItemName = item.DisplayName,
            ItemId = item.QualifiedItemId,
            Quantity = quantity,
            BaseUnitPrice = baseUnitPrice,
            BaseTotal = baseTotal,
            MarketMultiplier = multiplier,
            MarketTotal = marketTotal,
            MarketUnitPrice = quantity > 0
                ? (double)marketTotal / quantity
                : 0.0
        };
    }

    public int CalculateMarketTotal(
        int baseTotal,
        double multiplier
    )
    {
        if (baseTotal <= 0)
            return 0;

        double safeMultiplier = Math.Max(0.0, multiplier);

        return Math.Max(
            0,
            (int)Math.Round(
                baseTotal * safeMultiplier,
                MidpointRounding.AwayFromZero
            )
        );
    }

    public double GetShadowPriceMultiplier()
    {
        double multiplier = this.configService.Config.Market.ShadowPriceMultiplier;

        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier <= 0)
        {
            this.monitor.Log(
                $"Invalid shadow market multiplier {multiplier:0.###}; falling back to 1.0.",
                LogLevel.Warn
            );

            return 1.0;
        }

        return multiplier;
    }
}
