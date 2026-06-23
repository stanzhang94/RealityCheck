using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealityCheck.Models;
using RealityCheck.Services;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.UI;

public class TaxNoticeMenu : IClickableMenu
{
    private const int BusinessPropertyTaxThreshold = 20;

    private const int KegDailyTax = 48;
    private const int PreservesJarDailyTax = 64;
    private const int CaskDailyTax = 8;
    private const int BeeHouseDailyTax = 34;
    private const int MayonnaiseMachineDailyTax = 260;
    private const int CheesePressDailyTax = 51;
    private const int LoomDailyTax = 26;
    private const int OilMakerDailyTax = 88;
    private const int DehydratorDailyTax = 380;
    private const int FishSmokerDailyTax = 137;

    private readonly LedgerService ledgerService;
    private readonly TaxRecord record;

    private readonly List<NoticeElement> elements = new();

    private int scrollOffset = 0;
    private int contentHeight = 0;
    private int contentTop;
    private int contentBottom;

    private readonly Rectangle closeButtonBounds;

    public TaxNoticeMenu(
        LedgerService ledgerService,
        TaxRecord record
    )
        : base(
            Game1.uiViewport.Width / 2 - 480,
            Game1.uiViewport.Height / 2 - 360,
            960,
            720,
            true
        )
    {
        this.ledgerService = ledgerService;
        this.record = record;

        this.closeButtonBounds = new Rectangle(
            this.xPositionOnScreen + this.width - 70,
            this.yPositionOnScreen + 28,
            36,
            36
        );

        this.BuildDocument();
        this.contentHeight = this.CalculateContentHeight();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.closeButtonBounds.Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.exitActiveMenu();
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int oldOffset = this.scrollOffset;

        if (direction > 0)
            this.scrollOffset -= 64;
        else if (direction < 0)
            this.scrollOffset += 64;

        this.scrollOffset = Math.Clamp(
            this.scrollOffset,
            0,
            this.GetMaxScrollOffset()
        );

        if (this.scrollOffset != oldOffset)
            Game1.playSound("shiny4");
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(
            this.xPositionOnScreen,
            this.yPositionOnScreen,
            this.width,
            this.height,
            false,
            true
        );

        Rectangle paperRect = new Rectangle(
            this.xPositionOnScreen + 28,
            this.yPositionOnScreen + 26,
            this.width - 56,
            this.height - 52
        );

        b.Draw(
            Game1.fadeToBlackRect,
            paperRect,
            new Color(247, 239, 221)
        );

        IClickableMenu.drawTextureBox(
            b,
            paperRect.X,
            paperRect.Y,
            paperRect.Width,
            paperRect.Height,
            Color.White
        );

        this.DrawCloseButton(b);

        this.contentTop = paperRect.Y + 24;
        this.contentBottom = paperRect.Bottom - 32;

        int contentLeft = paperRect.X + 42;
        int contentRight = paperRect.Right - 42;
        int y = this.contentTop - this.scrollOffset;

        foreach (NoticeElement element in this.elements)
        {
            if (element.IsDivider)
            {
                int dividerY = y + element.TopPadding + 6;

                if (dividerY >= this.contentTop && dividerY <= this.contentBottom)
                {
                    b.Draw(
                        Game1.staminaRect,
                        new Rectangle(
                            contentLeft,
                            dividerY,
                            contentRight - contentLeft,
                            2
                        ),
                        new Color(120, 90, 60)
                    );
                }

                y += element.TopPadding + 14 + element.BottomPadding;
                continue;
            }

            SpriteFont font = element.UseDialogueFont
                ? Game1.dialogueFont
                : Game1.smallFont;

            Vector2 size = font.MeasureString(element.Text) * element.Scale;

            float drawX = contentLeft;

            if (element.Alignment == TextAlignment.Center)
            {
                drawX = contentLeft + ((contentRight - contentLeft) - size.X) / 2f;
            }
            else if (element.Alignment == TextAlignment.Right)
            {
                drawX = contentRight - size.X;
            }

            float drawY = y + element.TopPadding;

            if (drawY + size.Y >= this.contentTop && drawY <= this.contentBottom)
            {
                b.DrawString(
                    font,
                    element.Text,
                    new Vector2(drawX, drawY),
                    element.Color,
                    0f,
                    Vector2.Zero,
                    element.Scale,
                    SpriteEffects.None,
                    0.86f
                );
            }

            y += (int)size.Y + element.TopPadding + element.BottomPadding;
        }

        this.DrawScrollHint(b, paperRect);
        this.drawMouse(b);
    }

