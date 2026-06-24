using System;
using System.Collections.Generic;
using System.Linq;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
 
namespace RealityCheck.Services;
 
public class TaxService
{
   private const int StandardFarmTheoreticalTillableTiles = 3427;
 
   private readonly LedgerService ledgerService;
   private readonly IMonitor? monitor;
   private readonly TaxConfig taxConfig;
   public TaxRecord? LastSettledTaxRecord { get; private set; }
 
   public TaxService(
       LedgerService ledgerService,
       IMonitor? monitor = null,
       TaxConfig? taxConfig = null
   )
   {
       this.ledgerService = ledgerService;
       this.monitor = monitor;
       this.taxConfig = taxConfig ?? ConfigService.Current.Tax;
   }
 
   public int GetCurrentTaxWeekNumber()
   {
       return ((Game1.dayOfMonth - 1) / 7) + 1;
   }
 
   public int GetCurrentTaxWeekStartDay()
   {
       return ((this.GetCurrentTaxWeekNumber() - 1) * 7) + 1;
   }
 
   public int GetCurrentTaxWeekEndDay()
   {
       return Math.Min(
           this.GetCurrentTaxWeekStartDay() + 6,
           28
       );
   }
 
   public string GetCurrentTaxWeekLabel()
   {
       return I18n.PeriodSameSeason(
           Game1.year,
           Game1.currentSeason,
           this.GetCurrentTaxWeekStartDay(),
           this.GetCurrentTaxWeekEndDay()
       );
   }
 
   public string GetNextTaxSettlementLabel()
   {
       int endDay = this.GetCurrentTaxWeekEndDay();
 
       if (endDay < 28)
       {
           return I18n.Get(
               "date.morning",
               new
               {
                   year = Game1.year,
                   season = I18n.Season(Game1.currentSeason),
                   day = endDay + 1
               }
           );
       }
 
       string nextSeason = this.GetNextSeason(Game1.currentSeason);
       int nextYear = Game1.year;
 
       if (Game1.currentSeason == "winter")
           nextYear++;
 
       return I18n.Get(
           "date.morning",
           new
           {
               year = nextYear,
               season = I18n.Season(nextSeason),
               day = 1
           }
       );
   }
 
   public int GetCurrentWeekTaxableShippingBinIncome()
   {
       int startDay = this.GetCurrentTaxWeekStartDay();
       int currentDay = Game1.dayOfMonth;
 
       return this.GetTaxableShippingBinIncomeForPeriod(
           Game1.year,
           Game1.currentSeason,
           startDay,
           currentDay
       );
   }
 
   public double GetIncomeTaxRate()
   {
       int taxableIncome = this.GetCurrentWeekTaxableShippingBinIncome();
 
       return this.GetIncomeTaxRateForAmount(taxableIncome);
   }
 
   public string GetIncomeTaxBracketLabel()
   {
       int taxableIncome = this.GetCurrentWeekTaxableShippingBinIncome();
 
       return this.GetIncomeTaxBracketLabelForAmount(taxableIncome);
   }
 
   public int GetEstimatedIncomeTax()
   {
       int taxableIncome = this.GetCurrentWeekTaxableShippingBinIncome();
       double taxRate = this.GetIncomeTaxRateForAmount(taxableIncome);
 
       return (int)Math.Floor(
           taxableIncome * taxRate
       );
   }
 
   public int GetEstimatedPropertyTax()
   {
       return this.GetPropertyTaxAmountForPeriod(
           Game1.year,
           Game1.currentSeason,
           this.GetCurrentTaxWeekStartDay(),
           Game1.dayOfMonth
       );
   }
 
   public int GetEstimatedBusinessPropertyTax()
   {
       return this.GetBusinessPropertyTaxAmountForPeriod(
           Game1.year,
           Game1.currentSeason,
           this.GetCurrentTaxWeekStartDay(),
           Game1.dayOfMonth
       );
   }
 
   public int GetEstimatedTotalTaxDue()
   {
       return
           this.GetEstimatedIncomeTax()
           + this.GetEstimatedPropertyTax()
           + this.GetEstimatedBusinessPropertyTax();
   }
 
   public void EnsureTodayPropertyTaxAssessment()
   {
       if (this.ledgerService.HasPropertyTaxDailyAssessment(
           Game1.year,
           Game1.currentSeason,
           Game1.dayOfMonth
       ))
       {
           return;
       }
 
       PropertyTaxDailyAssessment assessment =
           this.CalculateTodayPropertyTaxAssessment();
 
       this.ledgerService.AddPropertyTaxDailyAssessment(
           assessment
       );
   }
 
