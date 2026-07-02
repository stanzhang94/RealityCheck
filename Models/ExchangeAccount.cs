using System;
using System.Collections.Generic;
using System.Linq;

namespace RealityCheck.Models;

public class ExchangeAccount
{
    public int CashBalance { get; set; } = 0;

    public int Debt { get; set; } = 0;

    public List<ExchangePosition> Positions { get; set; } = new();

    public List<ExchangeDeliveryStorageEntry> DeliveryStorage { get; set; } = new();

    public List<ExchangeAccountHistoryEntry> AccountHistory { get; set; } = new();

    public int LockedMargin => this.Positions
        .Where(position => position.IsOpenLike())
        .Sum(position => Math.Max(0, position.PositionMargin));

    public int AvailableBalance => this.CashBalance - this.LockedMargin - this.Debt;
}