    private void DrawCloseButton(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(
            b,
            this.closeButtonBounds.X,
            this.closeButtonBounds.Y,
            this.closeButtonBounds.Width,
            this.closeButtonBounds.Height,
            Color.White
        );

        b.DrawString(
            Game1.smallFont,
            "X",
            new Vector2(
                this.closeButtonBounds.X + 10,
                this.closeButtonBounds.Y + 6
            ),
            new Color(110, 40, 40)
        );
    }

    private void DrawScrollHint(SpriteBatch b, Rectangle paperRect)
    {
        if (this.GetMaxScrollOffset() <= 0)
            return;

        string hint = "Mouse Wheel to Scroll";
        Vector2 size = Game1.smallFont.MeasureString(hint) * 0.75f;

        b.DrawString(
            Game1.smallFont,
            hint,
            new Vector2(
                paperRect.Right - size.X - 18,
                paperRect.Bottom - size.Y - 10
            ),
            new Color(110, 90, 70),
            0f,
            Vector2.Zero,
            0.75f,
            SpriteEffects.None,
            0.87f
        );
    }

    private void BuildDocument()
    {
        this.elements.Clear();

        Color darkBrown = new Color(85, 60, 40);
        Color midBrown = new Color(110, 80, 55);
        Color sealRed = new Color(135, 45, 45);

        this.AddText(
            "Pelican Town Revenue Service",
            1.05f,
            darkBrown,
            TextAlignment.Center,
            useDialogueFont: true,
            topPadding: 0,
            bottomPadding: 0
        );

        this.AddText(
            "&",
            0.95f,
            darkBrown,
            TextAlignment.Center,
            useDialogueFont: true,
            topPadding: -2,
            bottomPadding: 0
        );

        this.AddText(
            "Pelican Town Property Assessment Office",
            0.86f,
            midBrown,
            TextAlignment.Center,
            useDialogueFont: false,
            topPadding: 0,
            bottomPadding: 12
        );

        this.AddDivider(0, 12);

        this.AddText(
            "Joint Weekly Tax Notice",
            0.84f,
            darkBrown,
            TextAlignment.Center,
            useDialogueFont: true,
            topPadding: 8,
            bottomPadding: 10
        );

        this.AddText(
            "Thank you for your continued support of Pelican Town's fiscal development.",
            0.82f,
            midBrown,
            TextAlignment.Center,
            useDialogueFont: false,
            topPadding: 0,
            bottomPadding: 16
        );

        this.AddText(
            $"Tax Period: Year {this.record.Year} {this.FormatSeason(this.record.Season)} {this.record.CoveredStartDay} - {this.FormatSeason(this.record.Season)} {this.record.CoveredEndDay}",
            0.86f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            4
        );

        this.AddText(
            $"Settlement Date: Year {this.record.SettlementYear} {this.FormatSeason(this.record.SettlementSeason)} {this.record.SettlementDay}",
            0.86f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            14
        );

        this.AddSectionHeader("Income Tax", darkBrown);
        this.BuildIncomeSection(darkBrown, midBrown);

        this.AddSectionHeader("Property Tax", darkBrown);
        this.BuildPropertySection(darkBrown, midBrown);

        this.AddSectionHeader("Business Property Tax", darkBrown);
        this.BuildBusinessPropertySection(darkBrown, midBrown);

        this.AddSectionHeader("Total Tax Due", darkBrown);
        this.BuildTotalSection(darkBrown, midBrown);

        this.AddDivider(14, 14);

        this.AddText(
            "Issued by:",
            0.82f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            4
        );

        this.AddText(
            "Pelican Town Revenue Service / Property Assessment Office",
            0.86f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            18
        );

        this.AddText(
            "Authorized Signature: __________________________",
            0.82f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            28
        );

        this.AddText(
            "OFFICIAL SEAL",
            0.92f,
            sealRed,
            TextAlignment.Right,
            false,
            -24,
            4
        );

        this.AddText(
            "Questions or appeals may be submitted through Nexus Mods - Reality Check - Posts.",
            0.78f,
            midBrown,
            TextAlignment.Left,
            false,
            12,
            4
        );

        this.AddText(
            "Please note that submitting an appeal does not guarantee adjustment, refund, review priority, or emotional closure of any kind.",
            0.76f,
            midBrown,
            TextAlignment.Left,
            false,
            0,
            10
        );
    }