   public void EnsureTodayBusinessPropertyTaxAssessment()
   {
       if (this.ledgerService.HasBusinessPropertyTaxDailyAssessment(
           Game1.year,
           Game1.currentSeason,
           Game1.dayOfMonth
       ))
       {
           return;
       }
 
       BusinessPropertyTaxDailyAssessment assessment =
           this.CalculateTodayBusinessPropertyTaxAssessment();
 
       this.ledgerService.AddBusinessPropertyTaxDailyAssessment(
           assessment
       );
   }
 
   public PropertyTaxDailyAssessment CalculateTodayPropertyTaxAssessment()
   {
       double replacementCost = 0;
       double incomePotentialValue = 0;
       double utilityPremium = 0;
       double riskShieldPremium = 0;
 
       this.AddFarmhouseAssessment(
           ref replacementCost
       );
 
       Farm farm = Game1.getFarm();
 
       foreach (Building building in farm.buildings)
       {
           string buildingType = this.GetBuildingTypeName(building);
 
           BuildingPropertyTaxConfig? config =
               this.GetBuildingPropertyTaxConfig(buildingType);
 
           if (config == null)
               continue;
 
           replacementCost += config.ReplacementCostAmount;
           incomePotentialValue += config.IncomePotentialValueAmount;
           utilityPremium += config.UtilityPremiumAmount;
           riskShieldPremium += config.RiskShieldPremiumAmount;
       }
 
       if (this.IsGreenhouseUnlocked())
       {
           BuildingPropertyTaxConfig greenhouseConfig =
               this.GetGreenhousePropertyTaxConfig();
 
           replacementCost += greenhouseConfig.ReplacementCostAmount;
           incomePotentialValue += greenhouseConfig.IncomePotentialValueAmount;
           utilityPremium += greenhouseConfig.UtilityPremiumAmount;
           riskShieldPremium += greenhouseConfig.RiskShieldPremiumAmount;
       }
 
       double depreciationFactor = this.GetDepreciationFactor();
 
       double assessedPropertyValue =
           (
               replacementCost
               + incomePotentialValue
               + utilityPremium
               + riskShieldPremium
           )
           * depreciationFactor;
 
       double agriculturalDeduction =
           this.GetTodayAgriculturalDeduction();
 
       double taxablePropertyAmount = Math.Max(
           0,
           assessedPropertyValue - agriculturalDeduction
       );
 
       double totalPropertyTaxAmount =
           taxablePropertyAmount
           + this.GetDailyAdministrativeFee()
           + this.GetDailyDocumentationFee();
 
       return new PropertyTaxDailyAssessment
       {
           Year = Game1.year,
           Season = Game1.currentSeason,
           Day = Game1.dayOfMonth,
 
           ReplacementCostAmount = replacementCost,
           IncomePotentialValueAmount = incomePotentialValue,
           UtilityPremiumAmount = utilityPremium,
           RiskShieldPremiumAmount = riskShieldPremium,
           DepreciationFactor = depreciationFactor,
           AgriculturalDeductionAmount = agriculturalDeduction,
           AdministrativeFeeAmount = this.GetDailyAdministrativeFee(),
           DocumentationFeeAmount = this.GetDailyDocumentationFee(),
           TotalPropertyTaxAmount = totalPropertyTaxAmount
       };
   }
 
