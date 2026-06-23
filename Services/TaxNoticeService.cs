using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck.Services;

public class TaxNoticeService
{
    private const int BusinessPropertyTaxThreshold = 20;

    private const int KegDailyTax = 48;
    private const int PreservesJarDailyTax = 64;
    private const int CaskDailyTax = 8;
    private const int BeeHouseDailyTax = 34;
    private const int MayonnaiseMachineDailyTax = 260;
    private const int CheesePressDailyTax = 51;
    private const int LoomDailyTax = 26;
    private const int OilMakerDailyTax = 88;
    private const int DehydratorDailyTax = 380;
    private const int FishSmokerDailyTax = 137;

    private readonly LedgerService ledgerService;
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    public TaxNoticeService(
        LedgerService ledgerService,
        IModHelper helper,
        IMonitor monitor
    )
    {
        this.ledgerService = ledgerService;
        this.helper = helper;
        this.monitor = monitor;
    }

    public void DeliverWeeklyTaxNotice(TaxRecord record)
    {
        string mailId = this.GetMailId(record);

        this.helper.GameContent.InvalidateCache("Data/Mail");

        try
        {
            Dictionary<string, string> mailData =
                Game1.content.Load<Dictionary<string, string>>("Data\\Mail");

            if (!mailData.ContainsKey(mailId))
            {
                this.monitor.Log(
                    $"Weekly tax notice mail body was not found after cache refresh: {mailId}",
                    LogLevel.Warn
                );
            }
        }
        catch (Exception ex)
        {
            this.monitor.Log(
                $"Failed to preload Data/Mail for weekly tax notice: {ex}",
                LogLevel.Warn
            );
        }

        if (!Game1.player.mailReceived.Contains(mailId)
            && !Game1.mailbox.Contains(mailId))
        {
            Game1.mailbox.Add(mailId);

            this.monitor.Log(
                $"Weekly tax notice delivered: {mailId}",
                LogLevel.Info
            );
        }
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            return;

        e.Edit(asset =>
        {
            IDictionary<string, string> mailData =
                asset.AsDictionary<string, string>().Data;

            foreach (TaxRecord record in this.ledgerService.GetTaxRecords())
            {
                string mailId = this.GetMailId(record);

                mailData[mailId] = this.BuildMailBody(record);
            }
        });
    }

    private string GetMailId(TaxRecord record)
    {
        return
            $"RC_WeeklyTaxNotice_" +
            $"Y{record.Year}_" +
            $"{record.Season}_" +
            $"{record.CoveredStartDay}_{record.CoveredEndDay}";
    }

    private string BuildMailBody(TaxRecord record)
    {
        var lines = new List<string>();

        lines.Add("Pelican Town Revenue Service");
        lines.Add("Pelican Town Property Assessment Office");
        lines.Add("");
        lines.Add("Joint Weekly Tax Notice");
        lines.Add("");
        lines.Add("Issued jointly by the Pelican Town Revenue Service and the Pelican Town Property Assessment Office.");
        lines.Add("");
        lines.Add("Thank you for your continued support of Pelican Town's fiscal development.");
        lines.Add("");
        lines.Add($"Tax Period: Year {record.Year} {this.FormatSeason(record.Season)} {record.CoveredStartDay} - {this.FormatSeason(record.Season)} {record.CoveredEndDay}");
        lines.Add($"Settlement Date: Year {record.SettlementYear} {this.FormatSeason(record.SettlementSeason)} {record.SettlementDay}");
        lines.Add("");
        lines.Add("Your weekly tax assessment has been processed as follows:");
        lines.Add("");

        this.AddIncomeTaxSection(lines, record);
        this.AddPropertyTaxSection(lines, record);
        this.AddBusinessPropertyTaxSection(lines, record);
        this.AddTotalSection(lines, record);
        this.AddAppealSection(lines, record);

        string body = string.Join("^", lines);

        return $"{body}[#]Joint Weekly Tax Notice";
    }

    private void AddIncomeTaxSection(
        List<string> lines,
        TaxRecord record
    )
    {
        lines.Add("Income Tax");
        lines.Add("");
        lines.Add($"Taxable Shipping Bin Income: {this.FormatGold(record.TaxableShippingBinIncome)}");
        lines.Add($"Applied Tax Rate: {this.FormatPercent(record.IncomeTaxRate)}");
        lines.Add($"Income Tax: {this.FormatGold(record.IncomeTaxAmount)}");
        lines.Add("");
    }