    private void BuildIncomeSection(Color darkBrown, Color midBrown)
    {
        this.AddBodyLine(
            $"Taxable Shipping Bin Income: {this.FormatGold(this.record.TaxableShippingBinIncome)}",
            darkBrown
        );

        this.AddBodyLine(
            $"Applied Tax Rate: {this.FormatPercent(this.record.IncomeTaxRate)}",
            darkBrown
        );

        this.AddBodyLine(
            $"Formula: {this.FormatGold(this.record.TaxableShippingBinIncome)} x {this.FormatPercent(this.record.IncomeTaxRate)} = {this.FormatGold(this.record.IncomeTaxAmount)}",
            darkBrown
        );

        this.AddBodyLine(
            $"Income Tax Due: {this.FormatGold(this.record.IncomeTaxAmount)}",
            darkBrown,
            bottomPadding: 12
        );
    }

    private void BuildPropertySection(Color darkBrown, Color midBrown)
    {
        List<PropertyTaxDailyAssessment> assessments =
            this.ledgerService.GetPropertyTaxDailyAssessments()
            .Where(a =>
                a.Year == this.record.Year
                && a.Season == this.record.Season
                && a.Day >= this.record.CoveredStartDay
                && a.Day <= this.record.CoveredEndDay
            )
            .OrderBy(a => a.Day)
            .ToList();

        double rc = assessments.Sum(a => a.ReplacementCostAmount);
        double ipv = assessments.Sum(a => a.IncomePotentialValueAmount);
        double up = assessments.Sum(a => a.UtilityPremiumAmount);
        double rsp = assessments.Sum(a => a.RiskShieldPremiumAmount);
        double ad = assessments.Sum(a => a.AgriculturalDeductionAmount);
        double admin = assessments.Sum(a => a.AdministrativeFeeAmount);
        double doc = assessments.Sum(a => a.DocumentationFeeAmount);

        double depreciationFactor = assessments.Count > 0
            ? assessments[0].DepreciationFactor
            : 1.0;

        this.AddBodyLine($"Replacement Cost (RC): {this.FormatGold(rc)}", darkBrown);
        this.AddBodyLine($"Income Potential Value (IPV): {this.FormatGold(ipv)}", darkBrown);
        this.AddBodyLine($"Utility Premium (UP): {this.FormatGold(up)}", darkBrown);
        this.AddBodyLine($"Risk Shield Premium (RSP): {this.FormatGold(rsp)}", darkBrown);
        this.AddBodyLine($"Depreciation Factor: {this.FormatPercent(depreciationFactor)}", darkBrown);
        this.AddBodyLine($"Agricultural Deduction (AD): -{this.FormatGoldValueOnly(ad)}", darkBrown);
        this.AddBodyLine($"Administrative Fee: {this.FormatGold(admin)}", darkBrown);
        this.AddBodyLine($"Documentation Fee: {this.FormatGold(doc)}", darkBrown);

        this.AddBodyLine(
            $"Formula: (({this.FormatGold(rc)} + {this.FormatGold(ipv)} + {this.FormatGold(up)} + {this.FormatGold(rsp)}) x {this.FormatPercent(depreciationFactor)}) - {this.FormatGoldValueOnly(ad)} + {this.FormatGold(admin)} + {this.FormatGold(doc)} = {this.FormatGold(this.record.PropertyTaxAmount)}",
            darkBrown
        );

        this.AddBodyLine(
            $"Property Tax Due: {this.FormatGold(this.record.PropertyTaxAmount)}",
            darkBrown,
            bottomPadding: 12
        );
    }