   public BusinessPropertyTaxDailyAssessment CalculateTodayBusinessPropertyTaxAssessment()
   {
       var assessment = new BusinessPropertyTaxDailyAssessment
       {
           Year = Game1.year,
           Season = Game1.currentSeason,
           Day = Game1.dayOfMonth
       };
 
       var locationLogLines = new List<string>();
 
       foreach (GameLocation location in this.GetBusinessPropertyTaxLocations())
       {
           int locationKegCount = 0;
           int locationPreservesJarCount = 0;
           int locationCaskCount = 0;
           int locationBeeHouseCount = 0;
           int locationMayonnaiseMachineCount = 0;
           int locationCheesePressCount = 0;
           int locationLoomCount = 0;
           int locationOilMakerCount = 0;
           int locationDehydratorCount = 0;
           int locationFishSmokerCount = 0;
 
           foreach (var pair in location.objects.Pairs)
           {
               if (pair.Value is not StardewValley.Object obj)
                   continue;
 
               string machineName = this.NormalizeObjectName(
                   obj.Name
               );
 
               switch (machineName)
               {
                   case "keg":
                       assessment.KegCount++;
                       locationKegCount++;
                       break;
 
                   case "preservesjar":
                       assessment.PreservesJarCount++;
                       locationPreservesJarCount++;
                       break;
 
                   case "cask":
                       assessment.CaskCount++;
                       locationCaskCount++;
                       break;
 
                   case "beehouse":
                       assessment.BeeHouseCount++;
                       locationBeeHouseCount++;
                       break;
 
                   case "mayonnaisemachine":
                       assessment.MayonnaiseMachineCount++;
                       locationMayonnaiseMachineCount++;
                       break;
 
                   case "cheesepress":
                       assessment.CheesePressCount++;
                       locationCheesePressCount++;
                       break;
 
                   case "loom":
                       assessment.LoomCount++;
                       locationLoomCount++;
                       break;
 
                   case "oilmaker":
                       assessment.OilMakerCount++;
                       locationOilMakerCount++;
                       break;
 
                   case "dehydrator":
                       assessment.DehydratorCount++;
                       locationDehydratorCount++;
                       break;
 
                   case "fishsmoker":
                       assessment.FishSmokerCount++;
                       locationFishSmokerCount++;
                       break;
               }
           }
 
           int locationTotal =
               locationKegCount
               + locationPreservesJarCount
               + locationCaskCount
               + locationBeeHouseCount
               + locationMayonnaiseMachineCount
               + locationCheesePressCount
               + locationLoomCount
               + locationOilMakerCount
               + locationDehydratorCount
               + locationFishSmokerCount;
 
           if (locationTotal > 0)
           {
               locationLogLines.Add(
                   $"{location.NameOrUniqueName}: " +
                   $"Keg {locationKegCount}, " +
                   $"Jar {locationPreservesJarCount}, " +
                   $"Cask {locationCaskCount}, " +
                   $"Bee {locationBeeHouseCount}, " +
                   $"Mayo {locationMayonnaiseMachineCount}, " +
                   $"Cheese {locationCheesePressCount}, " +
                   $"Loom {locationLoomCount}, " +
                   $"Oil {locationOilMakerCount}, " +
                   $"Dehydrator {locationDehydratorCount}, " +
                   $"Smoker {locationFishSmokerCount}"
               );
           }
       }
 
       assessment.TotalBusinessPropertyTaxAmount =
           this.CalculateBusinessPropertyDailyTax(assessment.KegCount, this.taxConfig.BusinessPropertyDailyTaxRates.Keg)
           + this.CalculateBusinessPropertyDailyTax(assessment.PreservesJarCount, this.taxConfig.BusinessPropertyDailyTaxRates.PreservesJar)
           + this.CalculateBusinessPropertyDailyTax(assessment.CaskCount, this.taxConfig.BusinessPropertyDailyTaxRates.Cask)
           + this.CalculateBusinessPropertyDailyTax(assessment.BeeHouseCount, this.taxConfig.BusinessPropertyDailyTaxRates.BeeHouse)
           + this.CalculateBusinessPropertyDailyTax(assessment.MayonnaiseMachineCount, this.taxConfig.BusinessPropertyDailyTaxRates.MayonnaiseMachine)
           + this.CalculateBusinessPropertyDailyTax(assessment.CheesePressCount, this.taxConfig.BusinessPropertyDailyTaxRates.CheesePress)
           + this.CalculateBusinessPropertyDailyTax(assessment.LoomCount, this.taxConfig.BusinessPropertyDailyTaxRates.Loom)
           + this.CalculateBusinessPropertyDailyTax(assessment.OilMakerCount, this.taxConfig.BusinessPropertyDailyTaxRates.OilMaker)
           + this.CalculateBusinessPropertyDailyTax(assessment.DehydratorCount, this.taxConfig.BusinessPropertyDailyTaxRates.Dehydrator)
           + this.CalculateBusinessPropertyDailyTax(assessment.FishSmokerCount, this.taxConfig.BusinessPropertyDailyTaxRates.FishSmoker);
 
       this.LogBusinessPropertyTaxScan(
           assessment,
           locationLogLines
       );
 
       return assessment;
   }
 
