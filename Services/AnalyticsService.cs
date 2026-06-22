using RealityCheck.Models;
using StardewValley;

namespace RealityCheck.Services;

public class AnalyticsService
{
    private readonly LedgerService ledgerService;

    public AnalyticsService(LedgerService ledgerService)
    {
        this.ledgerService = ledgerService;
    }

    public int GetTodayIncome()
    {
        return this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
                && e.Season == Game1.currentSeason
                && e.Day == Game1.dayOfMonth
            )
            .Sum(e => e.Amount);
    }

    public int GetSeasonIncome()
    {
        return this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
                && e.Season == Game1.currentSeason
            )
            .Sum(e => e.Amount);
    }

    public int GetYearIncome()
    {
        return this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
            )
            .Sum(e => e.Amount);
    }

    public int GetTotalIncome()
    {
        return this.ledgerService.GetEntries()
            .Where(e => e.Type == "Income")
            .Sum(e => e.Amount);
    }
    public List<ItemSummary> GetTodayItemSummaries()
    {
        return this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
                && e.Season == Game1.currentSeason
                && e.Day == Game1.dayOfMonth
            )
            .GroupBy(e => e.ItemName)
            .Select(g => new ItemSummary
            {
                ItemName = g.Key,
                Quantity = g.Sum(e => e.Quantity),
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }
        public List<ItemSummary> GetSeasonItemSummaries()
    {
        return this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
                && e.Season == Game1.currentSeason
            )
            .GroupBy(e => e.ItemName)
            .Select(g => new ItemSummary
            {
                ItemName = g.Key,
                Quantity = g.Sum(e => e.Quantity),
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }
    public List<ItemSummary> GetYearItemSummaries()
{
    return this.ledgerService.GetEntries()
        .Where(e =>
            e.Type == "Income"
            && e.Year == Game1.year
        )
        .GroupBy(e => e.ItemName)
        .Select(g => new ItemSummary
        {
            ItemName = g.Key,
            Quantity = g.Sum(e => e.Quantity),
            Amount = g.Sum(e => e.Amount)
        })
        .OrderByDescending(x => x.Amount)
        .ToList();
}
public List<int> GetSeasonDailyIncome()
{
    var result = new List<int>();

    for (int day = 1; day <= 28; day++)
    {
        int income = this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
                && e.Season == Game1.currentSeason
                && e.Day == day
            )
            .Sum(e => e.Amount);

        result.Add(income);
    }

    return result;
}
}