    private void BuildBusinessPropertySection(Color darkBrown, Color midBrown)
    {
        List<BusinessPropertyTaxDailyAssessment> assessments =
            this.ledgerService.GetBusinessPropertyTaxDailyAssessments()
            .Where(a =>
                a.Year == this.record.Year
                && a.Season == this.record.Season
                && a.Day >= this.record.CoveredStartDay
                && a.Day <= this.record.CoveredEndDay
            )
            .OrderBy(a => a.Day)
            .ToList();

        var businessLines = new List<string>();

        this.AddBusinessMachineLine(
            businessLines,
            "Keg",
            assessments,
            a => a.KegCount,
            KegDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Preserves Jar",
            assessments,
            a => a.PreservesJarCount,
            PreservesJarDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Cask",
            assessments,
            a => a.CaskCount,
            CaskDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Bee House",
            assessments,
            a => a.BeeHouseCount,
            BeeHouseDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Mayonnaise Machine",
            assessments,
            a => a.MayonnaiseMachineCount,
            MayonnaiseMachineDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Cheese Press",
            assessments,
            a => a.CheesePressCount,
            CheesePressDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Loom",
            assessments,
            a => a.LoomCount,
            LoomDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Oil Maker",
            assessments,
            a => a.OilMakerCount,
            OilMakerDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Dehydrator",
            assessments,
            a => a.DehydratorCount,
            DehydratorDailyTax
        );

        this.AddBusinessMachineLine(
            businessLines,
            "Fish Smoker",
            assessments,
            a => a.FishSmokerCount,
            FishSmokerDailyTax
        );

        if (businessLines.Count == 0)
        {
            this.AddBodyLine(
                "No assessed business equipment exceeded the taxable threshold.",
                darkBrown
            );
        }
        else
        {
            foreach (string line in businessLines)
                this.AddBodyLine(line, darkBrown);
        }

        this.AddBodyLine(
            $"Business Property Tax Due: {this.FormatGold(this.record.BusinessPropertyTaxAmount)}",
            darkBrown,
            bottomPadding: 12
        );
    }

    private void BuildTotalSection(Color darkBrown, Color midBrown)
    {
        this.AddBodyLine($"Income Tax: {this.FormatGold(this.record.IncomeTaxAmount)}", darkBrown);
        this.AddBodyLine($"Property Tax: {this.FormatGold(this.record.PropertyTaxAmount)}", darkBrown);
        this.AddBodyLine($"Business Property Tax: {this.FormatGold(this.record.BusinessPropertyTaxAmount)}", darkBrown);
        this.AddBodyLine(
            $"Total: {this.FormatGold(this.record.TotalTaxAmount)}",
            darkBrown,
            bottomPadding: 18
        );
    }

    private void AddBusinessMachineLine(
        List<string> lines,
        string displayName,
        List<BusinessPropertyTaxDailyAssessment> assessments,
        Func<BusinessPropertyTaxDailyAssessment, int> countSelector,
        int dailyTax
    )
    {
        var groups = assessments
            .Select(a => this.GetTaxableBusinessPropertyCount(countSelector(a)))
            .Where(count => count > 0)
            .GroupBy(count => count)
            .Select(g => new BusinessMachineGroup
            {
                Count = g.Key,
                Days = g.Count()
            })
            .OrderBy(g => g.Count)
            .ToList();

        if (groups.Count == 0)
            return;

        int totalAmount = groups.Sum(g => g.Count * g.Days * dailyTax);

        string countSummary;

        if (groups.Count == 1)
        {
            BusinessMachineGroup g = groups[0];
            countSummary = $"{g.Count} x {g.Days} x {dailyTax}g";
        }
        else
        {
            countSummary = string.Join(
                " + ",
                groups.Select(g => $"{g.Count} x {g.Days}")
            );

            countSummary = $"({countSummary}) x {dailyTax}g";
        }

        lines.Add($"{displayName}: {countSummary} = {this.FormatGold(totalAmount)}");
    }