  public bool TrySettlePreviousTaxWeek(out string message)
{
    message = "";
    this.LastSettledTaxRecord = null;

    if (!this.IsTaxSettlementDay())
        return false;

    if (!this.TryGetPreviousTaxWeek(out TaxWeekInfo previousWeek))
        return false;

    if (this.HasTaxRecordForPeriod(
        previousWeek.Year,
        previousWeek.Season,
        previousWeek.StartDay,
        previousWeek.EndDay
    ))
    {
        message =
            $"Tax settlement skipped: record already exists for {this.FormatSeason(previousWeek.Season)} {previousWeek.StartDay}-{previousWeek.EndDay}.";

        return false;
    }

    int taxableShippingBinIncome = this.GetTaxableShippingBinIncomeForPeriod(
        previousWeek.Year,
        previousWeek.Season,
        previousWeek.StartDay,
        previousWeek.EndDay
    );

    double incomeTaxRate = this.GetIncomeTaxRateForAmount(
        taxableShippingBinIncome
    );

    int incomeTaxAmount = (int)Math.Floor(
        taxableShippingBinIncome * incomeTaxRate
    );

    int propertyTaxAmount = this.GetPropertyTaxAmountForPeriod(
        previousWeek.Year,
        previousWeek.Season,
        previousWeek.StartDay,
        previousWeek.EndDay
    );

    int businessPropertyTaxAmount = this.GetBusinessPropertyTaxAmountForPeriod(
        previousWeek.Year,
        previousWeek.Season,
        previousWeek.StartDay,
        previousWeek.EndDay
    );

    int totalTaxAmount =
        incomeTaxAmount
        + propertyTaxAmount
        + businessPropertyTaxAmount;

    if (totalTaxAmount <= 0)
    {
        message =
            $"No tax due for {this.FormatSeason(previousWeek.Season)} {previousWeek.StartDay}-{previousWeek.EndDay}.";

        return false;
    }

    if (incomeTaxAmount > 0)
    {
        this.ledgerService.ChargeObligation(
            "Reality Check",
            "Income Tax",
            incomeTaxAmount
        );
    }

    if (propertyTaxAmount > 0)
    {
        this.ledgerService.ChargeObligation(
            "Reality Check",
            "Property Tax",
            propertyTaxAmount
        );
    }

    if (businessPropertyTaxAmount > 0)
    {
        this.ledgerService.ChargeObligation(
            "Reality Check",
            "Business Property Tax",
            businessPropertyTaxAmount
        );
    }

    var record = new TaxRecord
    {
        Year = previousWeek.Year,
        Season = previousWeek.Season,
        WeekNumber = previousWeek.WeekNumber,
        CoveredStartDay = previousWeek.StartDay,
        CoveredEndDay = previousWeek.EndDay,

        SettlementYear = Game1.year,
        SettlementSeason = Game1.currentSeason,
        SettlementDay = Game1.dayOfMonth,

        TaxableShippingBinIncome = taxableShippingBinIncome,
        IncomeTaxRate = incomeTaxRate,

        IncomeTaxAmount = incomeTaxAmount,
        PropertyTaxAmount = propertyTaxAmount,
        BusinessPropertyTaxAmount = businessPropertyTaxAmount,

        TotalTaxAmount = totalTaxAmount
    };

    this.ledgerService.AddTaxRecord(record);

    this.LastSettledTaxRecord = record;

    message =
        $"Weekly tax settled: {this.FormatSeason(previousWeek.Season)} {previousWeek.StartDay}-{previousWeek.EndDay}, total -{totalTaxAmount}g.";

    return true;
}
   public List<TaxRecord> GetRecentTaxRecords(int maxRecords = 16)
   {
       return this.ledgerService.GetTaxRecords()
           .OrderByDescending(r => this.GetTaxRecordSortKey(r))
           .Take(maxRecords)
           .OrderBy(r => this.GetTaxRecordSortKey(r))
           .ToList();
   }
 
   public string GetTaxRecordSummaryLine(TaxRecord record)
   {
       string coveredPeriod =
           I18n.Get(
               "tax_history.covered_period",
               new
               {
                   year = record.Year,
                   season = I18n.Season(record.Season),
                   startDay = record.CoveredStartDay,
                   endDay = record.CoveredEndDay
               }
           );

       string settlementDate =
           I18n.Get(
               "tax_history.settlement_date",
               new
               {
                   year = record.SettlementYear,
                   season = I18n.Season(record.SettlementSeason),
                   day = record.SettlementDay
               }
           );

       return I18n.Get(
           "tax_history.summary_line",
           new
           {
               coveredPeriod,
               settlementDate,
               income = this.FormatTaxAmount(record.IncomeTaxAmount),
               property = this.FormatTaxAmount(record.PropertyTaxAmount),
               business = this.FormatTaxAmount(record.BusinessPropertyTaxAmount)
           }
       );
   }
 
   public void AddTaxRecord(TaxRecord record)
   {
       this.ledgerService.AddTaxRecord(record);
   }
 
   public string FormatTaxRatePercent(double rate)
   {
       int percent = (int)Math.Round(rate * 100);
 
       return $"{percent}%";
   }
 
   private int GetPropertyTaxAmountForPeriod(
       int year,
       string season,
       int startDay,
       int endDay
   )
   {
       double total = this.ledgerService.GetPropertyTaxDailyAssessments()
           .Where(a =>
               a.Year == year
               && a.Season == season
               && a.Day >= startDay
               && a.Day <= endDay
           )
           .Sum(a => a.TotalPropertyTaxAmount);
 
       return (int)Math.Round(
           total,
           MidpointRounding.AwayFromZero
       );
   }
 
   private int GetBusinessPropertyTaxAmountForPeriod(
       int year,
       string season,
       int startDay,
       int endDay
   )
   {
       return this.ledgerService.GetBusinessPropertyTaxDailyAssessments()
           .Where(a =>
               a.Year == year
               && a.Season == season
               && a.Day >= startDay
               && a.Day <= endDay
           )
           .Sum(a => a.TotalBusinessPropertyTaxAmount);
   }
 
