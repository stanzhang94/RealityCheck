namespace RealityCheck.Models;

public class LedgerEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public int Year { get; set; }

    public string Season { get; set; } = "";

    public int Day { get; set; }

    public string Type { get; set; } = "Income";

    public string Source { get; set; } = "";

    public string ItemName { get; set; } = "";
    public string ItemId { get; set; } = "";

    /// <summary>
    /// Stable market identity for dynamic commodities.
    /// For regular items this usually matches ItemId. For flavored artisan goods,
    /// examples include Artisan:Wine:(O)268 and Artisan:Jelly:(O)613.
    /// </summary>
    public string MarketCommodityKey { get; set; } = "";

    /// <summary>
    /// Parent ingredient item ID for flavored artisan goods, such as (O)268 for Starfruit Wine.
    /// Empty for regular fixed items or flavored goods without a concrete parent.
    /// </summary>
    public string ParentItemId { get; set; } = "";

    /// <summary>
    /// Vanilla/base sell unit price recorded at the time of transaction.
    /// Used to rebuild discovered flavored artisan rows in Market Price.
    /// Zero for old entries created before this field existed.
    /// </summary>
    public int BaseUnitPrice { get; set; }

    public int Quantity { get; set; }

    public int Amount { get; set; }

    public int TimeOfDay { get; set; }

    public string DataOrigin { get; set; } = "";

    public string TransactionId { get; set; } = "";
}