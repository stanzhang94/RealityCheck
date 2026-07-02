namespace RealityCheck.Models;

public class ExchangeDeliveryStorageEntry
{
    public string MarketCommodityKey { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public string ParentItemId { get; set; } = string.Empty;

    public int Quality { get; set; } = 0;

    public int Quantity { get; set; } = 0;
}
