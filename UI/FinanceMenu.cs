using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealityCheck.Models;
using RealityCheck.Services;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace RealityCheck.UI;

public class FinanceMenu : IClickableMenu, IKeyboardSubscriber
{
    private readonly LedgerService ledgerService;
    private readonly AnalyticsService analyticsService;
    private readonly TaxService taxService;
    private readonly MarketPriceService marketPriceService;
    private readonly ExchangeService? exchangeService;
    private readonly ExchangeContractCatalogService? exchangeContractCatalogService;

    private ReportTab currentTab = ReportTab.Daily;

    private int scrollOffset = 0;

    private int contentTop;
    private int contentBottom;
    private int contentStartY;
    private int currentContentHeight = 0;

    private List<MarketPriceTableEntry>? marketPriceEntries;

    private readonly Dictionary<string, string> marketPriceSearchTextCache = new(StringComparer.OrdinalIgnoreCase);

    private string marketPriceSearchText = string.Empty;

    private bool marketPriceSearchFocused;

    private Rectangle marketPriceSearchBounds;

    private MarketPriceTableEntry? selectedMarketPriceEntry;

    private Rectangle marketPriceBackBounds;

    private MarketPriceSortMode marketPriceSortMode = MarketPriceSortMode.MarketPrice;

    private bool marketPriceSortDescending = true;

    private Rectangle marketPriceItemHeaderBounds;

    private Rectangle marketPriceMarketHeaderBounds;

    private Rectangle marketPriceBaseHeaderBounds;

    private Rectangle marketPriceDailyMultiplierHeaderBounds;

    private Rectangle marketPriceTotalMultiplierHeaderBounds;

    private Rectangle previousScissorRectangle;

    private readonly Rectangle dailyTab;
    private readonly Rectangle seasonalTab;
    private readonly Rectangle annualTab;
    private readonly Rectangle taxTab;
    private readonly Rectangle marketTab;
    private readonly Rectangle exchangeButton;

    private readonly Color chartColor = new Color(92, 63, 34);

    private const int MarketPriceRowHeight = 42;

    private const int MarketPriceRowsStartOffset = 134;

    private enum ReportTab
    {
        Daily,
        Seasonal,
        Annual,
        Tax,
        Market
    }

    private enum MarketPriceSortMode
    {
        ItemName,
        MarketPrice,
        BasePrice,
        DailyMultiplier,
        TotalMultiplier
    }

    public bool Selected { get; set; }

    public FinanceMenu(
        LedgerService ledgerService,
        AnalyticsService analyticsService,
        MarketPriceService marketPriceService,
        ExchangeService? exchangeService = null,
        ExchangeContractCatalogService? exchangeContractCatalogService = null
    )
    {
        this.ledgerService = ledgerService;
        this.analyticsService = analyticsService;
        this.taxService = new TaxService(ledgerService);
        this.marketPriceService = marketPriceService;
        this.exchangeService = exchangeService;
        this.exchangeContractCatalogService = exchangeContractCatalogService;

        int x = Game1.uiViewport.Width / 2 - 400;
        int y = Game1.uiViewport.Height / 2 - 300;

        this.dailyTab = new Rectangle(x + 35, y + 25, 105, 55);
        this.seasonalTab = new Rectangle(x + 145, y + 25, 130, 55);
        this.annualTab = new Rectangle(x + 280, y + 25, 115, 55);
        this.taxTab = new Rectangle(x + 400, y + 25, 145, 55);
        this.marketTab = new Rectangle(x + 550, y + 25, 190, 55);
        this.exchangeButton = new Rectangle(x + 540, y + 104, 205, 34);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.TryHandleReportTabClick(x, y, playSound))
            return;

        if (this.TryHandleExchangeButtonClick(x, y, playSound))
            return;

        if (this.currentTab == ReportTab.Market)
        {
            if (this.selectedMarketPriceEntry is not null)
            {
                if (this.marketPriceBackBounds.Contains(x, y))
                {
                    this.selectedMarketPriceEntry = null;
                    this.scrollOffset = 0;

                    if (playSound)
                        Game1.playSound("smallSelect");
                }

                return;
            }

            if (this.TryHandleMarketPriceSearchClick(x, y))
            {
                if (playSound)
                    Game1.playSound("smallSelect");

                return;
            }

            if (this.TryHandleMarketPriceHeaderClick(x, y))
            {
                if (playSound)
                    Game1.playSound("smallSelect");

                return;
            }

            if (this.TryHandleMarketPriceRowClick(x, y))
            {
                if (playSound)
                    Game1.playSound("smallSelect");

                return;
            }
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (this.currentTab == ReportTab.Market && this.selectedMarketPriceEntry is null && this.marketPriceSearchFocused)
        {
            if (key == Keys.Back && this.marketPriceSearchText.Length > 0)
            {
                this.marketPriceSearchText = this.marketPriceSearchText[..^1];
                this.scrollOffset = 0;
                Game1.playSound("tinyWhip");
                return;
            }

            if (key == Keys.Delete && this.marketPriceSearchText.Length > 0)
            {
                this.marketPriceSearchText = string.Empty;
                this.scrollOffset = 0;
                Game1.playSound("tinyWhip");
                return;
            }

            if (IsPotentialMarketPriceSearchKey(key))
                return;
        }

        base.receiveKeyPress(key);
    }

    private static bool IsPotentialMarketPriceSearchKey(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z)
            return true;

        if (key >= Keys.D0 && key <= Keys.D9)
            return true;

        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            return true;