   private List<GameLocation> GetBusinessPropertyTaxLocations()
   {
       var result = new List<GameLocation>();
       var seen = new HashSet<GameLocation>();
 
       void AddLocation(GameLocation? location)
       {
           if (location == null)
               return;
 
           if (!this.ShouldIncludeBusinessPropertyTaxLocation(location))
               return;
 
           if (seen.Add(location))
               result.Add(location);
       }
 
       foreach (GameLocation location in Game1.locations)
       {
           AddLocation(location);
       }
 
       Farm farm = Game1.getFarm();
 
       foreach (Building building in farm.buildings)
       {
           AddLocation(building.GetIndoors());
       }
 
       return result;
   }
 
   private bool ShouldIncludeBusinessPropertyTaxLocation(GameLocation location)
   {
       string locationName = location.NameOrUniqueName ?? location.Name ?? "";
 
       string normalizedName = locationName
           .Replace(" ", "")
           .Replace("_", "")
           .Replace("-", "")
           .ToLowerInvariant();
 
       if (normalizedName.StartsWith("cellar"))
       {
           if (normalizedName == "cellar")
               return this.GetHouseUpgradeLevel() >= 3;
 
           return false;
       }
 
       return true;
   }
 
   private int CalculateBusinessPropertyDailyTax(int count, int dailyTaxRate)
   {
       int taxableCount = this.GetTaxableBusinessPropertyCount(count);

       if (taxableCount <= 0)
           return 0;

       double scaleMultiplier = this.GetBusinessPropertyTaxScaleMultiplier(count);

       return (int)Math.Round(
           taxableCount * dailyTaxRate * scaleMultiplier,
           MidpointRounding.AwayFromZero
       );
   }

   private int GetTaxableBusinessPropertyCount(int count)
   {
       if (count <= this.taxConfig.BusinessPropertyTaxThreshold)
           return 0;

       return count;
   }

   private double GetBusinessPropertyTaxScaleMultiplier(int count)
   {
       if (count >= 100)
           return 2.0;

       if (count >= 50)
           return 1.5;

       return 1.0;
   }
 
private void LogBusinessPropertyTaxScan(
    BusinessPropertyTaxDailyAssessment assessment,
    List<string> locationLogLines
)
{
    if (this.monitor == null)
        return;

    this.monitor.Log(
        "=== Reality Check Business Property Tax Scan ===",
        LogLevel.Trace
    );

    this.monitor.Log(
        $"Date: Year {assessment.Year} {assessment.Season} {assessment.Day}",
        LogLevel.Trace
    );

    if (locationLogLines.Count == 0)
    {
        this.monitor.Log(
            "No taxable business machines found in scanned locations.",
            LogLevel.Trace
        );
    }
    else
    {
        foreach (string line in locationLogLines)
        {
            this.monitor.Log(
                line,
                LogLevel.Trace
            );
        }
    }

    this.monitor.Log(
        "Totals: " +
        $"Keg {assessment.KegCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.KegCount)}, " +
        $"Jar {assessment.PreservesJarCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.PreservesJarCount)}, " +
        $"Cask {assessment.CaskCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.CaskCount)}, " +
        $"Bee {assessment.BeeHouseCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.BeeHouseCount)}, " +
        $"Mayo {assessment.MayonnaiseMachineCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.MayonnaiseMachineCount)}, " +
        $"Cheese {assessment.CheesePressCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.CheesePressCount)}, " +
        $"Loom {assessment.LoomCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.LoomCount)}, " +
        $"Oil {assessment.OilMakerCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.OilMakerCount)}, " +
        $"Dehydrator {assessment.DehydratorCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.DehydratorCount)}, " +
        $"Smoker {assessment.FishSmokerCount} / taxable {this.GetTaxableBusinessPropertyCount(assessment.FishSmokerCount)}",
        LogLevel.Trace
    );

    this.monitor.Log(
        $"Business Property Tax Total Today: {assessment.TotalBusinessPropertyTaxAmount}g",
        LogLevel.Trace
    );

    this.monitor.Log(
        "==============================================",
        LogLevel.Trace
    );
}
   private void AddFarmhouseAssessment(
       ref double replacementCost
   )
   {
       int houseUpgradeLevel = this.GetHouseUpgradeLevel();
 
       if (houseUpgradeLevel <= 0)
           return;
 
       if (houseUpgradeLevel == 1)
       {
           replacementCost += 10000.0 / 80.0 / 7.0;
           return;
       }
 
       if (houseUpgradeLevel == 2)
       {
           replacementCost += 75000.0 / 80.0 / 7.0;
           return;
       }
 
       replacementCost += 175000.0 / 80.0 / 7.0;
   }
 
