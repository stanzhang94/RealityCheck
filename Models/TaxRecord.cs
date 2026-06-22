namespace RealityCheck.Models;

public class TaxRecord

{

    public int Year { get; set; }

    public string Season { get; set; } = "";

    public int WeekNumber { get; set; }

    public int CoveredStartDay { get; set; }

    public int CoveredEndDay { get; set; }

    public int SettlementYear { get; set; }

    public string SettlementSeason { get; set; } = "";

    public int SettlementDay { get; set; }

    public int TaxableShippingBinIncome { get; set; }

    public double IncomeTaxRate { get; set; }

    public int IncomeTaxAmount { get; set; }

    public int PropertyTaxAmount { get; set; }

    public int BusinessPropertyTaxAmount { get; set; }

    public int TotalTaxAmount { get; set; }

}