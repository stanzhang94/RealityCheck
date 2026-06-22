using System;
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

        return this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
                && e.Season == Game1.currentSeason
                && e.Day >= startDay
                && e.Day <= currentDay
                && this.IsShippingBinIncome(e)
            )
            .Sum(e => e.Amount);
    }

    public double GetIncomeTaxRate()
    {
        int taxableIncome = this.GetCurrentWeekTaxableShippingBinIncome();

        return this.GetIncomeTaxRateForAmount(taxableIncome);
    }

    public string GetIncomeTaxBracketLabel()
    {
        int taxableIncome = this.GetCurrentWeekTaxableShippingBinIncome();

        if (taxableIncome <= 5000)
            return "0 - 5,000g: 0%";

        if (taxableIncome <= 20000)
            return "5,001 - 20,000g: 5%";

        if (taxableIncome <= 50000)
            return "20,001 - 50,000g: 8%";

        if (taxableIncome <= 100000)
            return "50,001 - 100,000g: 12%";

        return "100,000g+: 15%";
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

    public string FormatTaxRatePercent(double rate)
    {
        int percent = (int)Math.Round(rate * 100);

        return $"{percent}%";
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
}