   private BuildingPropertyTaxConfig? GetBuildingPropertyTaxConfig(
       string buildingType
   )
   {
       string key = this.NormalizeBuildingType(buildingType);
 
       return key switch
       {
           "coop" => this.CreateConfig(
               replacementCost: 4000.0 / 80.0 / 7.0,
               incomePotentialValue: 4.0 * 175.0 / 7.0,
               utilityPremium: 0,
               riskShieldPremium: 0
           ),
 
           "bigcoop" => this.CreateConfig(
               replacementCost: 14000.0 / 80.0 / 7.0,
               incomePotentialValue: 8.0 * 175.0 / 7.0,
               utilityPremium: 0,
               riskShieldPremium: 0
           ),
 
           "deluxecoop" => this.CreateConfig(
               replacementCost: 34000.0 / 80.0 / 7.0,
               incomePotentialValue: 12.0 * 175.0 / 7.0,
               utilityPremium: 0,
               riskShieldPremium: 0
           ),
 
           "barn" => this.CreateConfig(
               replacementCost: 6000.0 / 80.0 / 7.0,
               incomePotentialValue: 4.0 * 175.0 / 7.0,
               utilityPremium: 0,
               riskShieldPremium: 0
           ),
 
           "bigbarn" => this.CreateConfig(
               replacementCost: 18000.0 / 80.0 / 7.0,
               incomePotentialValue: 8.0 * 175.0 / 7.0,
               utilityPremium: 0,
               riskShieldPremium: 0
           ),
 
           "deluxebarn" => this.CreateConfig(
               replacementCost: 43000.0 / 80.0 / 7.0,
               incomePotentialValue: 12.0 * 175.0 / 7.0,
               utilityPremium: 0,
               riskShieldPremium: 0
           ),
 
           "fishpond" => this.CreateUtilityConfig(
               5000.0
           ),
 
           "mill" => this.CreateUtilityConfig(
               2500.0
           ),
 
           "shed" => this.CreateUtilityConfig(
               15000.0
           ),
 
           "bigshed" => this.CreateUtilityConfig(
               35000.0
           ),
 
           "silo" => this.CreateUtilityConfig(
               100.0
           ),
 
           "slimehutch" => this.CreateUtilityConfig(
               10000.0
           ),
 
           "stable" => this.CreateUtilityConfig(
               10000.0
           ),
 
           "well" => this.CreateUtilityConfig(
               1000.0
           ),
 
           "cabin" => this.CreateConfig(
               replacementCost: 100.0 / 80.0 / 7.0,
               incomePotentialValue: 0,
               utilityPremium: 0,
               riskShieldPremium: 0
           ),
 
           "shippingbin" => this.CreateUtilityConfig(
               250.0
           ),
 
           "petbowl" => this.CreateUtilityConfig(
               5000.0
           ),
 
           "earthobelisk" => this.CreateUtilityConfig(
               500000.0
           ),
 
           "waterobelisk" => this.CreateUtilityConfig(
               500000.0
           ),
 
           "desertobelisk" => this.CreateUtilityConfig(
               1000000.0
           ),
 
           "islandobelisk" => this.CreateUtilityConfig(
               1000000.0
           ),
 
           "junimohut" => this.CreateUtilityConfig(
               20000.0
           ),
 
           "goldclock" => this.CreateUtilityConfig(
               10000000.0
           ),
 
           _ => null
       };
   }
 
   private BuildingPropertyTaxConfig GetGreenhousePropertyTaxConfig()
   {
       double replacementCost = 35000.0 / 80.0 / 7.0;
       double incomePotentialValue = 120.0 * 13.125 / 7.0;
       double riskShieldPremium = incomePotentialValue * 2.0;
 
       return this.CreateConfig(
           replacementCost: replacementCost,
           incomePotentialValue: incomePotentialValue,
           utilityPremium: 0,
           riskShieldPremium: riskShieldPremium
       );
   }
 
   private BuildingPropertyTaxConfig CreateUtilityConfig(
       double constructionCost
   )
   {
       double replacementCost = constructionCost / 80.0 / 7.0;
       double utilityPremium = replacementCost * 0.2;
 
       return this.CreateConfig(
           replacementCost: replacementCost,
           incomePotentialValue: 0,
           utilityPremium: utilityPremium,
           riskShieldPremium: 0
       );
   }
 
   private BuildingPropertyTaxConfig CreateConfig(
       double replacementCost,
       double incomePotentialValue,
       double utilityPremium,
       double riskShieldPremium
   )
   {
       return new BuildingPropertyTaxConfig
       {
           ReplacementCostAmount = replacementCost,
           IncomePotentialValueAmount = incomePotentialValue,
           UtilityPremiumAmount = utilityPremium,
           RiskShieldPremiumAmount = riskShieldPremium
       };
   }
 
   private double GetDailyAdministrativeFee()
   {
       return this.taxConfig.PropertyTax.WeeklyAdministrativeFee / 7.0;
   }

