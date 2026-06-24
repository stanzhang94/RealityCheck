using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealityCheck.Data;
using RealityCheck.Models;
using RealityCheck.Services;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.UI;

public class TaxNoticeMenu : IClickableMenu
{

    private readonly LedgerService ledgerService;
    private readonly TaxRecord record;
    private readonly TaxConfig taxConfig;

    private readonly List<NoticeElement> elements = new();

    private int scrollOffset = 0;
    private int contentHeight = 0;
    private int contentTop;
    private int contentBottom;

    private readonly Rectangle closeButtonBounds;
    private Rectangle signatureClickBounds = Rectangle.Empty;

    private bool isSigned = false;
    private bool showSignatureRequiredWarning = false;

    public TaxNoticeMenu(
        LedgerService ledgerService,
        TaxRecord record,
        TaxConfig? taxConfig = null
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
        this.taxConfig = taxConfig ?? ConfigService.Current.Tax;

        this.closeButtonBounds = new Rectangle(
            this.xPositionOnScreen + this.width - 70,
            this.yPositionOnScreen + 28,
            36,
            36
        );

        this.isSigned = this.ledgerService.IsTaxNoticeSigned(
            this.GetTaxNoticeId()
        );

        this.BuildDocument();
        this.contentHeight = this.CalculateContentHeight();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.closeButtonBounds.Contains(x, y))
        {
            if (this.taxConfig.RequireTaxNoticeSignature && !this.isSigned)
            {
                this.showSignatureRequiredWarning = true;
                this.contentHeight = this.CalculateContentHeight();
                this.scrollOffset = this.GetMaxScrollOffset();

                Game1.playSound("cancel");

                return;
            }

            Game1.playSound("bigDeSelect");
            Game1.exitActiveMenu();

            return;
        }

