using System.Collections.Generic;

namespace RealityCheck.Models;

public class ExchangeContractSpec
{
    public string MarketCommodityKey { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public string ParentItemId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int MarketUnitPrice { get; set; }

    public int QuantityPerLot { get; set; } = 100;

    public int ContractValuePerLot { get; set; }

    public int InitialMarginRequiredPerLot { get; set; }

    public int MaintenanceMarginRequiredPerLot { get; set; }

    public bool SupportsSevenDayContract { get; set; } = true;

    public bool SupportsFourteenDayContract { get; set; } = true;

    public bool SupportsTwentyEightDayContract { get; set; } = true;

    public bool SupportsTerm(int termDays)
    {
        return termDays switch
        {
            7 => this.SupportsSevenDayContract,
            14 => this.SupportsFourteenDayContract,
            28 => this.SupportsTwentyEightDayContract,
            _ => false
        };
    }

    public int GetFirstSupportedTerm()
    {
        if (this.SupportsSevenDayContract)
            return 7;

        if (this.SupportsFourteenDayContract)
            return 14;

        if (this.SupportsTwentyEightDayContract)
            return 28;

        return 0;
    }

    public string GetSupportedTermsLabel()
    {
        List<string> terms = new();

        if (this.SupportsSevenDayContract)
            terms.Add("7d");

        if (this.SupportsFourteenDayContract)
            terms.Add("14d");

        if (this.SupportsTwentyEightDayContract)
            terms.Add("28d");

        return terms.Count > 0
            ? string.Join("/", terms)
            : "None";
    }
}