   private double GetDailyDocumentationFee()
   {
       return this.taxConfig.PropertyTax.WeeklyDocumentationFee / 7.0;
   }

   private double GetMaxDailyAgriculturalDeduction()
   {
       return this.taxConfig.PropertyTax.MaximumWeeklyAgriculturalDeduction / 7.0;
   }

   private double GetDepreciationFactor()
   {
       return Game1.year switch
       {
           <= 1 => 1.00,
           2 => 0.95,
           3 => 0.90,
           4 => 0.85,
           _ => 0.80
       };
   }
 
   private double GetTodayAgriculturalDeduction()
   {
       if (!this.taxConfig.PropertyTax.EnableAgriculturalDeduction)
           return 0;

       int plantedOutdoorTiles = this.CountOutdoorPlantedTiles();
 
       double plantingRatio = plantedOutdoorTiles
           / (double)StandardFarmTheoreticalTillableTiles;
 
       plantingRatio = Math.Clamp(
           plantingRatio,
           0,
           1
       );
 
       return this.GetMaxDailyAgriculturalDeduction() * plantingRatio;
   }
 
   private int CountOutdoorPlantedTiles()
   {
       Farm farm = Game1.getFarm();
 
       int count = 0;
 
       foreach (var pair in farm.terrainFeatures.Pairs)
       {
           if (pair.Value is HoeDirt dirt && dirt.crop != null)
               count++;
       }
 
       return count;
   }
 
   private bool IsGreenhouseUnlocked()
   {
       Farm farm = Game1.getFarm();
 
       var field = farm.GetType().GetField("greenhouseUnlocked");
 
       object? rawValue = field?.GetValue(farm);
 
       if (rawValue == null)
       {
           var property = farm.GetType().GetProperty("greenhouseUnlocked");
           rawValue = property?.GetValue(farm);
       }
 
       if (rawValue != null)
       {
           var valueProperty = rawValue.GetType().GetProperty("Value");
 
           object? value = valueProperty?.GetValue(rawValue);
 
           if (value is bool isUnlocked)
               return isUnlocked;
       }
 
       return Game1.MasterPlayer.mailReceived.Contains("ccPantry")
           || Game1.MasterPlayer.mailReceived.Contains("jojaPantry");
   }
 
   private int GetHouseUpgradeLevel()
   {
       var property = Game1.player.GetType()
           .GetProperty("HouseUpgradeLevel");
 
       object? value = property?.GetValue(Game1.player);
 
       if (value is int level)
           return level;
 
       return 0;
   }
 
   private string GetBuildingTypeName(Building building)
   {
       return building.buildingType.Value ?? "";
   }
 
   private string NormalizeBuildingType(string buildingType)
   {
       return buildingType
           .Replace(" ", "")
           .Replace("_", "")
           .Replace("-", "")
           .ToLowerInvariant();
   }
 
   private string NormalizeObjectName(string objectName)
   {
       return objectName
           .Replace(" ", "")
           .Replace("_", "")
           .Replace("-", "")
           .ToLowerInvariant();
   }
 
   private bool IsTaxSettlementDay()
   {
       return Game1.dayOfMonth == 1
           || Game1.dayOfMonth == 8
           || Game1.dayOfMonth == 15
           || Game1.dayOfMonth == 22;
   }
 
   private bool TryGetPreviousTaxWeek(out TaxWeekInfo previousWeek)
   {
       previousWeek = new TaxWeekInfo();
 
       if (Game1.dayOfMonth == 8)
       {
           previousWeek = new TaxWeekInfo
           {
               Year = Game1.year,
               Season = Game1.currentSeason,
               WeekNumber = 1,
               StartDay = 1,
               EndDay = 7
           };
 
           return true;
       }
 
       if (Game1.dayOfMonth == 15)
       {
           previousWeek = new TaxWeekInfo
           {
               Year = Game1.year,
               Season = Game1.currentSeason,
               WeekNumber = 2,
               StartDay = 8,
               EndDay = 14
           };
 
           return true;
       }
 
       if (Game1.dayOfMonth == 22)
       {
           previousWeek = new TaxWeekInfo
           {
               Year = Game1.year,
               Season = Game1.currentSeason,
               WeekNumber = 3,
               StartDay = 15,
               EndDay = 21
           };
 
           return true;
       }
 
       if (Game1.dayOfMonth == 1)
       {
           string previousSeason = this.GetPreviousSeason(Game1.currentSeason);
           int previousYear = Game1.year;
 
           if (Game1.currentSeason == "spring")
               previousYear--;
 
           if (previousYear <= 0)
               return false;
 
           previousWeek = new TaxWeekInfo
           {
               Year = previousYear,
               Season = previousSeason,
               WeekNumber = 4,
               StartDay = 22,
               EndDay = 28
           };
 
           return true;
       }
 
       return false;
   }
 
