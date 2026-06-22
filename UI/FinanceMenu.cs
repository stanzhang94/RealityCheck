using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealityCheck.Services;
using StardewValley;
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
    private int currentContentHeight = 0;

    private readonly Rectangle dailyTab;
    private readonly Rectangle seasonalTab;
    private readonly Rectangle annualTab;

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
        // base.receiveScrollWheelAction(direction);

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
int contentStartY = y + 180;
int contentY = contentStartY - this.scrollOffset;

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

        // this.DrawScrollBar(b, x + width - 45, y + 165, height - 220);

        this.drawMouse(b);
    }

    private int GetMaxScrollOffset()
    {
        int visibleHeight = this.contentBottom - this.contentTop;

        return Math.Max(
            0,
            this.currentContentHeight - visibleHeight
        );
    }

        private void DrawScrollBar(SpriteBatch b, int x, int y, int height)
    {
        int visibleHeight = this.contentBottom - this.contentTop;
        int totalHeight = this.currentContentHeight;

        if (totalHeight <= visibleHeight)
            return;

        int maxScroll = this.GetMaxScrollOffset();

        int barHeight = Math.Max(
            40,
            (int)(height * (visibleHeight / (float)totalHeight))
        );

        int movableHeight = height - barHeight;

        int barY = y;

        if (maxScroll > 0)
        {
            barY = y + (int)(movableHeight * (this.scrollOffset / (float)maxScroll));
        }

        IClickableMenu.drawTextureBox(
            b,
            x,
            y,
            20,
            height,
            Color.LightGray
        );

        IClickableMenu.drawTextureBox(
            b,
            x,
            barY,
            20,
            barHeight,
            Color.White
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
        y += 55;

        this.DrawLine(b, "Items Sold Today", x, y);
        y += 45;

        var items = this.analyticsService.GetTodayItemSummaries();

        if (items.Count == 0)
{
    this.DrawLine(b, "No sales recorded today.", x, y);

    this.UpdateContentHeight(y + 50);

    return;
}

        foreach (var item in items)
        {
            this.DrawLine(
                b,
                $"{item.ItemName}   x{item.Quantity}   {item.Amount}g",
                x,
                y
            );

            y += 38;
        }
          this.UpdateContentHeight(y + 50);
    }

    private void DrawSeasonalReport(SpriteBatch b, int x, int y)
    {
        this.DrawLine(b, $"Season: Year {Game1.year} {Game1.currentSeason}", x, y);
        y += 45;

        this.DrawLine(b, $"Seasonal Income: {this.analyticsService.GetSeasonIncome()}g", x, y);
        y += 55;

        this.DrawLine(b, "Items Sold This Season", x, y);
        y += 45;

        var items = this.analyticsService.GetSeasonItemSummaries();

        if (items.Count == 0)
{
    this.DrawLine(b, "No sales recorded this season.", x, y);

    this.UpdateContentHeight(y + 150);

    return;
}

        foreach (var item in items)
        {
            this.DrawLine(
                b,
                $"{item.ItemName}   x{item.Quantity}   {item.Amount}g",
                x,
                y
            );

            y += 38;
        }

        y += 60;

       this.DrawLine(
    b,
    "28-Day Income Trend",
    x,
    y
    );

            y += 45;

            var trend = this.analyticsService.GetSeasonDailyIncome();

            for (int i = 0; i < trend.Count; i++)
            {
                this.DrawLine(
                    b,
                    $"Day {i + 1}: {trend[i]}g",
                    x,
                    y
                );

                y += 30;
            }
        this.UpdateContentHeight(y + 150);
    }

private void DrawAnnualReport(SpriteBatch b, int x, int y)
{
    this.DrawLine(b, $"Year: {Game1.year}", x, y);
    y += 45;

    this.DrawLine(b, $"Annual Income: {this.analyticsService.GetYearIncome()}g", x, y);
    y += 55;

    this.DrawLine(b, "Items Sold This Year", x, y);
    y += 45;

    var items = this.analyticsService.GetYearItemSummaries();

    if (items.Count == 0)
    {
        this.DrawLine(b, "No sales recorded this year.", x, y);

        this.UpdateContentHeight(y + 50);

        return;
    }

    foreach (var item in items)
    {
        this.DrawLine(
            b,
            $"{item.ItemName}   x{item.Quantity}   {item.Amount}g",
            x,
            y
        );

        y += 38;
    }

    y += 60;

    this.DrawLine(b, "112-day income trend: Coming Soon", x, y);

    this.UpdateContentHeight(y + 150);
}
private void UpdateContentHeight(int finalY)
{
    int contentStartY = Game1.uiViewport.Height / 2 - 300 + 180 - this.scrollOffset;

    this.currentContentHeight = Math.Max(
        0,
        finalY - contentStartY
    );

    this.scrollOffset = Math.Clamp(
        this.scrollOffset,
        0,
        this.GetMaxScrollOffset()
    );
}
    private void DrawLine(SpriteBatch b, string text, int x, int y)
    {
        if (y < this.contentTop || y > this.contentBottom)
            return;

        Utility.drawTextWithShadow(
            b,
            text,
            Game1.smallFont,
            new Vector2(x, y),
            Game1.textColor
        );
    }
}