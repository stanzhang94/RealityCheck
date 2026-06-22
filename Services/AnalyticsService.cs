using System;
using System.Collections.Generic;
using System.Linq;
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

    public int GetTodayExpense()
    {
        return this.GetExpenseTotal(
            Game1.year,
            Game1.currentSeason,
            Game1.dayOfMonth
        );
    }

    public int GetSeasonExpense()
    {
        return this.GetExpenseTotal(
            Game1.year,
            Game1.currentSeason,
            null
        );
    }

    public int GetYearExpense()
    {
        return this.GetExpenseTotal(
            Game1.year,
            null,
            null
        );
    }

    public int GetTodayNet()
    {
        return this.GetTodayIncome() - this.GetTodayExpense();
    }

    public int GetSeasonNet()
    {
        return this.GetSeasonIncome() - this.GetSeasonExpense();
    }

    public int GetYearNet()
    {
        return this.GetYearIncome() - this.GetYearExpense();
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
            .GroupBy(e => new
            {
                e.ItemId,
                e.ItemName
            })
            .Select(g => new ItemSummary
            {
                ItemId = g.Key.ItemId,
                ItemName = g.Key.ItemName,
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
            .GroupBy(e => new
            {
                e.ItemId,
                e.ItemName
            })
            .Select(g => new ItemSummary
            {
                ItemId = g.Key.ItemId,
                ItemName = g.Key.ItemName,
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
            .GroupBy(e => new
            {
                e.ItemId,
                e.ItemName
            })
            .Select(g => new ItemSummary
            {
                ItemId = g.Key.ItemId,
                ItemName = g.Key.ItemName,
                Quantity = g.Sum(e => e.Quantity),
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    public List<ExpenseSummary> GetTodayExpenseBreakdown()
    {
        return this.GetExpenseBreakdown(
            Game1.year,
            Game1.currentSeason,
            Game1.dayOfMonth
        );
    }

    public List<ExpenseSummary> GetSeasonExpenseBreakdown()
    {
        return this.GetExpenseBreakdown(
            Game1.year,
            Game1.currentSeason,
            null
        );
    }

    public List<ExpenseSummary> GetYearExpenseBreakdown()
    {
        return this.GetExpenseBreakdown(
            Game1.year,
            null,
            null
        );
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

    public List<int> GetSeasonDailyExpense()
    {
        var result = new List<int>();

        for (int day = 1; day <= 28; day++)
        {
            int expense = this.GetExpenseTotal(
                Game1.year,
                Game1.currentSeason,
                day
            );

            result.Add(expense);
        }

        return result;
    }

    public List<int> GetYearDailyExpense()
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
                int expense = this.GetExpenseTotal(
                    Game1.year,
                    season,
                    day
                );

                result.Add(expense);
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

    public List<DailyIncomeSummary> GetSeasonDailyExpenseDetailsToDate()
    {
        var result = new List<DailyIncomeSummary>();

        for (int day = 1; day <= Game1.dayOfMonth; day++)
        {
            int expense = this.GetExpenseTotal(
                Game1.year,
                Game1.currentSeason,
                day
            );

            if (expense <= 0)
                continue;

            result.Add(new DailyIncomeSummary
            {
                Label = $"Year {Game1.year} {this.FormatSeason(Game1.currentSeason)} {day}",
                Amount = expense
            });
        }

        return result;
    }

    public List<DailyIncomeSummary> GetYearDailyExpenseDetailsToDate()
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
                int expense = this.GetExpenseTotal(
                    Game1.year,
                    season,
                    day
                );

                if (expense <= 0)
                    continue;

                result.Add(new DailyIncomeSummary
                {
                    Label = $"Year {Game1.year} {this.FormatSeason(season)} {day}",
                    Amount = expense
                });
            }
        }

        return result;
    }

    private int GetExpenseTotal(
        int year,
        string? season,
        int? day
    )
    {
        int expenses = this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Expense"
                && e.Year == year
                && (season == null || e.Season == season)
                && (day == null || e.Day == day)
            )
            .Sum(e => e.Amount);

        int offsets = this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "ExpenseOffset"
                && e.Year == year
                && (season == null || e.Season == season)
                && (day == null || e.Day == day)
            )
            .Sum(e => e.Amount);

        return Math.Max(
            0,
            expenses - offsets
        );
    }

    private List<ExpenseSummary> GetExpenseBreakdown(
        int year,
        string? season,
        int? day
    )
    {
        var expenseRows = this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "Expense"
                && e.Year == year
                && (season == null || e.Season == season)
                && (day == null || e.Day == day)
            )
            .GroupBy(e =>
                string.IsNullOrWhiteSpace(e.ItemName)
                    ? "Base Game Expenses"
                    : e.ItemName
            )
            .Select(g => new ExpenseSummary
            {
                Category = g.Key,
                Amount = -g.Sum(e => e.Amount)
            });

        var offsetRows = this.ledgerService.GetEntries()
            .Where(e =>
                e.Type == "ExpenseOffset"
                && e.Year == year
                && (season == null || e.Season == season)
                && (day == null || e.Day == day)
            )
            .GroupBy(e =>
                string.IsNullOrWhiteSpace(e.ItemName)
                    ? "Expense Offset"
                    : e.ItemName
            )
            .Select(g => new ExpenseSummary
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount)
            });

        return expenseRows
            .Concat(offsetRows)
            .OrderBy(x => x.Amount)
            .ToList();
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