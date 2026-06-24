using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealityCheck.Models;
using RealityCheck.Services;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace RealityCheck.UI;

public class FinanceMenu : IClickableMenu
{
    private readonly AnalyticsService analyticsService;
    private readonly TaxService taxService;

    private ReportTab currentTab = ReportTab.Daily;

    private int scrollOffset = 0;

    private int contentTop;
    private int contentBottom;
    private int contentStartY;
    private int currentContentHeight = 0;

    private Rectangle previousScissorRectangle;

    private readonly Rectangle dailyTab;
    private readonly Rectangle seasonalTab;
    private readonly Rectangle annualTab;
    private readonly Rectangle taxTab;

    private readonly Color chartColor = new Color(92, 63, 34);

    private enum ReportTab
    {
        Daily,
        Seasonal,
        Annual,
        Tax
    }

    public FinanceMenu(
        LedgerService ledgerService,
        AnalyticsService analyticsService
    )
    {
        this.analyticsService = analyticsService;
        this.taxService = new TaxService(ledgerService);

        int x = Game1.uiViewport.Width / 2 - 400;
        int y = Game1.uiViewport.Height / 2 - 300;

        this.dailyTab = new Rectangle(x + 35, y + 25, 150, 55);
        this.seasonalTab = new Rectangle(x + 195, y + 25, 185, 55);
        this.annualTab = new Rectangle(x + 390, y + 25, 150, 55);
        this.taxTab = new Rectangle(x + 550, y + 25, 155, 55);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.dailyTab.Contains(x, y))
        {
            this.currentTab = ReportTab.Daily;
            this.scrollOffset = 0;
            Game1.playSound("smallSelect");
        }
        else if (this.seasonalTab.Contains(x, y))
        {
            this.currentTab = ReportTab.Seasonal;
            this.scrollOffset = 0;
            Game1.playSound("smallSelect");
        }
        else if (this.annualTab.Contains(x, y))
        {
            this.currentTab = ReportTab.Annual;
            this.scrollOffset = 0;
            Game1.playSound("smallSelect");
        }
        else if (this.taxTab.Contains(x, y))
        {
            this.currentTab = ReportTab.Tax;
            this.scrollOffset = 0;
            Game1.playSound("smallSelect");
        }
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
            I18n.Get("finance.items_sold_today"),
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
            I18n.Get("finance.items_sold_this_season"),
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
            I18n.Get("finance.items_sold_this_year"),
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
            this.DrawLine(b, I18n.Get("finance.no_sales_recorded"), leftX, leftY);
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
        int thickness
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
            this.chartColor,
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0
        );
    }

    private void DrawItemSummaryLine(SpriteBatch b, ItemSummary item, int x, int y)
    {
        int textX = x;
        string displayName = item.ItemName;

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
            "en" => 0.65f,
            "ja" => 0.95f,
            "ko" => 0.95f,
            "pt" => 0.82f,
            "es" => 0.85f,
            "fr" => 0.85f,
            "de" => 0.78f,
            "it" => 0.85f,
            "ru" => 0.85f,
            "tr" => 0.85f,
            "hu" => 0.85f,
            _ => 0.85f
        };
    }
}