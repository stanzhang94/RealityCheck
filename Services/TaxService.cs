using System;
using System.Collections.Generic;
using System.Linq;
using RealityCheck.Models;
using StardewValley;

namespace RealityCheck.Services;

public class TaxService
{
    private readonly LedgerService ledgerService;

    public TaxService(LedgerService ledgerService)
    {
        this.ledgerService = ledgerService;
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
        return $"Year {Game1.year} {this.FormatSeason(Game1.currentSeason)} {this.GetCurrentTaxWeekStartDay()} - {this.FormatSeason(Game1.currentSeason)} {this.GetCurrentTaxWeekEndDay()}";
    }

    public string GetNextTaxSettlementLabel()
    {
        int endDay = this.GetCurrentTaxWeekEndDay();

        if (endDay < 28)
        {
            return $"Year {Game1.year} {this.FormatSeason(Game1.currentSeason)} {endDay + 1} Morning";
        }

        string nextSeason = this.GetNextSeason(Game1.currentSeason);
        int nextYear = Game1.year;

        if (Game1.currentSeason == "winter")
            nextYear++;

        return $"Year {nextYear} {this.FormatSeason(nextSeason)} 1 Morning";
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
        // Coming soon.
        return 0;
    }

    public int GetEstimatedBusinessPropertyTax()
    {
        // Coming soon.
        return 0;
    }

    public int GetEstimatedTotalTaxDue()
    {
        return
            this.GetEstimatedIncomeTax()
            + this.GetEstimatedPropertyTax()
            + this.GetEstimatedBusinessPropertyTax();
    }

    public bool TrySettlePreviousTaxWeek(out string message)
    {
        message = "";

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

        int propertyTaxAmount = 0;
        int businessPropertyTaxAmount = 0;

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
        $"Y{record.Year} {this.FormatSeason(record.Season)} {record.CoveredStartDay}-{record.CoveredEndDay}";

    string settlementDate =
        $"Y{record.SettlementYear} {this.FormatSeason(record.SettlementSeason)} {record.SettlementDay}";

    return
        $"{coveredPeriod} -> {settlementDate}: " +
        $"Income {this.FormatTaxAmount(record.IncomeTaxAmount)} | " +
        $"Property {this.FormatTaxAmount(record.PropertyTaxAmount)} | " +
        $"Business {this.FormatTaxAmount(record.BusinessPropertyTaxAmount)}";
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

    private bool IsTaxSettlementDay()
    {
        return Game1.dayOfMonth == 1
            || Game1.dayOfMonth == 8
            || Game1.dayOfMonth == 15
            || Game1.dayOfMonth == 22;
    }

    private string FormatTaxAmount(int amount)
{
    if (amount <= 0)
        return "0g";

    return $"-{amount}g";
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
        if (amount <= 5000)
            return 0.00;

        if (amount <= 20000)
            return 0.05;

        if (amount <= 50000)
            return 0.08;

        if (amount <= 100000)
            return 0.12;

        return 0.15;
    }

    private string GetIncomeTaxBracketLabelForAmount(int amount)
    {
        if (amount <= 5000)
            return "0 - 5,000g: 0%";

        if (amount <= 20000)
            return "5,001 - 20,000g: 5%";

        if (amount <= 50000)
            return "20,001 - 50,000g: 8%";

        if (amount <= 100000)
            return "50,001 - 100,000g: 12%";

        return "100,000g+: 15%";
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
        return season switch
        {
            "spring" => "Spring",
            "summer" => "Summer",
            "fall" => "Fall",
            "winter" => "Winter",
            _ => season
        };
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