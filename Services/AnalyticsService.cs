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
public List<int> GetYearDailyIncome()
{
    var result = new List<int>();

    string[] seasons =
    {
        "spring",
        "summer",
        "fall",
        "winter"
    };

    foreach (string season in seasons)
    {
        for (int day = 1; day <= 28; day++)
        {
            int income = this.ledgerService.GetEntries()
                .Where(e =>
                    e.Type == "Income"
                    && e.Year == Game1.year
                    && e.Season == season
                    && e.Day == day
                )
                .Sum(e => e.Amount);

            result.Add(income);
        }
    }

    return result;
}

public List<DailyIncomeSummary> GetSeasonDailyIncomeDetailsToDate()
{
    var result = new List<DailyIncomeSummary>();

    for (int day = 1; day <= Game1.dayOfMonth; day++)
    {
        int income = this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Income"
                && e.Year == Game1.year
                && e.Season == Game1.currentSeason
                && e.Day == day
            )
            .Sum(e => e.Amount);

        if (income <= 0)
            continue;

        result.Add(new DailyIncomeSummary
        {
            Label = $"Year {Game1.year} {this.FormatSeason(Game1.currentSeason)} {day}",
            Amount = income
        });
    }

    return result;
}
public List<DailyIncomeSummary> GetYearDailyIncomeDetailsToDate()
{
    var result = new List<DailyIncomeSummary>();

    string[] seasons =
    {
        "spring",
        "summer",
        "fall",
        "winter"
    };

    int currentSeasonIndex = Array.IndexOf(seasons, Game1.currentSeason);

    if (currentSeasonIndex < 0)
        return result;

    for (int seasonIndex = 0; seasonIndex <= currentSeasonIndex; seasonIndex++)
    {
        string season = seasons[seasonIndex];

        int lastDay = seasonIndex == currentSeasonIndex
            ? Game1.dayOfMonth
            : 28;

        for (int day = 1; day <= lastDay; day++)
        {
            int income = this.ledgerService.GetEntries()
                .Where(e =>
                    e.Type == "Income"
                    && e.Year == Game1.year
                    && e.Season == season
                    && e.Day == day
                )
                .Sum(e => e.Amount);

            if (income <= 0)
                continue;

            result.Add(new DailyIncomeSummary
            {
                Label = $"Year {Game1.year} {this.FormatSeason(season)} {day}",
                Amount = income
            });
        }
    }

    return result;
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