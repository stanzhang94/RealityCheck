using System;

namespace RealityCheck.Models;

public class ExchangePosition
{
    public const string DirectionLong = "Long";
    public const string DirectionShort = "Short";

    public const string StatusOpen = "Open";
    public const string StatusMarginCall = "MarginCall";
    public const string StatusClosed = "Closed";
    public const string StatusForcedLiquidated = "ForcedLiquidated";
    public const string StatusDelivered = "Delivered";
    public const string StatusDeliveryDefault = "DeliveryDefault";

    public string ContractId { get; set; } = string.Empty;

    public string MarketCommodityKey { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public string ParentItemId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Direction { get; set; } = DirectionLong;

    public int QuantityPerLot { get; set; } = 100;

    public int Lots { get; set; } = 1;

    public int TotalQuantity { get; set; } = 100;

    public int TermDays { get; set; } = 7;

    public int OpenPrice { get; set; } = 0;

    public int LastSettlementPrice { get; set; } = 0;

    public int CurrentPrice { get; set; } = 0;

    public int OpenNotionalValue { get; set; } = 0;

    public int InitialMarginRequired { get; set; } = 0;

    public int MaintenanceMarginRequired { get; set; } = 0;

    public int PositionMargin { get; set; } = 0;

    public string Status { get; set; } = StatusOpen;

    public int OpenYear { get; set; } = 0;

    public string OpenSeason { get; set; } = string.Empty;

    public int OpenDay { get; set; } = 0;

    public int OpenDateIndex { get; set; } = 0;

    public int ExpiryDateIndex { get; set; } = 0;

    public bool IsOpenLike()
    {
        return string.Equals(this.Status, StatusOpen, StringComparison.OrdinalIgnoreCase)
            || string.Equals(this.Status, StatusMarginCall, StringComparison.OrdinalIgnoreCase);
    }
}
