using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealityCheck.Data;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public class MarketTrendService
{
    private const string SaveDataKey = "market-trend-state";
    private const int MaxHistoryDays = 28;

    private const double SoftLowFinalFactor = 0.50;
    private const double SoftHighFinalFactor = 2.00;
    private const double HardLowFinalFactor = 0.30;
    private const double HardHighFinalFactor = 2.20;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private MarketTrendSaveData data = new();
    private string? loadedSaveId;

    public MarketTrendService(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public void Load()
    {
        if (!Context.IsWorldReady)
        {
            this.data = new MarketTrendSaveData();
            this.loadedSaveId = null;
            return;
        }

        this.data =
            this.helper.Data.ReadSaveData<MarketTrendSaveData>(SaveDataKey)
            ?? new MarketTrendSaveData();

        this.EnsureCollections();
        this.loadedSaveId = this.GetCurrentSaveId();
    }

    public void Save()
    {
        if (!Context.IsWorldReady)
            return;

        this.EnsureLoadedForCurrentSave();
        this.EnsureCollections();
        this.TrimAllHistory();

        this.helper.Data.WriteSaveData(
            SaveDataKey,
            this.data
        );
    }

    public MarketTrendSnapshot GetTrendSnapshot(string marketCommodityKey)
    {
        if (!Context.IsWorldReady || string.IsNullOrWhiteSpace(marketCommodityKey))
            return new MarketTrendSnapshot();

        this.EnsureLoadedForCurrentSave();

        MarketTrendItemState state = this.GetOrCreateState(marketCommodityKey);
        this.EnsureStateForToday(state);

        return new MarketTrendSnapshot
        {
            LongTermFactor = SanitizeFactor(state.LongTermFactor),
            TodayTrendChange = SanitizeFactor(state.TodayTrendChange),
            TrendMode = state.TrendMode,
            DaysRemaining = state.DaysRemaining
        };
    }

    public void RecordPrice(
        string marketCommodityKey,
        string itemId,
        string itemName,
        int baseUnitPrice,
        int marketUnitPrice,
        double dailyMultiplier,
        double totalMultiplier
    )
    {
        if (!Context.IsWorldReady || string.IsNullOrWhiteSpace(marketCommodityKey))
            return;

        if (baseUnitPrice <= 0 || marketUnitPrice < 0)
            return;

        this.EnsureLoadedForCurrentSave();

        MarketTrendItemState state = this.GetOrCreateState(marketCommodityKey);
        this.EnsureStateForToday(state);

        state.ItemId = itemId ?? string.Empty;
        state.ItemName = itemName ?? string.Empty;
        state.LastFinalFactor = SanitizeFactor(totalMultiplier);

        int dateIndex = GetCurrentDateIndex();

        if (!this.data.PriceHistory.TryGetValue(marketCommodityKey, out List<MarketPriceHistoryPoint>? history))
        {
            history = new List<MarketPriceHistoryPoint>();
            this.data.PriceHistory[marketCommodityKey] = history;
        }

        history.RemoveAll(p => p.DateIndex == dateIndex);
        history.Add(
            new MarketPriceHistoryPoint
            {
                DateIndex = dateIndex,
                Year = Game1.year,
                Season = Game1.currentSeason,
                Day = Game1.dayOfMonth,
                BaseUnitPrice = Math.Max(0, baseUnitPrice),
                MarketUnitPrice = Math.Max(0, marketUnitPrice),
                DailyMultiplier = SanitizeFactor(dailyMultiplier),
                TotalMultiplier = SanitizeFactor(totalMultiplier)
            }
        );

        TrimHistory(history);
    }

    public IReadOnlyList<MarketPriceHistoryPoint> GetHistory(string marketCommodityKey)
    {
        if (string.IsNullOrWhiteSpace(marketCommodityKey))
            return Array.Empty<MarketPriceHistoryPoint>();

        this.EnsureLoadedForCurrentSave();

        if (!this.data.PriceHistory.TryGetValue(marketCommodityKey, out List<MarketPriceHistoryPoint>? history))
            return Array.Empty<MarketPriceHistoryPoint>();

        return history
            .OrderBy(p => p.DateIndex)
            .ToList();
    }

    private MarketTrendItemState GetOrCreateState(string marketCommodityKey)
    {
        this.EnsureCollections();

        if (!this.data.TrendStates.TryGetValue(marketCommodityKey, out MarketTrendItemState? state))
        {
            state = new MarketTrendItemState
            {
                MarketCommodityKey = marketCommodityKey,
                LongTermFactor = 1.0,
                TodayTrendChange = 1.0,
                TrendMode = MarketTrendMode.Flat,
                DaysRemaining = 0,
                LastUpdatedDateKey = string.Empty,
                LastUpdatedDateIndex = 0,
                LastFinalFactor = 1.0
            };

            this.data.TrendStates[marketCommodityKey] = state;
        }

        if (string.IsNullOrWhiteSpace(state.MarketCommodityKey))
            state.MarketCommodityKey = marketCommodityKey;

        state.LongTermFactor = SanitizeFactor(state.LongTermFactor);
        state.TodayTrendChange = SanitizeFactor(state.TodayTrendChange);
        state.LastFinalFactor = SanitizeFactor(state.LastFinalFactor);

        return state;
    }

    private void EnsureStateForToday(MarketTrendItemState state)
    {
        int currentDateIndex = GetCurrentDateIndex();
        string currentDateKey = GetCurrentDateKey();

        if (state.LastUpdatedDateIndex <= 0)
        {
            if (string.Equals(state.LastUpdatedDateKey, currentDateKey, StringComparison.OrdinalIgnoreCase))
            {
                state.LastUpdatedDateIndex = currentDateIndex;
                return;
            }

            state.LastUpdatedDateIndex = Math.Max(0, currentDateIndex - 1);
        }

        if (state.LastUpdatedDateIndex >= currentDateIndex)
            return;

        while (state.LastUpdatedDateIndex < currentDateIndex)
        {
            int nextDateIndex = state.LastUpdatedDateIndex + 1;
            string dateKey = GetDateKeyFromIndex(nextDateIndex);

            this.UpdateStateForDate(
                state,
                dateKey
            );

            state.LastUpdatedDateIndex = nextDateIndex;
            state.LastUpdatedDateKey = dateKey;
        }
    }

    private void UpdateStateForDate(
        MarketTrendItemState state,
        string dateKey
    )
    {
        double referenceFinalFactor = SanitizeFactor(state.LastFinalFactor);

        if (state.DaysRemaining <= 0 || referenceFinalFactor <= HardLowFinalFactor || referenceFinalFactor >= HardHighFinalFactor)
        {
            state.TrendMode = this.ChooseNextMode(
                state.MarketCommodityKey,
                referenceFinalFactor,
                dateKey
            );

            state.DaysRemaining = this.GetDuration(
                state.TrendMode,
                state.MarketCommodityKey,
                dateKey
            );
        }

        double todayTrendChange = this.GetTodayTrendChange(
            state.TrendMode,
            state.MarketCommodityKey,
            dateKey
        );

        state.TodayTrendChange = todayTrendChange;
        state.LongTermFactor = SanitizeFactor(state.LongTermFactor * todayTrendChange);
        state.DaysRemaining = Math.Max(0, state.DaysRemaining - 1);
    }

    private MarketTrendMode ChooseNextMode(
        string marketCommodityKey,
        double referenceFinalFactor,
        string dateKey
    )
    {
        var random = new Random(this.GetSeed(marketCommodityKey, dateKey, "mode"));

        if (referenceFinalFactor <= HardLowFinalFactor)
            return this.PickWeighted(
                random,
                new Dictionary<MarketTrendMode, double>
                {
                    [MarketTrendMode.StrongUp] = 0.80,
                    [MarketTrendMode.Up] = 0.20
                }
            );

        if (referenceFinalFactor < SoftLowFinalFactor)
            return this.PickWeighted(
                random,
                new Dictionary<MarketTrendMode, double>
                {
                    [MarketTrendMode.StrongUp] = 0.30,
                    [MarketTrendMode.Up] = 0.50,
                    [MarketTrendMode.Flat] = 0.20
                }
            );

        if (referenceFinalFactor >= HardHighFinalFactor)
            return this.PickWeighted(
                random,
                new Dictionary<MarketTrendMode, double>
                {
                    [MarketTrendMode.StrongDown] = 0.80,
                    [MarketTrendMode.Down] = 0.20
                }
            );

        if (referenceFinalFactor > SoftHighFinalFactor)
            return this.PickWeighted(
                random,
                new Dictionary<MarketTrendMode, double>
                {
                    [MarketTrendMode.Flat] = 0.20,
                    [MarketTrendMode.Down] = 0.50,
                    [MarketTrendMode.StrongDown] = 0.30
                }
            );

        return this.PickWeighted(
            random,
            new Dictionary<MarketTrendMode, double>
            {
                [MarketTrendMode.StrongUp] = 0.10,
                [MarketTrendMode.Up] = 0.25,
                [MarketTrendMode.Flat] = 0.30,
                [MarketTrendMode.Down] = 0.25,
                [MarketTrendMode.StrongDown] = 0.10
            }
        );
    }

    private MarketTrendMode PickWeighted(
        Random random,
        Dictionary<MarketTrendMode, double> weights
    )
    {
        double total = weights.Values.Sum();
        double roll = random.NextDouble() * total;
        double cumulative = 0.0;

        foreach (var pair in weights)
        {
            cumulative += pair.Value;

            if (roll <= cumulative)
                return pair.Key;
        }

        return weights.Keys.Last();
    }

    private int GetDuration(
        MarketTrendMode mode,
        string marketCommodityKey,
        string dateKey
    )
    {
        var random = new Random(this.GetSeed(marketCommodityKey, dateKey, "duration"));

        return mode switch
        {
            MarketTrendMode.StrongUp => random.Next(3, 9),
            MarketTrendMode.Up => random.Next(5, 15),
            MarketTrendMode.Flat => random.Next(4, 11),
            MarketTrendMode.Down => random.Next(5, 15),
            MarketTrendMode.StrongDown => random.Next(3, 9),
            _ => random.Next(4, 11)
        };
    }

    private double GetTodayTrendChange(
        MarketTrendMode mode,
        string marketCommodityKey,
        string dateKey
    )
    {
        var random = new Random(this.GetSeed(marketCommodityKey, dateKey, $"change:{mode}"));

        double delta = mode switch
        {
            MarketTrendMode.StrongUp => NextDouble(random, 0.05, 0.09),
            MarketTrendMode.Up => NextDouble(random, 0.02, 0.05),
            MarketTrendMode.Flat => NextDouble(random, -0.015, 0.015),
            MarketTrendMode.Down => -NextDouble(random, 0.02, 0.06),
            MarketTrendMode.StrongDown => -NextDouble(random, 0.05, 0.09),
            _ => 0.0
        };

        return Math.Max(0.01, 1.0 + delta);
    }

    private static double NextDouble(Random random, double min, double max)
    {
        return min + random.NextDouble() * (max - min);
    }

    private int GetSeed(string marketCommodityKey, string dateKey, string purpose)
    {
        string seed = $"{Game1.uniqueIDForThisGame}|{dateKey}|{marketCommodityKey}|{purpose}";
        ulong hash = ComputeStableHash(seed);

        return unchecked((int)(hash & 0x7FFFFFFF));
    }

    private void EnsureLoadedForCurrentSave()
    {
        if (!Context.IsWorldReady)
            return;

        string currentSaveId = this.GetCurrentSaveId();

        if (this.loadedSaveId == currentSaveId)
            return;

        this.Load();
    }

    private string GetCurrentSaveId()
    {
        if (!Context.IsWorldReady || Game1.player is null)
            return string.Empty;

        return $"{Game1.uniqueIDForThisGame}:{Game1.player.UniqueMultiplayerID}";
    }

    private void EnsureCollections()
    {
        this.data.TrendStates ??= new Dictionary<string, MarketTrendItemState>();
        this.data.PriceHistory ??= new Dictionary<string, List<MarketPriceHistoryPoint>>();
    }

    private void TrimAllHistory()
    {
        foreach (List<MarketPriceHistoryPoint> history in this.data.PriceHistory.Values)
            TrimHistory(history);
    }

    private static void TrimHistory(List<MarketPriceHistoryPoint> history)
    {
        if (history.Count <= MaxHistoryDays)
            return;

        var trimmed = history
            .OrderByDescending(p => p.DateIndex)
            .Take(MaxHistoryDays)
            .OrderBy(p => p.DateIndex)
            .ToList();

        history.Clear();
        history.AddRange(trimmed);
    }

    private static string GetCurrentDateKey()
    {
        return $"{Game1.year}:{Game1.currentSeason}:{Game1.dayOfMonth}";
    }


    private static string GetDateKeyFromIndex(int dateIndex)
    {
        int safeDateIndex = Math.Max(1, dateIndex);
        int zeroBased = safeDateIndex - 1;
        int year = zeroBased / 112 + 1;
        int dayOfYear = zeroBased % 112;
        int seasonIndex = dayOfYear / 28;
        int day = dayOfYear % 28 + 1;
        string season = seasonIndex switch
        {
            0 => "spring",
            1 => "summer",
            2 => "fall",
            3 => "winter",
            _ => "spring"
        };

        return $"{year}:{season}:{day}";
    }

    private static int GetCurrentDateIndex()
    {
        int seasonIndex = Game1.currentSeason switch
        {
            "spring" => 0,
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };

        return Math.Max(0, Game1.year - 1) * 112
            + seasonIndex * 28
            + Math.Max(1, Game1.dayOfMonth);
    }

    private static double SanitizeFactor(double factor)
    {
        if (double.IsNaN(factor) || double.IsInfinity(factor) || factor <= 0)
            return 1.0;

        return Math.Clamp(factor, 0.01, 10.0);
    }

    private static ulong ComputeStableHash(string text)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        unchecked
        {
            ulong hash = offsetBasis;

            foreach (byte value in Encoding.UTF8.GetBytes(text))
            {
                hash ^= value;
                hash *= prime;
            }

            return hash;
        }
    }
}
