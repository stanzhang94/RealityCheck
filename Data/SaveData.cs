using System.Collections.Generic;
using RealityCheck.Models;

namespace RealityCheck.Data;

public class SaveData
{
    public List<LedgerEntry> Ledger { get; set; } = new();

    public List<TaxRecord> TaxRecords { get; set; } = new();

    public List<PropertyTaxDailyAssessment> PropertyTaxDailyAssessments { get; set; } = new();

    public List<BusinessPropertyTaxDailyAssessment> BusinessPropertyTaxDailyAssessments { get; set; } = new();

    public List<string> SignedTaxNoticeIds { get; set; } = new();

    public List<HealthInsuranceClaim> HealthInsuranceClaims { get; set; } = new();

    // Positive internal value.
    // UI may display this as negative debt if needed.
    public int OutstandingBalance { get; set; } = 0;

    public List<string> FavoriteMarketCommodityKeys { get; set; } = new();
}
