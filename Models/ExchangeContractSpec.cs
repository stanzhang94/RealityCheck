namespace RealityCheck.Models;

public class ExchangeContractSpec
{
    public string MarketCommodityKey { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public string ParentItemId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int QuantityPerLot { get; set; } = 100;

    public bool SupportsSevenDayContract { get; set; } = true;

    public bool SupportsFourteenDayContract { get; set; } = true;

    public bool SupportsTwentyEightDayContract { get; set; } = true;
}