        if (!this.isSigned && this.signatureClickBounds.Contains(x, y))
        {
            this.isSigned = true;
            this.showSignatureRequiredWarning = false;

            this.ledgerService.SignTaxNotice(
                this.GetTaxNoticeId()
            );

            this.BuildDocument();
            this.contentHeight = this.CalculateContentHeight();

            Game1.playSound("coin");

            return;
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        // Emergency exit only. The close button still requires signature.
        if (key == Keys.Escape)
        {
            Game1.playSound("bigDeSelect");
            Game1.exitActiveMenu();

            return;
        }

        base.receiveKeyPress(key);
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

        this.signatureClickBounds = Rectangle.Empty;

        foreach (NoticeElement element in this.elements)
        {
            if (element.Kind == NoticeElementKind.Divider)
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

            if (element.Kind == NoticeElementKind.SignatureBlock)
            {
                int blockHeight = this.DrawSignatureBlock(
                    b,
                    contentLeft,
                    contentRight,
                    y
                );

                y += blockHeight;

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

    private int DrawSignatureBlock(
        SpriteBatch b,
        int contentLeft,
        int contentRight,
        int y
    )
    {
        Color labelColor = new Color(85, 60, 40);
        Color signatureColor = new Color(25, 25, 25);
        Color hintColor = new Color(135, 45, 45);
        Color lineColor = new Color(120, 90, 60);

        int topPadding = 0;
        int bottomPadding = 28;
        int blockCoreHeight = this.showSignatureRequiredWarning ? 82 : 58;

        float labelScale = 0.82f;
        float nameScale = 0.96f;
        float hintScale = 0.72f;
        float warningScale = 0.76f;

        float labelX = contentLeft;
        float labelY = y + topPadding;

        string label = I18n.Get("tax_notice.authorized_signature");
        Vector2 labelSize = Game1.smallFont.MeasureString(label) * labelScale;

        int lineX = contentLeft + 255;
        int lineY = (int)labelY + 28;
        int lineWidth = Math.Max(
            260,
            contentRight - lineX - 190
        );

        int lineHeight = 2;

        if (labelY + blockCoreHeight >= this.contentTop
            && labelY <= this.contentBottom)
        {
            b.DrawString(
                Game1.smallFont,
                label,
                new Vector2(labelX, labelY),
                labelColor,
                0f,
                Vector2.Zero,
                labelScale,
                SpriteEffects.None,
                0.86f
            );

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    lineX,
                    lineY,
                    lineWidth,
                    lineHeight
                ),
                lineColor
            );

            if (this.isSigned)
            {
                string signatureName = this.GetPlayerSignatureName();
                Vector2 nameSize = Game1.smallFont.MeasureString(signatureName) * nameScale;

                float nameX = lineX + (lineWidth - nameSize.X) / 2f;
                float nameY = labelY - 2;

                b.DrawString(
                    Game1.smallFont,
                    signatureName,
                    new Vector2(nameX, nameY),
                    signatureColor,
                    0f,
                    Vector2.Zero,
                    nameScale,
                    SpriteEffects.None,
                    0.87f
                );
            }
            else
            {
                string hint = I18n.Get("tax_notice.click_to_sign");
                Vector2 hintSize = Game1.smallFont.MeasureString(hint) * hintScale;

                b.DrawString(
                    Game1.smallFont,
                    hint,
                    new Vector2(
                        lineX + lineWidth + 18,
                        labelY + 10
                    ),
                    hintColor,
                    0f,
                    Vector2.Zero,
                    hintScale,
                    SpriteEffects.None,
                    0.87f
                );

                if (this.showSignatureRequiredWarning)
                {
                    string warning = I18n.Get("tax_notice.signature_required");

                    b.DrawString(
                        Game1.smallFont,
                        warning,
                        new Vector2(
                            lineX,
                            lineY + 14
                        ),
                        hintColor,
                        0f,
                        Vector2.Zero,
                        warningScale,
                        SpriteEffects.None,
                        0.87f
                    );
                }

                this.signatureClickBounds = new Rectangle(
                    lineX - 12,
                    (int)labelY - 6,
                    lineWidth + 175,
                    52
                );
            }
        }

        return topPadding + blockCoreHeight + bottomPadding;
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
            I18n.Get("ui.close_x"),
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

        string hint = I18n.Get("ui.mouse_wheel_to_scroll");
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
            I18n.Get("tax_notice.revenue_service"),
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
            I18n.Get("tax_notice.property_assessment_office"),
            0.86f,
            midBrown,
            TextAlignment.Center,
            useDialogueFont: false,
            topPadding: 0,
            bottomPadding: 12
        );

        this.AddDivider(0, 12);

        this.AddText(
            I18n.Get("tax_notice.title"),
            0.84f,
            darkBrown,
            TextAlignment.Center,
            useDialogueFont: true,
            topPadding: 8,
            bottomPadding: 10
        );

        this.AddText(
            I18n.Get("tax_notice.thank_you"),
            0.82f,
            midBrown,
            TextAlignment.Center,
            useDialogueFont: false,
            topPadding: 0,
            bottomPadding: 16
        );

        this.AddText(
            I18n.Get("tax_notice.tax_period", new { period = I18n.PeriodSameSeason(this.record.Year, this.record.Season, this.record.CoveredStartDay, this.record.CoveredEndDay) }),
            0.86f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            4
        );

        this.AddText(
            I18n.Get("tax_notice.settlement_date", new { date = I18n.Date(this.record.SettlementYear, this.record.SettlementSeason, this.record.SettlementDay) }),
            0.86f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            14
        );

        this.AddSectionHeader(I18n.Get("tax.income_tax"), darkBrown);
        this.BuildIncomeSection(darkBrown, midBrown);

        this.AddSectionHeader(I18n.Get("tax.property_tax"), darkBrown);
        this.BuildPropertySection(darkBrown, midBrown);

        this.AddSectionHeader(I18n.Get("tax.business_property_tax"), darkBrown);
        this.BuildBusinessPropertySection(darkBrown, midBrown);

        this.AddSectionHeader(I18n.Get("tax_notice.total_tax_due"), darkBrown);
        this.BuildTotalSection(darkBrown, midBrown);

        this.AddDivider(14, 14);

        this.AddText(
            I18n.Get("tax_notice.issued_by"),
            0.82f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            4
        );

        this.AddText(
            I18n.Get("tax_notice.issued_by_value"),
            0.86f,
            darkBrown,
            TextAlignment.Left,
            false,
            0,
            18
        );

        this.AddSignatureBlock();

        this.AddText(
            I18n.Get("tax_notice.official_seal"),
            0.92f,
            sealRed,
            TextAlignment.Right,
            false,
            -24,
            4
        );

        this.AddText(
            I18n.Get("tax_notice.questions_or_appeals"),
            0.78f,
            midBrown,
            TextAlignment.Left,
            false,
            12,
            4
        );

        this.AddText(
            I18n.Get("tax_notice.appeal_disclaimer"),
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
            I18n.Get("tax_notice.taxable_shipping_bin_income", new { amount = this.FormatGold(this.record.TaxableShippingBinIncome) }),
            darkBrown
        );

        this.AddBodyLine(
            I18n.Get("tax_notice.applied_tax_rate", new { rate = this.FormatPercent(this.record.IncomeTaxRate) }),
            darkBrown
        );

        this.AddBodyLine(
            I18n.Get("tax_notice.formula", new { formula = $"{this.FormatGold(this.record.TaxableShippingBinIncome)} x {this.FormatPercent(this.record.IncomeTaxRate)} = {this.FormatGold(this.record.IncomeTaxAmount)}" }),
            darkBrown
        );

        this.AddBodyLine(
            I18n.Get("tax_notice.income_tax_due", new { amount = this.FormatGold(this.record.IncomeTaxAmount) }),
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

        this.AddBodyLine(I18n.Get("tax_notice.replacement_cost", new { amount = this.FormatGold(rc) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.income_potential_value", new { amount = this.FormatGold(ipv) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.utility_premium", new { amount = this.FormatGold(up) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.risk_shield_premium", new { amount = this.FormatGold(rsp) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.depreciation_factor", new { factor = this.FormatPercent(depreciationFactor) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.agricultural_deduction", new { amount = this.FormatGoldValueOnly(ad) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.administrative_fee", new { amount = this.FormatGold(admin) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.documentation_fee", new { amount = this.FormatGold(doc) }), darkBrown);

        this.AddBodyLine(
            I18n.Get("tax_notice.formula", new { formula = $"(({this.FormatGold(rc)} + {this.FormatGold(ipv)} + {this.FormatGold(up)} + {this.FormatGold(rsp)}) x {this.FormatPercent(depreciationFactor)}) - {this.FormatGoldValueOnly(ad)} + {this.FormatGold(admin)} + {this.FormatGold(doc)} = {this.FormatGold(this.record.PropertyTaxAmount)}" }),
            darkBrown
        );

        this.AddBodyLine(
            I18n.Get("tax_notice.property_tax_due", new { amount = this.FormatGold(this.record.PropertyTaxAmount) }),
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
            I18n.Get("machine.keg"),
            assessments,
            a => a.KegCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.Keg
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.preserves_jar"),
            assessments,
            a => a.PreservesJarCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.PreservesJar
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.cask"),
            assessments,
            a => a.CaskCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.Cask
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.bee_house"),
            assessments,
            a => a.BeeHouseCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.BeeHouse
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.mayonnaise_machine"),
            assessments,
            a => a.MayonnaiseMachineCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.MayonnaiseMachine
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.cheese_press"),
            assessments,
            a => a.CheesePressCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.CheesePress
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.loom"),
            assessments,
            a => a.LoomCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.Loom
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.oil_maker"),
            assessments,
            a => a.OilMakerCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.OilMaker
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.dehydrator"),
            assessments,
            a => a.DehydratorCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.Dehydrator
        );

        this.AddBusinessMachineLine(
            businessLines,
            I18n.Get("machine.fish_smoker"),
            assessments,
            a => a.FishSmokerCount,
            this.taxConfig.BusinessPropertyDailyTaxRates.FishSmoker
        );

        if (businessLines.Count == 0)
        {
            this.AddBodyLine(
                I18n.Get("tax_notice.no_taxable_business_equipment"),
                darkBrown
            );
        }
        else
        {
            foreach (string line in businessLines)
                this.AddBodyLine(line, darkBrown);
        }

        this.AddBodyLine(
            I18n.Get("tax_notice.business_property_tax_due", new { amount = this.FormatGold(this.record.BusinessPropertyTaxAmount) }),
            darkBrown,
            bottomPadding: 12
        );
    }

    private void BuildTotalSection(Color darkBrown, Color midBrown)
    {
        this.AddBodyLine(I18n.Get("tax_notice.income_tax_total_line", new { amount = this.FormatGold(this.record.IncomeTaxAmount) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.property_tax_total_line", new { amount = this.FormatGold(this.record.PropertyTaxAmount) }), darkBrown);
        this.AddBodyLine(I18n.Get("tax_notice.business_property_tax_total_line", new { amount = this.FormatGold(this.record.BusinessPropertyTaxAmount) }), darkBrown);
        this.AddBodyLine(
            I18n.Get("tax_notice.total_line", new { amount = this.FormatGold(this.record.TotalTaxAmount) }),
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
        if (count <= this.taxConfig.BusinessPropertyTaxThreshold)
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
            Kind = NoticeElementKind.Text
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
            Kind = NoticeElementKind.Divider
        });
    }

    private void AddSignatureBlock()
    {
        this.elements.Add(new NoticeElement
        {
            Text = "",
            Scale = 1f,
            Color = Color.White,
            Alignment = TextAlignment.Left,
            UseDialogueFont = false,
            TopPadding = 0,
            BottomPadding = 0,
            Kind = NoticeElementKind.SignatureBlock
        });
    }

    private int CalculateContentHeight()
    {
        int total = 0;

        foreach (NoticeElement element in this.elements)
        {
            if (element.Kind == NoticeElementKind.Divider)
            {
                total += element.TopPadding + 14 + element.BottomPadding;
                continue;
            }

            if (element.Kind == NoticeElementKind.SignatureBlock)
            {
                total += this.GetSignatureBlockHeight();
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

    private int GetSignatureBlockHeight()
    {
        int topPadding = 0;
        int bottomPadding = 28;
        int blockCoreHeight = this.showSignatureRequiredWarning ? 82 : 58;

        return topPadding + blockCoreHeight + bottomPadding;
    }

    private int GetMaxScrollOffset()
    {
        int visibleHeight = this.contentBottom - this.contentTop;
        return Math.Max(0, this.contentHeight - visibleHeight);
    }

    private string GetTaxNoticeId()
    {
        return
            $"RC_WeeklyTaxNotice_" +
            $"Y{this.record.Year}_" +
            $"{this.record.Season}_" +
            $"{this.record.CoveredStartDay}_{this.record.CoveredEndDay}";
    }

    private string GetPlayerSignatureName()
    {
        string name = Game1.player?.Name ?? "";

        if (string.IsNullOrWhiteSpace(name))
            return "Farmer";

        return name;
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
        return I18n.Season(season);
    }

    private enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    private enum NoticeElementKind
    {
        Text,
        Divider,
        SignatureBlock
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
        public NoticeElementKind Kind { get; set; }
    }

    private class BusinessMachineGroup
    {
        public int Count { get; set; }
        public int Days { get; set; }
    }
}
