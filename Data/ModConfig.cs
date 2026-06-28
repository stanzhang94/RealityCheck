using System.Collections.Generic;
using StardewModdingAPI.Utilities;

namespace RealityCheck.Data;

public class ModConfig
{
    public TaxConfig Tax { get; set; } = new();

    public MarketConfig Market { get; set; } = new();

    public KeybindList OpenReportKey { get; set; } = KeybindList.Parse("O");
}

public class MarketConfig
{
    public bool EnableShippingBinShadowPriceTest { get; set; } = false;

    public bool EnableShippingSettlementVerboseTrace { get; set; } = false;

    public bool EnableShippingBinMarketSettlement { get; set; } = true;
}

public class TaxConfig
{
    public bool EnableTaxNoticeMail { get; set; } = true;

    public bool RequireTaxNoticeSignature { get; set; } = true;

    public int BusinessPropertyTaxThreshold { get; set; } = 20;

    public BusinessPropertyDailyTaxRates BusinessPropertyDailyTaxRates { get; set; } = new();

    public List<IncomeTaxBracketConfig> IncomeTaxBrackets { get; set; } = new()
    {
        new IncomeTaxBracketConfig
        {
            MinimumTaxableIncome = 0,
            Rate = 0.00
        },
        new IncomeTaxBracketConfig
        {
            MinimumTaxableIncome = 5001,
            Rate = 0.05
        },
        new IncomeTaxBracketConfig
        {
            MinimumTaxableIncome = 20001,
            Rate = 0.08
        },
        new IncomeTaxBracketConfig
        {
            MinimumTaxableIncome = 50001,
            Rate = 0.12
        },
        new IncomeTaxBracketConfig
        {
            MinimumTaxableIncome = 100001,
            Rate = 0.15
        }
    };

    public PropertyTaxConfig PropertyTax { get; set; } = new();
}

public class IncomeTaxBracketConfig
{
    public int MinimumTaxableIncome { get; set; }

    public double Rate { get; set; }
}

public class BusinessPropertyDailyTaxRates
{
    public int Keg { get; set; } = 48;

    public int PreservesJar { get; set; } = 64;

    public int Cask { get; set; } = 8;

    public int BeeHouse { get; set; } = 34;

    public int MayonnaiseMachine { get; set; } = 260;

    public int CheesePress { get; set; } = 51;

    public int Loom { get; set; } = 26;

    public int OilMaker { get; set; } = 88;

    public int Dehydrator { get; set; } = 380;

    public int FishSmoker { get; set; } = 137;
}

public class PropertyTaxConfig
{
    public bool EnableAgriculturalDeduction { get; set; } = true;

    public double MaximumWeeklyAgriculturalDeduction { get; set; } = 1000.0;

    public double WeeklyAdministrativeFee { get; set; } = 50.0;

    public double WeeklyDocumentationFee { get; set; } = 10.0;
}
