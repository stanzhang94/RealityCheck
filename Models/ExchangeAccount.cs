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

    // CashBalance is the exchange account total equity: available cash plus current
    // position margin equity. It can be reduced by daily mark-to-market losses.
    //
    // LockedMargin is dynamic: it is the current margin equity still locked inside
    // open positions, not the original initial-margin requirement. Losses consume
    // PositionMargin first; excess loss then reduces available cash through the
    // account total. Profits refill PositionMargin up to InitialMarginRequired;
    // only excess profit becomes available cash.
    public int LockedMargin => this.Positions
        .Where(position => position.IsOpenLike()
            || string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase))
        .Sum(position => Math.Max(0, Math.Min(position.PositionMargin, position.InitialMarginRequired)));

    public int AvailableBalance => this.CashBalance - this.LockedMargin - this.Debt;
}
