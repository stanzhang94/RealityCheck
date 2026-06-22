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
    private readonly LedgerService ledgerService;
    private readonly AnalyticsService analyticsService;

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

    private readonly Color chartColor = new Color(92, 63, 34);

    private enum ReportTab
    {
        Daily,
        Seasonal,
        Annual
    }

    public FinanceMenu(
        LedgerService ledgerService,
        AnalyticsService analyticsService
    )
    {
        this.ledgerService = ledgerService;
        this.analyticsService = analyticsService;

        int x = Game1.uiViewport.Width / 2 - 400;
        int y = Game1.uiViewport.Height / 2 - 300;

        this.dailyTab = new Rectangle(x + 60, y + 25, 170, 55);
        this.seasonalTab = new Rectangle(x + 240, y + 25, 210, 55);
        this.annualTab = new Rectangle(x + 460, y + 25, 190, 55);
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
            "Reality Check",
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
        this.DrawTab(b, this.dailyTab, "Daily Report", this.currentTab == ReportTab.Daily);
        this.DrawTab(b, this.seasonalTab, "Seasonal Report", this.currentTab == ReportTab.Seasonal);
        this.DrawTab(b, this.annualTab, "Annual Report", this.currentTab == ReportTab.Annual);
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
        this.DrawLine(b, $"Date: Year {Game1.year} {Game1.currentSeason} {Game1.dayOfMonth}", x, y);
        y += 45;

        this.DrawLine(b, $"Today's Income: {this.analyticsService.GetTodayIncome()}g", x, y);
        y += 35;

        this.DrawLine(b, $"Today's Expenses: {this.analyticsService.GetTodayExpense()}g", x, y);
        y += 35;

        this.DrawLine(b, $"Today's Net: {this.analyticsService.GetTodayNet()}g", x, y);
        y += 55;

        y = this.DrawItemAndExpenseColumns(
            b,
            "Items Sold Today",
            this.analyticsService.GetTodayItemSummaries(),
            "Expense Breakdown",
            this.analyticsService.GetTodayExpenseBreakdown(),
            x,
            y
        );

        this.UpdateContentHeight(y + 50);
    }

    private void DrawSeasonalReport(SpriteBatch b, int x, int y)
    {
        this.DrawLine(b, $"Season: Year {Game1.year} {Game1.currentSeason}", x, y);
        y += 45;

        this.DrawLine(b, $"Seasonal Income: {this.analyticsService.GetSeasonIncome()}g", x, y);
        y += 35;

        this.DrawLine(b, $"Seasonal Expenses: {this.analyticsService.GetSeasonExpense()}g", x, y);
        y += 35;

        this.DrawLine(b, $"Seasonal Net: {this.analyticsService.GetSeasonNet()}g", x, y);
        y += 55;

        y = this.DrawItemAndExpenseColumns(
            b,
            "Items Sold This Season",
            this.analyticsService.GetSeasonItemSummaries(),
            "Expense Breakdown",
            this.analyticsService.GetSeasonExpenseBreakdown(),
            x,
            y
        );

        this.DrawLine(b, "Income Trend", x, y);
        y += 50;

        var incomeTrend = this.analyticsService.GetSeasonDailyIncome();

        this.DrawSeasonTrendChart(
            b,
            incomeTrend,
            x,
            y
        );

        y += 330;

        this.DrawLine(b, "Expense Trend", x, y);
        y += 50;

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
        this.DrawLine(b, $"Year: {Game1.year}", x, y);
        y += 45;

        this.DrawLine(b, $"Annual Income: {this.analyticsService.GetYearIncome()}g", x, y);
        y += 35;

        this.DrawLine(b, $"Annual Expenses: {this.analyticsService.GetYearExpense()}g", x, y);
        y += 35;

        this.DrawLine(b, $"Annual Net: {this.analyticsService.GetYearNet()}g", x, y);
        y += 55;

        y = this.DrawItemAndExpenseColumns(
            b,
            "Items Sold This Year",
            this.analyticsService.GetYearItemSummaries(),
            "Expense Breakdown",
            this.analyticsService.GetYearExpenseBreakdown(),
            x,
            y
        );

        this.DrawLine(b, "Income Trend", x, y);
        y += 50;

        var incomeTrend = this.analyticsService.GetYearDailyIncome();

        this.DrawAnnualTrendChart(
            b,
            incomeTrend,
            x,
            y
        );

        y += 360;

        this.DrawLine(b, "Expense Trend", x, y);
        y += 50;

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
            this.DrawLine(b, "No sales recorded.", leftX, leftY);
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
            this.DrawLine(b, "No expenses recorded.", rightX, rightY);
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

        this.DrawLine(b, "Daily Income Details", leftX, y);
        this.DrawLine(b, "Daily Expense Details", rightX, y);

        y += 45;

        int leftY = y;
        int rightY = y;

        if (incomeDetails.Count == 0)
        {
            this.DrawLine(b, "No income days recorded.", leftX, leftY);
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
            this.DrawLine(b, "No expense days recorded.", rightX, rightY);
            rightY += 35;
        }
        else
        {
            foreach (var day in expenseDetails)
            {
                this.DrawLine(
                    b,
                    $"{day.Label}   {day.Amount}g",
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
            "Spring 1",
            Game1.smallFont,
            new Vector2(left - 5, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            "Summer 1",
            Game1.smallFont,
            new Vector2(left + chartWidth * 0.25f - 25, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            "Fall 1",
            Game1.smallFont,
            new Vector2(left + chartWidth * 0.50f - 20, bottom + 10),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            "Winter 1",
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

        if (!string.IsNullOrWhiteSpace(item.ItemId))
        {
            try
            {
                Item iconItem = ItemRegistry.Create(item.ItemId);

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
            $"{item.ItemName}   x{item.Quantity}   {item.Amount}g",
            textX,
            y
        );
    }

    private void DrawExpenseSummaryLine(SpriteBatch b, ExpenseSummary expense, int x, int y)
    {
        this.DrawLine(
            b,
            $"{expense.Category}   {expense.Amount}g",
            x,
            y
        );
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
        Utility.drawTextWithShadow(
            b,
            text,
            Game1.smallFont,
            new Vector2(x, y),
            Game1.textColor
        );
    }
}