    private int GetTaxableBusinessPropertyCount(int count)
    {
        if (count <= BusinessPropertyTaxThreshold)
            return 0;

        return count;
    }

    private void AddSectionHeader(string text, Color color)
    {
        this.AddText(
            text,
            0.92f,
            color,
            TextAlignment.Left,
            useDialogueFont: false,
            topPadding: 6,
            bottomPadding: 8
        );
    }

    private void AddBodyLine(
        string text,
        Color color,
        int bottomPadding = 4
    )
    {
        this.AddText(
            text,
            0.82f,
            color,
            TextAlignment.Left,
            useDialogueFont: false,
            topPadding: 0,
            bottomPadding: bottomPadding
        );
    }

    private void AddText(
        string text,
        float scale,
        Color color,
        TextAlignment alignment,
        bool useDialogueFont,
        int topPadding,
        int bottomPadding
    )
    {
        this.elements.Add(new NoticeElement
        {
            Text = text,
            Scale = scale,
            Color = color,
            Alignment = alignment,
            UseDialogueFont = useDialogueFont,
            TopPadding = topPadding,
            BottomPadding = bottomPadding,
            IsDivider = false
        });
    }

    private void AddDivider(int topPadding, int bottomPadding)
    {
        this.elements.Add(new NoticeElement
        {
            Text = "",
            Scale = 1f,
            Color = Color.White,
            Alignment = TextAlignment.Left,
            UseDialogueFont = false,
            TopPadding = topPadding,
            BottomPadding = bottomPadding,
            IsDivider = true
        });
    }

    private int CalculateContentHeight()
    {
        int total = 0;

        foreach (NoticeElement element in this.elements)
        {
            if (element.IsDivider)
            {
                total += element.TopPadding + 14 + element.BottomPadding;
                continue;
            }

            SpriteFont font = element.UseDialogueFont
                ? Game1.dialogueFont
                : Game1.smallFont;

            Vector2 size = font.MeasureString(element.Text) * element.Scale;

            total += (int)size.Y + element.TopPadding + element.BottomPadding;
        }

        return total;
    }

    private int GetMaxScrollOffset()
    {
        int visibleHeight = this.contentBottom - this.contentTop;
        return Math.Max(0, this.contentHeight - visibleHeight);
    }

    private int RoundMoney(double amount)
    {
        return (int)Math.Round(amount, MidpointRounding.AwayFromZero);
    }

    private string FormatGold(double amount)
    {
        return this.FormatGold(this.RoundMoney(amount));
    }

    private string FormatGold(int amount)
    {
        return $"{amount.ToString("N0", CultureInfo.InvariantCulture)}g";
    }

    private string FormatGoldValueOnly(double amount)
    {
        int rounded = this.RoundMoney(amount);
        return $"{rounded.ToString("N0", CultureInfo.InvariantCulture)}g";
    }

    private string FormatPercent(double rate)
    {
        int percent = (int)Math.Round(rate * 100, MidpointRounding.AwayFromZero);
        return $"{percent}%";
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

    private enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    private class NoticeElement
    {
        public string Text { get; set; } = "";
        public float Scale { get; set; }
        public Color Color { get; set; }
        public TextAlignment Alignment { get; set; }
        public bool UseDialogueFont { get; set; }
        public int TopPadding { get; set; }
        public int BottomPadding { get; set; }
        public bool IsDivider { get; set; }
    }

    private class BusinessMachineGroup
    {
        public int Count { get; set; }
        public int Days { get; set; }
    }
}