        return key is Keys.Space
            or Keys.OemPeriod
            or Keys.OemComma
            or Keys.OemMinus
            or Keys.OemPlus;
    }

    public void RecieveTextInput(char inputChar)
    {
        if (!this.CanEditMarketPriceSearchText() || char.IsControl(inputChar))
            return;

        this.marketPriceSearchText += inputChar;
        this.scrollOffset = 0;
    }

    public void RecieveTextInput(string text)
    {
        if (!this.CanEditMarketPriceSearchText() || string.IsNullOrEmpty(text))
            return;

        foreach (char c in text)
        {
            if (!char.IsControl(c))
                this.marketPriceSearchText += c;
        }

        this.scrollOffset = 0;
    }

    public void RecieveCommandInput(char command)
    {
        if (!this.CanEditMarketPriceSearchText())
            return;

        if (command == '\b' && this.marketPriceSearchText.Length > 0)
        {
            this.marketPriceSearchText = this.marketPriceSearchText[..^1];
            this.scrollOffset = 0;
            Game1.playSound("tinyWhip");
        }
    }

    public void RecieveSpecialInput(Keys key)
    {
        if (!this.CanEditMarketPriceSearchText())
            return;

        if (key == Keys.Back && this.marketPriceSearchText.Length > 0)
        {
            this.marketPriceSearchText = this.marketPriceSearchText[..^1];
            this.scrollOffset = 0;
            Game1.playSound("tinyWhip");
            return;
        }

        if (key == Keys.Delete && this.marketPriceSearchText.Length > 0)
        {
            this.marketPriceSearchText = string.Empty;
            this.scrollOffset = 0;
            Game1.playSound("tinyWhip");
            return;
        }

        if (key == Keys.Escape)
            this.SetMarketPriceSearchFocused(false);
    }

    private bool CanEditMarketPriceSearchText()
    {
        return this.currentTab == ReportTab.Market
            && this.selectedMarketPriceEntry is null
            && this.marketPriceSearchFocused;
    }

    private void SetMarketPriceSearchFocused(bool focused)
    {
        this.marketPriceSearchFocused = focused;
        this.Selected = focused;

        if (focused)
        {
            Game1.keyboardDispatcher.Subscriber = this;
            return;
        }

        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this))
            Game1.keyboardDispatcher.Subscriber = null;
    }

    protected override void cleanupBeforeExit()
    {
        this.SetMarketPriceSearchFocused(false);
        base.cleanupBeforeExit();
    }

    private bool TryHandleReportTabClick(int x, int y, bool playSound)
    {
        ReportTab? targetTab = null;

        if (this.dailyTab.Contains(x, y))
            targetTab = ReportTab.Daily;
        else if (this.seasonalTab.Contains(x, y))
            targetTab = ReportTab.Seasonal;
        else if (this.annualTab.Contains(x, y))
            targetTab = ReportTab.Annual;
        else if (this.taxTab.Contains(x, y))
            targetTab = ReportTab.Tax;
        else if (this.marketTab.Contains(x, y))
            targetTab = ReportTab.Market;

        if (targetTab is null)
            return false;

        this.currentTab = targetTab.Value;
        this.scrollOffset = 0;
        this.selectedMarketPriceEntry = null;
        this.SetMarketPriceSearchFocused(false);

        if (targetTab.Value == ReportTab.Market)
        {
            this.marketPriceEntries = null;
            this.marketPriceSearchTextCache.Clear();
        }

        if (playSound)
            Game1.playSound("smallSelect");

        return true;
    }


    private bool TryHandleExchangeButtonClick(int x, int y, bool playSound)
    {
        if (!this.exchangeButton.Contains(x, y))
            return false;

        if (this.exchangeService is null)
        {
            Game1.showRedMessage("Exchange is not available.");
            return true;
        }

        this.SetMarketPriceSearchFocused(false);
        Game1.activeClickableMenu = new ExchangeMenu(
            this.exchangeService,
            this.exchangeContractCatalogService
        );

        if (playSound)
            Game1.playSound("bigSelect");

        return true;
    }

    private void DrawExchangeButton(SpriteBatch b)
    {
        if (this.exchangeService is null)
            return;

        b.Draw(Game1.staminaRect, this.exchangeButton, Color.SaddleBrown * 0.12f);
        b.Draw(
            Game1.staminaRect,
            new Rectangle(this.exchangeButton.X, this.exchangeButton.Y, 5, this.exchangeButton.Height),
            Color.DarkGoldenrod * 0.82f
        );
        this.DrawRectangleOutline(b, this.exchangeButton, 1, Color.SaddleBrown * 0.46f);

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.button"),
            Game1.smallFont,
            new Vector2(this.exchangeButton.X + 16, this.exchangeButton.Y + 7),
            Game1.textColor
        );
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0)
            this.scrollOffset -= 80;
        else if (direction < 0)
            this.scrollOffset += 80;

        this.scrollOffset = Math.Clamp(
            this.scrollOffset,
            0,
            this.GetMaxScrollOffset()
        );
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);

        int x = Game1.uiViewport.Width / 2 - 400;
        int y = Game1.uiViewport.Height / 2 - 300;
        int width = 800;
        int height = 600;

        this.contentTop = y + 170;
        this.contentBottom = y + height - 45;
        this.contentStartY = y + 180;

        IClickableMenu.drawTextureBox(
            b,
            x,
            y,
            width,
            height,
            Color.White
        );

        this.DrawTabs(b);
        this.DrawExchangeButton(b);

        Utility.drawTextWithShadow(
            b,
            I18n.Get("mod.name"),
            Game1.dialogueFont,
            new Vector2(x + 60, y + 105),
            Game1.textColor
        );

        int contentX = x + 70;
        int contentY = this.contentStartY - this.scrollOffset;

        Rectangle clipArea = new Rectangle(
            x + 50,
            this.contentTop,
            width - 100,
            this.contentBottom - this.contentTop
        );

        this.BeginContentClip(b, clipArea);

        switch (this.currentTab)
        {
            case ReportTab.Daily:
                this.DrawDailyReport(b, contentX, contentY);
                break;

            case ReportTab.Seasonal:
                this.DrawSeasonalReport(b, contentX, contentY);
                break;

            case ReportTab.Annual:
                this.DrawAnnualReport(b, contentX, contentY);
                break;

            case ReportTab.Tax:
                this.DrawTaxReport(b, contentX, contentY);
                break;

            case ReportTab.Market:
                this.DrawMarketPriceReport(b, contentX, contentY);
                break;
        }

        this.EndContentClip(b);

        this.drawMouse(b);
    }

    private void BeginContentClip(SpriteBatch b, Rectangle clipArea)
    {
        this.previousScissorRectangle = b.GraphicsDevice.ScissorRectangle;

        b.End();

        b.GraphicsDevice.ScissorRectangle = clipArea;

        RasterizerState rasterizerState = new RasterizerState
        {
            ScissorTestEnable = true
        };

        b.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            rasterizerState
        );
    }

    private int DrawCurrentDebtSummary(SpriteBatch b, int x, int y)
{
    int unpaidBalance = this.analyticsService.GetOutstandingBalance();

    this.DrawLine(
        b,
        I18n.Get("finance.current_unpaid_balance", new { amount = this.FormatDebt(unpaidBalance) }),
        x,
        y
    );

    y += 55;

    return y;
}

    private void EndContentClip(SpriteBatch b)
    {
        b.End();

        b.GraphicsDevice.ScissorRectangle = this.previousScissorRectangle;

        b.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null
        );
    }

    private int GetMaxScrollOffset()
    {
        int visibleHeight = this.contentBottom - this.contentTop;

        return Math.Max(
            0,
            this.currentContentHeight - visibleHeight
        );
    }

    private void DrawTabs(SpriteBatch b)
    {
        this.DrawTab(b, this.dailyTab, I18n.Get("tab.daily"), this.currentTab == ReportTab.Daily);
        this.DrawTab(b, this.seasonalTab, I18n.Get("tab.seasonal"), this.currentTab == ReportTab.Seasonal);
        this.DrawTab(b, this.annualTab, I18n.Get("tab.annual"), this.currentTab == ReportTab.Annual);
        this.DrawTab(b, this.taxTab, I18n.Get("tab.tax_report"), this.currentTab == ReportTab.Tax);
        this.DrawTab(b, this.marketTab, I18n.Get("tab.market_price"), this.currentTab == ReportTab.Market);
    }

    private void DrawTab(SpriteBatch b, Rectangle rect, string label, bool active)
    {
        Color color = active ? Color.White : Color.LightGray;

        IClickableMenu.drawTextureBox(
            b,
            rect.X,
            rect.Y,
            rect.Width,
            rect.Height,
            color
        );

        Utility.drawTextWithShadow(
            b,
            label,
            Game1.smallFont,
            new Vector2(rect.X + 15, rect.Y + 16),
            Game1.textColor
        );
    }

    private void DrawDailyReport(SpriteBatch b, int x, int y)
    {
        this.DrawLine(b, I18n.Get("finance.date", new { date = I18n.Date(Game1.year, Game1.currentSeason, Game1.dayOfMonth) }), x, y);
        y += 45;

        y = this.DrawCurrentDebtSummary(b, x, y);

        this.DrawLine(b, I18n.Get("finance.today_income", new { amount = $"{this.analyticsService.GetTodayIncome()}g" }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("finance.today_expenses", new { amount = this.FormatExpense(this.analyticsService.GetTodayExpense()) }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("finance.today_net", new { amount = $"{this.analyticsService.GetTodayNet()}g" }), x, y);
        y += 55;

        y = this.DrawItemAndExpenseColumns(
            b,
            I18n.Get("finance.income_details_today"),
            this.analyticsService.GetTodayItemSummaries(),
            I18n.Get("finance.expense_breakdown"),
            this.analyticsService.GetTodayExpenseBreakdown(),
            x,
            y
        );

        this.UpdateContentHeight(y + 50);
    }

    private void DrawSeasonalReport(SpriteBatch b, int x, int y)
    {
this.DrawLine(b, I18n.Get("finance.season", new { year = Game1.year, season = I18n.Season(Game1.currentSeason) }), x, y);
y += 45;

y = this.DrawCurrentDebtSummary(b, x, y);

this.DrawLine(b, I18n.Get("finance.seasonal_income", new { amount = $"{this.analyticsService.GetSeasonIncome()}g" }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("finance.seasonal_expenses", new { amount = this.FormatExpense(this.analyticsService.GetSeasonExpense()) }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("finance.seasonal_net", new { amount = $"{this.analyticsService.GetSeasonNet()}g" }), x, y);
        y += 55;

        y = this.DrawItemAndExpenseColumns(
            b,
            I18n.Get("finance.income_details_this_season"),
            this.analyticsService.GetSeasonItemSummaries(),
            I18n.Get("finance.expense_breakdown"),
            this.analyticsService.GetSeasonExpenseBreakdown(),
            x,
            y
        );

        this.DrawLine(b, I18n.Get("finance.income_trend"), x, y);
        y += 75;

        var incomeTrend = this.analyticsService.GetSeasonDailyIncome();

        this.DrawSeasonTrendChart(
            b,
            incomeTrend,
            x,
            y
        );

        y += 330;

        this.DrawLine(b, I18n.Get("finance.expense_trend"), x, y);
        y += 75;

        var expenseTrend = this.analyticsService.GetSeasonDailyExpense();

        this.DrawSeasonTrendChart(
            b,
            expenseTrend,
            x,
            y
        );

        y += 330;

        y = this.DrawDailyIncomeAndExpenseDetails(
            b,
            this.analyticsService.GetSeasonDailyIncomeDetailsToDate(),
            this.analyticsService.GetSeasonDailyExpenseDetailsToDate(),
            x,
            y
        );

        this.UpdateContentHeight(y + 150);
    }

    private void DrawAnnualReport(SpriteBatch b, int x, int y)
    {
this.DrawLine(b, I18n.Get("finance.year", new { year = Game1.year }), x, y);
y += 45;

y = this.DrawCurrentDebtSummary(b, x, y);

this.DrawLine(b, I18n.Get("finance.annual_income", new { amount = $"{this.analyticsService.GetYearIncome()}g" }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("finance.annual_expenses", new { amount = this.FormatExpense(this.analyticsService.GetYearExpense()) }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("finance.annual_net", new { amount = $"{this.analyticsService.GetYearNet()}g" }), x, y);
        y += 55;

        y = this.DrawItemAndExpenseColumns(
            b,
            I18n.Get("finance.income_details_this_year"),
            this.analyticsService.GetYearItemSummaries(),
            I18n.Get("finance.expense_breakdown"),
            this.analyticsService.GetYearExpenseBreakdown(),
            x,
            y
        );

        this.DrawLine(b, I18n.Get("finance.income_trend"), x, y);
        y += 75;

        var incomeTrend = this.analyticsService.GetYearDailyIncome();

        this.DrawAnnualTrendChart(
            b,
            incomeTrend,
            x,
            y
        );

        y += 360;

        this.DrawLine(b, I18n.Get("finance.expense_trend"), x, y);
        y += 75;

        var expenseTrend = this.analyticsService.GetYearDailyExpense();

        this.DrawAnnualTrendChart(
            b,
            expenseTrend,
            x,
            y
        );

        y += 360;

        y = this.DrawDailyIncomeAndExpenseDetails(
            b,
            this.analyticsService.GetYearDailyIncomeDetailsToDate(),
            this.analyticsService.GetYearDailyExpenseDetailsToDate(),
            x,
            y
        );

        this.UpdateContentHeight(y + 150);
    }

    private bool TryHandleMarketPriceRowClick(int x, int y)
    {
        if (y < this.contentTop || y > this.contentBottom)
            return false;

        int menuX = Game1.uiViewport.Width / 2 - 400;
        int menuY = Game1.uiViewport.Height / 2 - 300;
        int contentX = menuX + 70;
        int firstRowY = menuY + 180 - this.scrollOffset + MarketPriceRowsStartOffset;

        int relativeY = y - firstRowY;

        if (relativeY < 0)
            return false;

        int rowIndex = relativeY / MarketPriceRowHeight;
        int rowOffset = relativeY % MarketPriceRowHeight;

        if (rowOffset > 34)
            return false;

        List<MarketPriceTableEntry> entries = this.GetVisibleMarketPriceEntries();

        if (rowIndex < 0 || rowIndex >= entries.Count)
            return false;

        MarketPriceTableEntry entry = entries[rowIndex];
        int rowY = firstRowY + rowIndex * MarketPriceRowHeight;

        if (this.GetMarketPriceFavoriteBounds(contentX, rowY).Contains(x, y))
        {
            this.SetMarketPriceSearchFocused(false);
            this.ledgerService.ToggleFavoriteMarketCommodity(entry.MarketCommodityKey);
            this.scrollOffset = Math.Clamp(
                this.scrollOffset,
                0,
                this.GetMaxScrollOffset()
            );
            return true;
        }

        int rowLeft = contentX + 24;
        int rowRight = contentX + 690;

        if (x < rowLeft || x > rowRight)
            return false;

        this.SetMarketPriceSearchFocused(false);
        this.selectedMarketPriceEntry = entry;
        this.scrollOffset = 0;

        return true;
    }

    private bool TryHandleMarketPriceSearchClick(int x, int y)
    {
        if (!this.marketPriceSearchBounds.Contains(x, y))
            return false;

        this.SetMarketPriceSearchFocused(true);
        return true;
    }

    private bool TryHandleMarketPriceHeaderClick(int x, int y)
    {
        if (this.marketPriceItemHeaderBounds.Contains(x, y))
        {
            this.SetMarketPriceSearchFocused(false);
            return this.SetMarketPriceSort(MarketPriceSortMode.ItemName);
        }

        if (this.marketPriceMarketHeaderBounds.Contains(x, y))
        {
            this.SetMarketPriceSearchFocused(false);
            return this.SetMarketPriceSort(MarketPriceSortMode.MarketPrice);
        }

        if (this.marketPriceBaseHeaderBounds.Contains(x, y))
        {
            this.SetMarketPriceSearchFocused(false);
            return this.SetMarketPriceSort(MarketPriceSortMode.BasePrice);
        }

        if (this.marketPriceDailyMultiplierHeaderBounds.Contains(x, y))
        {
            this.SetMarketPriceSearchFocused(false);
            return this.SetMarketPriceSort(MarketPriceSortMode.DailyMultiplier);
        }

        if (this.marketPriceTotalMultiplierHeaderBounds.Contains(x, y))
        {
            this.SetMarketPriceSearchFocused(false);
            return this.SetMarketPriceSort(MarketPriceSortMode.TotalMultiplier);
        }

        return false;
    }

    private bool SetMarketPriceSort(MarketPriceSortMode mode)
    {
        if (this.marketPriceSortMode == mode)
        {
            this.marketPriceSortDescending = !this.marketPriceSortDescending;
        }
        else
        {
            this.marketPriceSortMode = mode;
            this.marketPriceSortDescending = mode != MarketPriceSortMode.ItemName;
        }

        this.scrollOffset = 0;

        return true;
    }

    private List<MarketPriceTableEntry> GetVisibleMarketPriceEntries()
    {
        List<MarketPriceTableEntry> sortedEntries = this.GetSortedMarketPriceEntries();
        string normalizedSearchText = NormalizeMarketPriceSearchText(this.marketPriceSearchText);

        if (!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            return sortedEntries
                .Select(entry => new
                {
                    Entry = entry,
                    Rank = this.GetMarketPriceSearchRank(entry, normalizedSearchText)
                })
                .Where(match => match.Rank >= 0)
                .OrderBy(match => match.Rank)
                .Select(match => match.Entry)
                .ToList();
        }

        HashSet<string> favoriteKeys = new(
            this.ledgerService.GetFavoriteMarketCommodityKeys(),
            StringComparer.OrdinalIgnoreCase
        );

        return sortedEntries
            .OrderByDescending(entry => favoriteKeys.Contains(entry.MarketCommodityKey))
            .ToList();
    }

    private List<MarketPriceTableEntry> GetSortedMarketPriceEntries()
    {
        this.marketPriceEntries ??= this.marketPriceService.GetSellableObjectMarketPriceTable(
            this.ledgerService.GetEntries()
        );

        IEnumerable<MarketPriceTableEntry> sorted = this.marketPriceSortMode switch
        {
            MarketPriceSortMode.ItemName => this.marketPriceEntries.OrderBy(e => e.ItemName, StringComparer.CurrentCulture),
            MarketPriceSortMode.BasePrice => this.marketPriceEntries.OrderBy(e => e.BaseUnitPrice).ThenBy(e => e.ItemName, StringComparer.CurrentCulture),
            MarketPriceSortMode.DailyMultiplier => this.marketPriceEntries.OrderBy(e => e.DailyMultiplier).ThenBy(e => e.ItemName, StringComparer.CurrentCulture),
            MarketPriceSortMode.TotalMultiplier => this.marketPriceEntries.OrderBy(e => e.TotalMultiplier).ThenBy(e => e.ItemName, StringComparer.CurrentCulture),
            _ => this.marketPriceEntries.OrderBy(e => e.MarketUnitPrice).ThenBy(e => e.ItemName, StringComparer.CurrentCulture)
        };

        if (this.marketPriceSortDescending)
            sorted = this.marketPriceSortMode switch
            {
                MarketPriceSortMode.ItemName => this.marketPriceEntries.OrderByDescending(e => e.ItemName, StringComparer.CurrentCulture),
                MarketPriceSortMode.BasePrice => this.marketPriceEntries.OrderByDescending(e => e.BaseUnitPrice).ThenBy(e => e.ItemName, StringComparer.CurrentCulture),
                MarketPriceSortMode.DailyMultiplier => this.marketPriceEntries.OrderByDescending(e => e.DailyMultiplier).ThenBy(e => e.ItemName, StringComparer.CurrentCulture),
                MarketPriceSortMode.TotalMultiplier => this.marketPriceEntries.OrderByDescending(e => e.TotalMultiplier).ThenBy(e => e.ItemName, StringComparer.CurrentCulture),
                _ => this.marketPriceEntries.OrderByDescending(e => e.MarketUnitPrice).ThenBy(e => e.ItemName, StringComparer.CurrentCulture)
            };

        return sorted.ToList();
    }

    private int GetMarketPriceSearchRank(MarketPriceTableEntry entry, string normalizedSearchText)
    {
        string itemName = NormalizeMarketPriceSearchText(entry.ItemName);
        string haystack = this.GetMarketPriceSearchText(entry);

        if (itemName.StartsWith(normalizedSearchText, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (itemName.Contains(normalizedSearchText, StringComparison.OrdinalIgnoreCase))
            return 1;

        if (haystack.Contains(normalizedSearchText, StringComparison.OrdinalIgnoreCase))
            return 2;

        if (IsFuzzyMarketPriceMatch(itemName, normalizedSearchText))
            return 3;

        if (IsFuzzyMarketPriceMatch(haystack, normalizedSearchText))
            return 4;

        return -1;
    }

    private string GetMarketPriceSearchText(MarketPriceTableEntry entry)
    {
        string cacheKey = string.IsNullOrWhiteSpace(entry.MarketCommodityKey)
            ? entry.ItemId
            : entry.MarketCommodityKey;

        if (this.marketPriceSearchTextCache.TryGetValue(cacheKey, out string? cachedText))
            return cachedText;

        List<string> searchParts = new()
        {
            entry.ItemName,
            entry.ItemId,
            entry.MarketCommodityKey,
            entry.ParentItemId
        };

        this.AddItemSearchAliases(searchParts, entry.IconItem);
        this.AddItemSearchAliases(searchParts, entry.ParentItemId);

        string searchText = NormalizeMarketPriceSearchText(string.Join(" ", searchParts));
        this.marketPriceSearchTextCache[cacheKey] = searchText;

        return searchText;
    }

    private void AddItemSearchAliases(List<string> searchParts, Item? item)
    {
        if (item is null)
            return;

        searchParts.Add(item.Name);
        searchParts.Add(item.DisplayName);
        searchParts.Add(item.QualifiedItemId);
    }

    private void AddItemSearchAliases(List<string> searchParts, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        try
        {
            this.AddItemSearchAliases(
                searchParts,
                ItemRegistry.Create(itemId)
            );
        }
        catch
        {
        }
    }

    private static string NormalizeMarketPriceSearchText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        StringBuilder builder = new();

        foreach (char c in text.Normalize(NormalizationForm.FormKC).ToLowerInvariant())
        {
            if (char.IsWhiteSpace(c))
                continue;

            builder.Append(c);
        }

        return builder.ToString();
    }

    private static bool IsFuzzyMarketPriceMatch(string haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(needle))
            return true;

        if (string.IsNullOrWhiteSpace(haystack))
            return false;

        int needleIndex = 0;

        foreach (char c in haystack)
        {
            if (c != needle[needleIndex])
                continue;

            needleIndex++;

            if (needleIndex >= needle.Length)
                return true;
        }

        return false;
    }

    private string GetMarketPriceSortLabel(MarketPriceSortMode mode)
    {
        if (this.marketPriceSortMode != mode)
            return string.Empty;

        if (mode == MarketPriceSortMode.ItemName)
            return this.marketPriceSortDescending ? " Z-A" : " A-Z";

        return this.marketPriceSortDescending ? " 高-低" : " 低-高";
    }

    private void DrawMarketPriceReport(SpriteBatch b, int x, int y)
    {
        if (this.selectedMarketPriceEntry is not null)
        {
            this.DrawMarketPriceHistoryReport(b, this.selectedMarketPriceEntry, x, y);
            return;
        }

        this.DrawLine(b, I18n.Get("market_price.title"), x, y);
        y += 42;

        this.DrawMarketPriceSearchBox(b, x, y);
        y += 52;

        this.DrawMarketPriceHeader(b, x, y);
        y += 40;

        List<MarketPriceTableEntry> entries = this.GetVisibleMarketPriceEntries();

        if (entries.Count == 0)
        {
            this.DrawLine(
                b,
                string.IsNullOrWhiteSpace(this.marketPriceSearchText)
                    ? I18n.Get("market_price.no_items")
                    : "No matching market items / 没有匹配的市场物品",
                x,
                y
            );
            y += 35;
            this.UpdateContentHeight(y + 80);
            return;
        }

        foreach (MarketPriceTableEntry entry in entries)
        {
            this.DrawMarketPriceLine(b, entry, x, y);
            y += 42;
        }

        this.UpdateContentHeight(y + 80);
    }

    private void DrawMarketPriceHistoryReport(SpriteBatch b, MarketPriceTableEntry entry, int x, int y)
    {
        this.marketPriceBackBounds = new Rectangle(x, y - 8, 125, 42);

        IClickableMenu.drawTextureBox(
            b,
            this.marketPriceBackBounds.X,
            this.marketPriceBackBounds.Y,
            this.marketPriceBackBounds.Width,
            this.marketPriceBackBounds.Height,
            Color.White
        );

        Utility.drawTextWithShadow(
            b,
            I18n.Get("market_price.history_back"),
            Game1.smallFont,
            new Vector2(x + 34, y + 2),
            Game1.textColor
        );

        y += 50;

        this.DrawLine(
            b,
            I18n.Get("market_price.history_title", new { item = entry.ItemName }),
            x,
            y
        );

        y += 55;

        IReadOnlyList<MarketPriceHistoryPoint> history = this.marketPriceService.GetMarketPriceHistory(entry.MarketCommodityKey);

        int currentPrice = Math.Max(0, (int)Math.Round(entry.MarketUnitPrice, MidpointRounding.AwayFromZero));
        int highPrice = history.Count > 0 ? history.Max(p => p.MarketUnitPrice) : currentPrice;
        int lowPrice = history.Count > 0 ? history.Min(p => p.MarketUnitPrice) : currentPrice;

        this.DrawLine(
            b,
            I18n.Get(
                "market_price.history_summary",
                new
                {
                    current = $"{currentPrice}g",
                    high = $"{highPrice}g",
                    low = $"{lowPrice}g"
                }
            ),
            x,
            y
        );

        y += 55;

        if (history.Count == 0)
        {
            this.DrawLine(b, I18n.Get("market_price.history_no_data"), x, y);
            y += 45;
            this.UpdateContentHeight(y + 80);
            return;
        }

        this.DrawMarketPriceHistoryChart(b, history.ToList(), x, y);
        y += 315;

        MarketPriceHistoryPoint first = history.First();
        MarketPriceHistoryPoint last = history.Last();

        this.DrawLine(
            b,
            I18n.Get(
                "market_price.history_range",
                new
                {
                    start = this.FormatHistoryDate(first),
                    end = this.FormatHistoryDate(last)
                }
            ),
            x,
            y
        );

        y += 45;

        this.DrawLine(b, I18n.Get("market_price.history_note"), x, y);
        y += 45;

        this.UpdateContentHeight(y + 80);
    }

    private void DrawMarketPriceHistoryChart(SpriteBatch b, List<MarketPriceHistoryPoint> history, int x, int y)
    {
        int labelWidth = 70;
        int chartWidth = 560;
        int chartHeight = 230;

        int left = x + labelWidth;
        int top = y;
        int bottom = y + chartHeight;
        int right = left + chartWidth;

        int rawMinValue = Math.Max(0, history.Min(p => p.MarketUnitPrice));
        int rawMaxValue = Math.Max(1, history.Max(p => p.MarketUnitPrice));

        int minValue = this.RoundChartMinimum(rawMinValue);
        int maxValue = this.RoundChartMaximum(rawMaxValue);

        if (maxValue <= minValue)
            maxValue = minValue + 10;

        this.DrawMarketPriceChartGrid(
            b,
            left,
            right,
            top,
            bottom,
            minValue,
            maxValue
        );

        this.DrawLineSegment(
            b,
            new Vector2(left, bottom),
            new Vector2(right, bottom),
            2
        );

        this.DrawLineSegment(
            b,
            new Vector2(left, top),
            new Vector2(left, bottom),
            2
        );

        if (history.Count >= 2)
        {
            float xStep = chartWidth / (float)(history.Count - 1);
            Vector2? previousPoint = null;

            for (int i = 0; i < history.Count; i++)
            {
                float pointX = left + i * xStep;
                float normalized = (history[i].MarketUnitPrice - minValue) / (float)(maxValue - minValue);
                float pointY = bottom - normalized * chartHeight;
                Vector2 currentPoint = new Vector2(pointX, pointY);

                if (previousPoint.HasValue)
                {
                    this.DrawLineSegment(
                        b,
                        previousPoint.Value,
                        currentPoint,
                        3
                    );
                }

                previousPoint = currentPoint;

                b.Draw(
                    Game1.staminaRect,
                    new Rectangle(
                        (int)pointX - 3,
                        (int)pointY - 3,
                        6,
                        6
                    ),
                    this.chartColor
                );
            }
        }
        else
        {
            float pointX = left + chartWidth / 2f;
            float pointY = top + chartHeight / 2f;

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)pointX - 4,
                    (int)pointY - 4,
                    8,
                    8
                ),
                this.chartColor
            );
        }
    }

    private void DrawMarketPriceChartGrid(
        SpriteBatch b,
        int left,
        int right,
        int top,
        int bottom,
        int minValue,
        int maxValue
    )
    {
        const int tickCount = 5;

        for (int i = 0; i < tickCount; i++)
        {
            float t = i / (float)(tickCount - 1);
            int value = (int)Math.Round(maxValue - (maxValue - minValue) * t);
            float y = top + (bottom - top) * t;

            Color gridColor = new Color(160, 135, 100) * 0.35f;

            this.DrawLineSegment(
                b,
                new Vector2(left, y),
                new Vector2(right, y),
                1,
                gridColor
            );

            Utility.drawTextWithShadow(
                b,
                $"{value}g",
                Game1.smallFont,
                new Vector2(left - 64, y - 12),
                Game1.textColor
            );
        }
    }

    private int RoundChartMinimum(int value)
    {
        if (value <= 0)
            return 0;

        int step = this.GetChartStep(value);
        return Math.Max(0, value / step * step);
    }

    private int RoundChartMaximum(int value)
    {
        int step = this.GetChartStep(value);
        return Math.Max(step, ((value + step - 1) / step) * step);
    }

    private int GetChartStep(int value)
    {
        if (value < 100)
            return 10;

        if (value < 500)
            return 25;

        if (value < 1000)
            return 50;

        if (value < 5000)
            return 100;

        return 500;
    }

    private string FormatHistoryDate(MarketPriceHistoryPoint point)
    {
        string seasonKey = point.Season switch
        {
            "spring" => "season.spring",
            "summer" => "season.summer",
            "fall" => "season.fall",
            "winter" => "season.winter",
            _ => "season.spring"
        };

        return $"{I18n.Get(seasonKey)} {point.Day}";
    }

    private void DrawMarketPriceSearchBox(SpriteBatch b, int x, int y)
    {
        this.marketPriceSearchBounds = new Rectangle(x, y - 5, 635, 38);

        Color backgroundColor = string.IsNullOrWhiteSpace(this.marketPriceSearchText)
            ? Color.White * 0.22f
            : Color.White * 0.36f;

        b.Draw(Game1.staminaRect, this.marketPriceSearchBounds, backgroundColor);
        this.DrawRectangleOutline(
            b,
            this.marketPriceSearchBounds,
            2,
            this.marketPriceSearchFocused ? Color.Goldenrod * 0.95f : Color.Black * 0.55f
        );

        string text = string.IsNullOrWhiteSpace(this.marketPriceSearchText)
            ? "Search / 搜索"
            : this.marketPriceSearchText;

        Color textColor = string.IsNullOrWhiteSpace(this.marketPriceSearchText)
            ? Game1.textColor * 0.65f
            : Game1.textColor;

        this.DrawColoredText(
            b,
            text,
            this.marketPriceSearchBounds.X + 14,
            this.marketPriceSearchBounds.Y + 8,
            textColor,
            this.GetBodyTextScale()
        );
    }

    private void DrawMarketPriceHeader(SpriteBatch b, int x, int y)
    {
        this.marketPriceItemHeaderBounds = new Rectangle(x + 78, y - 5, 180, 35);
        this.marketPriceMarketHeaderBounds = new Rectangle(x + 285, y - 5, 105, 35);
        this.marketPriceBaseHeaderBounds = new Rectangle(x + 395, y - 5, 80, 35);
        this.marketPriceDailyMultiplierHeaderBounds = new Rectangle(x + 480, y - 5, 115, 35);
        this.marketPriceTotalMultiplierHeaderBounds = new Rectangle(x + 600, y - 5, 105, 35);

        this.DrawLine(
            b,
            I18n.Get("market_price.header_item") + this.GetMarketPriceSortLabel(MarketPriceSortMode.ItemName),
            x + 78,
            y
        );

        this.DrawLine(
            b,
            I18n.Get("market_price.header_market_price") + this.GetMarketPriceSortLabel(MarketPriceSortMode.MarketPrice),
            x + 285,
            y
        );

        this.DrawLine(
            b,
            I18n.Get("market_price.header_base_price") + this.GetMarketPriceSortLabel(MarketPriceSortMode.BasePrice),
            x + 395,
            y
        );

        this.DrawLine(
            b,
            I18n.Get("market_price.header_daily_multiplier") + this.GetMarketPriceSortLabel(MarketPriceSortMode.DailyMultiplier),
            x + 480,
            y
        );

        this.DrawLine(
            b,
            I18n.Get("market_price.header_total_multiplier") + this.GetMarketPriceSortLabel(MarketPriceSortMode.TotalMultiplier),
            x + 600,
            y
        );
    }

    private void DrawMarketPriceLine(SpriteBatch b, MarketPriceTableEntry entry, int x, int y)
    {
        this.DrawMarketPriceRowTextBackground(b, x, x + 690, y);

        Rectangle favoriteBounds = this.GetMarketPriceFavoriteBounds(x, y);
        bool isFavorite = this.ledgerService.IsFavoriteMarketCommodity(entry.MarketCommodityKey);

        this.DrawMarketPriceFavoriteMarker(b, favoriteBounds, isFavorite);

        int itemNameX = x + 28;
        int iconX = x + 24;

        if (entry.IconItem is not null)
        {
            try
            {
                entry.IconItem.drawInMenu(
                    b,
                    new Vector2(iconX, y - 12),
                    0.65f,
                    1f,
                    0.9f,
                    StackDrawType.Hide,
                    Color.White,
                    false
                );

                itemNameX = x + 78;
            }
            catch
            {
                itemNameX = x + 28;
            }
        }

        float scale = this.GetBodyTextScale();

        this.DrawColoredText(b, entry.ItemName, itemNameX, y, Game1.textColor, scale);
        this.DrawColoredText(
            b,
            this.FormatMarketUnitPrice(entry.MarketUnitPrice),
            x + 285,
            y,
            Game1.textColor,
            scale
        );
        this.DrawColoredText(b, $"{entry.BaseUnitPrice}g", x + 395, y, Game1.textColor, scale);
        this.DrawColoredText(
            b,
            this.FormatMultiplierPercent(entry.DailyMultiplier),
            x + 480,
            y,
            this.GetMarketMultiplierColor(entry.DailyMultiplier),
            scale
        );
        this.DrawColoredText(
            b,
            this.FormatMultiplierPercent(entry.TotalMultiplier),
            x + 600,
            y,
            this.GetMarketMultiplierColor(entry.TotalMultiplier),
            scale
        );
    }

    private Rectangle GetMarketPriceFavoriteBounds(int x, int y)
    {
        return new Rectangle(x, y + 5, 15, 15);
    }

    private void DrawMarketPriceFavoriteMarker(SpriteBatch b, Rectangle bounds, bool isFavorite)
    {
        if (isFavorite)
        {
            b.Draw(Game1.staminaRect, bounds, Color.Goldenrod * 0.95f);
            this.DrawRectangleOutline(b, bounds, 1, Color.Black * 0.55f);
            return;
        }

        this.DrawRectangleOutline(b, bounds, 2, Game1.textColor * 0.65f);
    }

    private void DrawRectangleOutline(SpriteBatch b, Rectangle bounds, int thickness, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || thickness <= 0)
            return;

        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }


    private void DrawMarketPriceRowTextBackground(SpriteBatch b, int left, int right, int y)
    {
        Rectangle bounds = new Rectangle(
            left - 8,
            y - 5,
            Math.Max(0, right - left + 8),
            36
        );

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        Point mousePoint = new Point(Game1.getMouseX(), Game1.getMouseY());
        Color backgroundColor = bounds.Contains(mousePoint)
            ? Color.White * 0.32f
            : Color.White * 0.16f;

        b.Draw(Game1.staminaRect, bounds, backgroundColor);
    }

    private string FormatMarketUnitPrice(double price)
    {
        if (double.IsNaN(price) || double.IsInfinity(price) || price < 0)
            return "0g";

        int roundedPrice = Math.Max(
            0,
            (int)Math.Round(
                price,
                MidpointRounding.AwayFromZero
            )
        );

        return $"{roundedPrice}g";
    }

    private Color GetMarketMultiplierColor(double multiplier)
    {
        const double epsilon = 0.0001;

        if (multiplier > 1.0 + epsilon)
            return Color.ForestGreen;

        if (multiplier < 1.0 - epsilon)
            return Color.DarkRed;

        return Game1.textColor;
    }

    private void DrawColoredText(SpriteBatch b, string text, int x, int y, Color color, float scale)
    {
        b.DrawString(
            Game1.smallFont,
            text,
            new Vector2(x, y),
            color,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            1f
        );
    }


    private string FormatMultiplierPercent(double multiplier)
    {
        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
            return "0.0%";

        double percent = (multiplier - 1.0) * 100.0;

        if (Math.Abs(percent) < 0.05)
            percent = 0.0;

        string sign = percent > 0 ? "+" : string.Empty;
        return $"{sign}{percent:0.0}%";
    }

    private string FormatMultiplier(double multiplier)
    {
        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0)
            return "1.000";

        return multiplier.ToString("0.000");
    }

    private void DrawTaxReport(SpriteBatch b, int x, int y)
    {
        this.DrawLine(b, I18n.Get("tab.tax_report"), x, y);
        y += 50;

        this.DrawLine(b, I18n.Get("tax_report.current_tax_week", new { period = this.taxService.GetCurrentTaxWeekLabel() }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("tax_report.next_tax_settlement", new { date = this.taxService.GetNextTaxSettlementLabel() }), x, y);
        y += 55;

        this.DrawLine(b, I18n.Get("tax.income_tax"), x, y);
        y += 45;

        this.DrawLine(b, I18n.Get("tax_report.taxable_shipping_bin_income", new { amount = $"{this.taxService.GetCurrentWeekTaxableShippingBinIncome()}g" }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("tax_report.income_tax_bracket", new { bracket = this.taxService.GetIncomeTaxBracketLabel() }), x, y);
        y += 35;

        this.DrawLine(b, I18n.Get("tax_report.estimated_income_tax", new { amount = this.FormatExpense(this.taxService.GetEstimatedIncomeTax()) }), x, y);
        y += 60;

        this.DrawLine(b, I18n.Get("tax.property_tax"), x, y);
        y += 45;

        this.DrawLine(b, I18n.Get("tax_report.estimated_property_tax", new { amount = this.FormatExpense(this.taxService.GetEstimatedPropertyTax()) }), x, y);
        y += 60;

        this.DrawLine(b, I18n.Get("tax.business_property_tax"), x, y);
        y += 45;

        this.DrawLine(b, I18n.Get("tax_report.estimated_business_property_tax", new { amount = this.FormatExpense(this.taxService.GetEstimatedBusinessPropertyTax()) }), x, y);
        y += 60;

        this.DrawLine(b, I18n.Get("tax_report.estimated_total_tax_due", new { amount = this.FormatExpense(this.taxService.GetEstimatedTotalTaxDue()) }), x, y);
        y += 70;

        this.DrawLine(b, I18n.Get("tax_report.tax_history"), x, y);
        y += 45;

        var taxRecords = this.taxService.GetRecentTaxRecords(16);

        if (taxRecords.Count == 0)
        {
            this.DrawLine(b, I18n.Get("tax_report.no_tax_records"), x, y);
            y += 35;
        }
        else
        {
            foreach (var record in taxRecords)
            {
                this.DrawLine(
                    b,
                    this.taxService.GetTaxRecordSummaryLine(record),
                    x,
                    y
                );

                y += 35;
            }
        }

        y += 40;

        this.DrawLine(b, I18n.Get("tax_report.direct_sales_note"), x, y);
        y += 40;

        this.UpdateContentHeight(y + 80);
    }

    private int DrawItemAndExpenseColumns(
        SpriteBatch b,
        string itemTitle,
        List<ItemSummary> items,
        string expenseTitle,
        List<ExpenseSummary> expenses,
        int x,
        int y
    )
    {
        int leftX = x;
        int rightX = x + 360;

        this.DrawLine(b, itemTitle, leftX, y);
        this.DrawLine(b, expenseTitle, rightX, y);

        y += 45;

        int leftY = y;
        int rightY = y;

        if (items.Count == 0)
        {
            this.DrawLine(b, I18n.Get("finance.no_income_recorded"), leftX, leftY);
            leftY += 35;
        }
        else
        {
            foreach (var item in items)
            {
                this.DrawItemSummaryLine(
                    b,
                    item,
                    leftX,
                    leftY
                );

                leftY += 38;
            }
        }

        if (expenses.Count == 0)
        {
            this.DrawLine(b, I18n.Get("finance.no_expenses_recorded"), rightX, rightY);
            rightY += 35;
        }
        else
        {
            foreach (var expense in expenses)
            {
                this.DrawExpenseSummaryLine(
                    b,
                    expense,
                    rightX,
                    rightY
                );

                rightY += 38;
            }
        }

        return Math.Max(leftY, rightY) + 55;
    }

    private int DrawDailyIncomeAndExpenseDetails(
        SpriteBatch b,
        List<DailyIncomeSummary> incomeDetails,
        List<DailyIncomeSummary> expenseDetails,
        int x,
        int y
    )
    {
        int leftX = x;
        int rightX = x + 360;

        this.DrawLine(b, I18n.Get("finance.daily_income_details"), leftX, y);
        this.DrawLine(b, I18n.Get("finance.daily_expense_details"), rightX, y);

        y += 45;

        int leftY = y;
        int rightY = y;

        if (incomeDetails.Count == 0)
        {
            this.DrawLine(b, I18n.Get("finance.no_income_days_recorded"), leftX, leftY);
            leftY += 35;
        }
        else
        {
            foreach (var day in incomeDetails)
            {
                this.DrawLine(
                    b,
                    $"{day.Label}   {day.Amount}g",
                    leftX,
                    leftY
                );

                leftY += 30;
            }
        }

        if (expenseDetails.Count == 0)
        {
            this.DrawLine(b, I18n.Get("finance.no_expense_days_recorded"), rightX, rightY);
            rightY += 35;
        }
        else
        {
            foreach (var day in expenseDetails)
            {
                this.DrawLine(
                    b,
                    $"{day.Label}   {this.FormatExpense(day.Amount)}",
                    rightX,
                    rightY
                );

                rightY += 30;
            }
        }

        return Math.Max(leftY, rightY) + 55;
    }

    private void DrawSeasonTrendChart(SpriteBatch b, List<int> values, int x, int y)
    {
        int chartWidth = 620;
        int chartHeight = 260;

        int left = x;
        int top = y;
        int bottom = y + chartHeight;
        int right = x + chartWidth;

        int maxValue = values.Count > 0 ? values.Max() : 0;

        if (maxValue <= 0)
            maxValue = 1;

        this.DrawLineSegment(
            b,
            new Vector2(left, bottom),
            new Vector2(right, bottom),
            2
        );

        this.DrawLineSegment(
            b,
            new Vector2(left, top),
            new Vector2(left, bottom),
            2
        );

        if (values.Count >= 2)
        {
            float xStep = chartWidth / (float)(values.Count - 1);

            Vector2? previousPoint = null;

            for (int i = 0; i < values.Count; i++)
            {
                float pointX = left + i * xStep;
                float normalized = values[i] / (float)maxValue;
                float pointY = bottom - normalized * chartHeight;

                Vector2 currentPoint = new Vector2(pointX, pointY);

                if (previousPoint.HasValue)
                {
                    this.DrawLineSegment(
                        b,
                        previousPoint.Value,
                        currentPoint,
                        3
                    );
                }

                previousPoint = currentPoint;

                b.Draw(
                    Game1.staminaRect,
                    new Rectangle(
                        (int)pointX - 3,
                        (int)pointY - 3,
                        6,
                        6
                    ),
                    this.chartColor
                );
            }
        }

        Utility.drawTextWithShadow(
            b,
            "1",
            Game1.smallFont,
            new Vector2(left - 5, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            "14",
            Game1.smallFont,
            new Vector2(left + chartWidth / 2 - 10, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            "28",
            Game1.smallFont,
            new Vector2(right - 20, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            $"{maxValue}g",
            Game1.smallFont,
            new Vector2(left + 10, top - 35),
            Game1.textColor
        );
    }

    private void DrawAnnualTrendChart(SpriteBatch b, List<int> values, int x, int y)
    {
        int chartWidth = 620;
        int chartHeight = 260;

        int left = x;
        int top = y;
        int bottom = y + chartHeight;
        int right = x + chartWidth;

        int maxValue = values.Count > 0 ? values.Max() : 0;

        if (maxValue <= 0)
            maxValue = 1;

        this.DrawLineSegment(
            b,
            new Vector2(left, bottom),
            new Vector2(right, bottom),
            2
        );

        this.DrawLineSegment(
            b,
            new Vector2(left, top),
            new Vector2(left, bottom),
            2
        );

        if (values.Count >= 2)
        {
            float xStep = chartWidth / (float)(values.Count - 1);

            Vector2? previousPoint = null;

            for (int i = 0; i < values.Count; i++)
            {
                float pointX = left + i * xStep;
                float normalized = values[i] / (float)maxValue;
                float pointY = bottom - normalized * chartHeight;

                Vector2 currentPoint = new Vector2(pointX, pointY);

                if (previousPoint.HasValue)
                {
                    this.DrawLineSegment(
                        b,
                        previousPoint.Value,
                        currentPoint,
                        2
                    );
                }

                previousPoint = currentPoint;
            }
        }

        Utility.drawTextWithShadow(
            b,
            I18n.Get("chart.spring_1"),
            Game1.smallFont,
            new Vector2(left - 5, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            I18n.Get("chart.summer_1"),
            Game1.smallFont,
            new Vector2(left + chartWidth * 0.25f - 25, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            I18n.Get("chart.fall_1"),
            Game1.smallFont,
            new Vector2(left + chartWidth * 0.50f - 20, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            I18n.Get("chart.winter_1"),
            Game1.smallFont,
            new Vector2(left + chartWidth * 0.75f - 25, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            $"{maxValue}g",
            Game1.smallFont,
            new Vector2(left + 10, top - 35),
            Game1.textColor
        );
    }

    private void DrawLineSegment(
        SpriteBatch b,
        Vector2 start,
        Vector2 end,
        int thickness,
        Color? color = null
    )
    {
        Vector2 edge = end - start;

        float angle = (float)Math.Atan2(edge.Y, edge.X);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                (int)start.X,
                (int)start.Y,
                (int)edge.Length(),
                thickness
            ),
            null,
            color ?? this.chartColor,
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0
        );
    }

    private void DrawItemSummaryLine(SpriteBatch b, ItemSummary item, int x, int y)
    {
        if (item.ItemId == LedgerService.UnclassifiedIncomeItemId)
        {
            string unclassifiedDisplayName = I18n.Get("income.unclassified_channel");

            this.DrawLine(
                b,
                $"{unclassifiedDisplayName}   {item.Amount}g",
                x,
                y
            );

            return;
        }

        int textX = x;
        string displayName = item.ItemName;

        if (this.IsNonItemIncomeSummary(item))
        {
            displayName = I18n.Category(displayName);
            this.DrawLine(
                b,
                $"{displayName}   {item.Amount}g",
                textX,
                y
            );

            return;
        }

        if (!string.IsNullOrWhiteSpace(item.ItemId))
        {
            try
            {
                Item iconItem = ItemRegistry.Create(item.ItemId);
                displayName = iconItem.DisplayName;

                iconItem.drawInMenu(
                    b,
                    new Vector2(x, y - 12),
                    0.65f,
                    1f,
                    0.9f,
                    StackDrawType.Hide,
                    Color.White,
                    false
                );

                textX = x + 55;
            }
            catch
            {
                textX = x;
            }
        }

        this.DrawLine(
            b,
            $"{displayName}   x{item.Quantity}   {item.Amount}g",
            textX,
            y
        );
    }

    private bool IsNonItemIncomeSummary(ItemSummary item)
    {
        if (string.IsNullOrWhiteSpace(item.ItemId))
            return true;

        return string.Equals(
            item.ItemId,
            "RC.ExchangeTransfer",
            StringComparison.OrdinalIgnoreCase
        );
    }

    private void DrawExpenseSummaryLine(SpriteBatch b, ExpenseSummary expense, int x, int y)
    {
        this.DrawLine(
            b,
            $"{I18n.Category(expense.Category)}   {this.FormatSignedMoney(expense.Amount)}",
            x,
            y
        );
    }

    private string FormatExpense(int amount)
    {
        if (amount <= 0)
            return "0g";

        return $"-{amount}g";
    }
    private string FormatDebt(int amount)
{
    if (amount <= 0)
        return "0g";

    return $"-{amount}g";
}


    private string FormatSignedMoney(int amount)
    {
        if (amount > 0)
            return $"+{amount}g";

        if (amount < 0)
            return $"{amount}g";

        return "0g";
    }

    private void UpdateContentHeight(int finalY)
    {
        int scrolledContentStartY = this.contentStartY - this.scrollOffset;

        this.currentContentHeight = Math.Max(
            0,
            finalY - scrolledContentStartY
        );

        this.scrollOffset = Math.Clamp(
            this.scrollOffset,
            0,
            this.GetMaxScrollOffset()
        );
    }

    private void DrawLine(SpriteBatch b, string text, int x, int y)
    {
        float scale = this.GetBodyTextScale();

        if (Math.Abs(scale - 1f) < 0.001f)
        {
            Utility.drawTextWithShadow(
                b,
                text,
                Game1.smallFont,
                new Vector2(x, y),
                Game1.textColor
            );

            return;
        }

        b.DrawString(
            Game1.smallFont,
            text,
            new Vector2(x, y),
            Game1.textColor,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            1f
        );
    }

    private float GetBodyTextScale()
    {
        string languageCode = LocalizedContentManager.CurrentLanguageCode.ToString();

        return languageCode switch
        {
            "zh" => 1.00f,
            "en" => 0.75f,
            "ja" => 0.80f,
            "ko" => 0.65f,
            "pt" => 0.82f,
            "es" => 0.85f,
            "fr" => 0.60f,
            "de" => 0.60f,
            "it" => 0.85f,
            "ru" => 0.85f,
            "tr" => 0.85f,
            "hu" => 0.85f,
            _ => 0.85f
        };
    }
}