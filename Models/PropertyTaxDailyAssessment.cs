namespace RealityCheck.Models;

public class PropertyTaxDailyAssessment
{
    public int Year { get; set; }

    public string Season { get; set; } = "";

    public int Day { get; set; }

    public double ReplacementCostAmount { get; set; }

    public double IncomePotentialValueAmount { get; set; }

    public double UtilityPremiumAmount { get; set; }

    public double RiskShieldPremiumAmount { get; set; }

    public double DepreciationFactor { get; set; }

    public double AgriculturalDeductionAmount { get; set; }

    public double AdministrativeFeeAmount { get; set; }

    public double DocumentationFeeAmount { get; set; }

    public double TotalPropertyTaxAmount { get; set; }
}