   private bool HasTaxRecordForPeriod(
       int year,
       string season,
       int startDay,
       int endDay
   )
   {
       return this.ledgerService.GetTaxRecords()
           .Any(r =>
               r.Year == year
               && r.Season == season
               && r.CoveredStartDay == startDay
               && r.CoveredEndDay == endDay
           );
   }
 
   private int GetTaxableShippingBinIncomeForPeriod(
       int year,
       string season,
       int startDay,
       int endDay
   )
   {
       return this.ledgerService.GetEntries()
           .Where(e =>
               e.Type == "Income"
               && e.Year == year
               && e.Season == season
               && e.Day >= startDay
               && e.Day <= endDay
               && this.IsShippingBinIncome(e)
           )
           .Sum(e => e.Amount);
   }
 
   private double GetIncomeTaxRateForAmount(int amount)
   {
       IncomeTaxBracketConfig bracket = this.taxConfig.IncomeTaxBrackets
           .OrderBy(b => b.MinimumTaxableIncome)
           .LastOrDefault(b => amount >= b.MinimumTaxableIncome)
           ?? new IncomeTaxBracketConfig
           {
               MinimumTaxableIncome = 0,
               Rate = 0.00
           };

       return bracket.Rate;
   }

   private string GetIncomeTaxBracketLabelForAmount(int amount)
   {
       List<IncomeTaxBracketConfig> brackets = this.taxConfig.IncomeTaxBrackets
           .OrderBy(b => b.MinimumTaxableIncome)
           .ToList();

       if (brackets.Count == 0)
           return "0g+: 0%";

       for (int i = 0; i < brackets.Count; i++)
       {
           IncomeTaxBracketConfig current = brackets[i];
           IncomeTaxBracketConfig? next = i + 1 < brackets.Count
               ? brackets[i + 1]
               : null;

           bool applies = next == null
               ? amount >= current.MinimumTaxableIncome
               : amount >= current.MinimumTaxableIncome
                   && amount < next.MinimumTaxableIncome;

           if (!applies)
               continue;

           string minimum = this.FormatTaxAmount(current.MinimumTaxableIncome);
           string maximum = next == null
               ? "+"
               : $" - {this.FormatTaxAmount(next.MinimumTaxableIncome - 1)}";

           return $"{minimum}{maximum}: {this.FormatTaxRatePercent(current.Rate)}";
       }

       IncomeTaxBracketConfig first = brackets[0];

       return $"{this.FormatTaxAmount(first.MinimumTaxableIncome)}+: {this.FormatTaxRatePercent(first.Rate)}";
   }

   private bool IsShippingBinIncome(LedgerEntry entry)
   {
       if (string.IsNullOrWhiteSpace(entry.Source))
           return false;
 
       string normalizedSource = entry.Source
           .Replace(" ", "")
           .Replace("_", "")
           .Replace("-", "")
           .ToLowerInvariant();
 
       return normalizedSource == "shippingbin"
           || normalizedSource.Contains("shippingbin");
   }
 
   private long GetTaxRecordSortKey(TaxRecord record)
   {
       int seasonIndex = this.GetSeasonIndex(record.SettlementSeason);
 
       return
           (record.SettlementYear * 10000L)
           + (seasonIndex * 100L)
           + record.SettlementDay;
   }
 
   private int GetSeasonIndex(string season)
   {
       return season switch
       {
           "spring" => 1,
           "summer" => 2,
           "fall" => 3,
           "winter" => 4,
           _ => 0
       };
   }
 
   private string GetNextSeason(string season)
   {
       return season switch
       {
           "spring" => "summer",
           "summer" => "fall",
           "fall" => "winter",
           "winter" => "spring",
           _ => "spring"
       };
   }
 
   private string GetPreviousSeason(string season)
   {
       return season switch
       {
           "spring" => "winter",
           "summer" => "spring",
           "fall" => "summer",
           "winter" => "fall",
           _ => "spring"
       };
   }
 
   private string FormatSeason(string season)
   {
       return I18n.Season(season);
   }
 
   private string FormatTaxAmount(int amount)
   {
       if (amount <= 0)
           return "0g";
 
       return $"-{amount}g";
   }
 
   private class BuildingPropertyTaxConfig
   {
       public double ReplacementCostAmount { get; set; }
 
       public double IncomePotentialValueAmount { get; set; }
 
       public double UtilityPremiumAmount { get; set; }
 
       public double RiskShieldPremiumAmount { get; set; }
   }
 
   private class TaxWeekInfo
   {
       public int Year { get; set; }
 
       public string Season { get; set; } = "";
 
       public int WeekNumber { get; set; }
 
       public int StartDay { get; set; }
 
       public int EndDay { get; set; }
   }
}