    private void AddPropertyTaxSection(
        List<string> lines,
        TaxRecord record
    )
    {
        List<PropertyTaxDailyAssessment> assessments =
            this.GetPropertyTaxAssessmentsForRecord(record);

        double replacementCost = assessments.Sum(a => a.ReplacementCostAmount);
        double incomePotentialValue = assessments.Sum(a => a.IncomePotentialValueAmount);
        double utilityPremium = assessments.Sum(a => a.UtilityPremiumAmount);
        double riskShieldPremium = assessments.Sum(a => a.RiskShieldPremiumAmount);

        double depreciationFactor = assessments.Count > 0
            ? assessments[0].DepreciationFactor
            : 1.0;

        double agriculturalDeduction = assessments.Sum(a => a.AgriculturalDeductionAmount);
        double administrativeFee = assessments.Sum(a => a.AdministrativeFeeAmount);
        double documentationFee = assessments.Sum(a => a.DocumentationFeeAmount);

        lines.Add("Property Tax");
        lines.Add("");
        lines.Add($"Replacement Cost (RC): {this.FormatGold(replacementCost)}");
        lines.Add($"Income Potential Value (IPV): {this.FormatGold(incomePotentialValue)}");
        lines.Add($"Utility Premium (UP): {this.FormatGold(utilityPremium)}");
        lines.Add($"Risk Shield Premium (RSP): {this.FormatGold(riskShieldPremium)}");
        lines.Add($"Depreciation Factor: {this.FormatPercent(depreciationFactor)}");
        lines.Add($"Agricultural Deduction (AD): -{this.FormatGoldValueOnly(agriculturalDeduction)}");
        lines.Add($"Administrative Fee: {this.FormatGold(administrativeFee)}");
        lines.Add($"Documentation Fee: {this.FormatGold(documentationFee)}");
        lines.Add($"Property Tax: {this.FormatGold(record.PropertyTaxAmount)}");
        lines.Add("");
    }

    private void AddBusinessPropertyTaxSection(
        List<string> lines,
        TaxRecord record
    )
    {
        List<BusinessPropertyTaxDailyAssessment> assessments =
            this.GetBusinessPropertyTaxAssessmentsForRecord(record);

        var businessLines = new List<string>();

        this.AddBusinessMachineLine(
            businessLines,
            "Keg",
            assessments,
            a => a.KegCount,
            KegDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Preserves Jar",
            assessments,
            a => a.PreservesJarCount,
            PreservesJarDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Cask",
            assessments,
            a => a.CaskCount,
            CaskDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Bee House",
            assessments,
            a => a.BeeHouseCount,
            BeeHouseDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Mayonnaise Machine",
            assessments,
            a => a.MayonnaiseMachineCount,
            MayonnaiseMachineDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Cheese Press",
            assessments,
            a => a.CheesePressCount,
            CheesePressDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Loom",
            assessments,
            a => a.LoomCount,
            LoomDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Oil Maker",
            assessments,
            a => a.OilMakerCount,
            OilMakerDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Dehydrator",
            assessments,
            a => a.DehydratorCount,
            DehydratorDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Fish Smoker",
            assessments,
            a => a.FishSmokerCount,
            FishSmokerDailyTax
        );

        lines.Add("Business Property Tax");
        lines.Add("");

        if (businessLines.Count == 0)
        {
            lines.Add("No assessed business equipment exceeded the taxable threshold.");
        }
        else
        {
            foreach (string line in businessLines)
                lines.Add(line);

            lines.Add("");
            lines.Add($"Business Property Tax Total: {this.FormatGold(record.BusinessPropertyTaxAmount)}");
        }

        lines.Add("");
    }

    private void AddBusinessMachineLine(
        List<string> lines,
        string displayName,
        List<BusinessPropertyTaxDailyAssessment> assessments,
        Func<BusinessPropertyTaxDailyAssessment, int> countSelector,
        int dailyTax
    )
    {
        var groups = assessments
            .Select(a => this.GetTaxableBusinessPropertyCount(countSelector(a)))
            .Where(count => count > 0)
            .GroupBy(count => count)
            .Select(g => new BusinessMachineGroup
            {
                Count = g.Key,
                Days = g.Count()
            })
            .OrderBy(g => g.Count)
            .ToList();

        if (groups.Count == 0)
            return;

        int totalAmount = groups.Sum(g =>
            g.Count * g.Days * dailyTax
        );

        string expression;

        if (groups.Count == 1)
        {
            BusinessMachineGroup group = groups[0];

            expression =
                $"{group.Count} x {group.Days} x {dailyTax}g";
        }
        else
        {
            string inner = string.Join(
                " / ",
                groups.Select(g => $"{g.Count} x {g.Days}")
            );

            expression =
                $"{inner} x {dailyTax}g";
        }

        lines.Add(
            $"{displayName}: {expression}, Tax: {this.FormatGold(totalAmount)}"
        );
    }

    private void AddTotalSection(
        List<string> lines,
        TaxRecord record
    )
    {
        lines.Add("Total Tax Due");
        lines.Add("");
        lines.Add($"Income Tax: {this.FormatGold(record.IncomeTaxAmount)}");
        lines.Add($"Property Tax: {this.FormatGold(record.PropertyTaxAmount)}");
        lines.Add($"Business Property Tax: {this.FormatGold(record.BusinessPropertyTaxAmount)}");
        lines.Add("");
        lines.Add($"Total: {this.FormatGold(record.TotalTaxAmount)}");
        lines.Add("");
    }

    private void AddAppealSection(
        List<string> lines,
        TaxRecord record
    )
    {
        string period =
            $"Year {record.Year} {this.FormatSeason(record.Season)} {record.CoveredStartDay}-{record.CoveredEndDay}";

        lines.Add("If you have questions regarding this assessment, or wish to file an appeal, please submit a comment on Nexus Mods - Reality Check - Posts using one of the following formats:");
        lines.Add("");
        lines.Add($"[Question] Weekly Tax Notice - {period}");
        lines.Add($"[Appeal] Weekly Tax Notice - {period}");
        lines.Add("");
        lines.Add("Please note that submitting an appeal does not guarantee adjustment, refund, review priority, or emotional closure of any kind.");
    }

    private List<PropertyTaxDailyAssessment> GetPropertyTaxAssessmentsForRecord(
        TaxRecord record
    )
    {
        return this.ledgerService.GetPropertyTaxDailyAssessments()
            .Where(a =>
                a.Year == record.Year
                && a.Season == record.Season
                && a.Day >= record.CoveredStartDay
                && a.Day <= record.CoveredEndDay
            )
            .OrderBy(a => a.Day)
            .ToList();
    }

    private List<BusinessPropertyTaxDailyAssessment> GetBusinessPropertyTaxAssessmentsForRecord(
        TaxRecord record
    )
    {
        return this.ledgerService.GetBusinessPropertyTaxDailyAssessments()
            .Where(a =>
                a.Year == record.Year
                && a.Season == record.Season
                && a.Day >= record.CoveredStartDay
                && a.Day <= record.CoveredEndDay
            )
            .OrderBy(a => a.Day)
            .ToList();
    }

    private int GetTaxableBusinessPropertyCount(int count)
    {
        if (count <= BusinessPropertyTaxThreshold)
            return 0;

        return count;
    }

    private int RoundMoney(double amount)
    {
        return (int)Math.Round(
            amount,
            MidpointRounding.AwayFromZero
        );
    }

    private string FormatGold(double amount)
    {
        return this.FormatGold(
            this.RoundMoney(amount)
        );
    }

    private string FormatGold(int amount)
    {
        return $"{amount.ToString("N0", CultureInfo.InvariantCulture)}g";
    }

    private string FormatGoldValueOnly(double amount)
    {
        int rounded = this.RoundMoney(amount);

        return $"{rounded.ToString("N0", CultureInfo.InvariantCulture)}g";
    }

    private string FormatPercent(double rate)
    {
        int percent = (int)Math.Round(
            rate * 100,
            MidpointRounding.AwayFromZero
        );

        return $"{percent}%";
    }

    private string FormatSeason(string season)
    {
        return season switch
        {
            "spring" => "Spring",
            "summer" => "Summer",
            "fall" => "Fall",
            "winter" => "Winter",
            _ => season
        };
    }

    private class BusinessMachineGroup
    {
        public int Count { get; set; }

        public int Days { get; set; }
    }
}