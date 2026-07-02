using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealityCheck.Models;
using RealityCheck.Services;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.UI;

public class ExchangeMenu : IClickableMenu
{
    private enum ExchangePage
    {
        Account,
        Create
    }

    private enum CommodityFilter
    {
        All,
        Artisan,
        Vegetable,
        Fruit
    }

    private const int CommodityListVisibleRows = 9;
    private const int CommodityListRowHeight = 32;
    private const int CreatePagePadding = 18;
    private const int CreatePageSectionGap = 22;

    private readonly ExchangeService exchangeService;
    private readonly ExchangeContractCatalogService? catalogService;
    private readonly List<ExchangeContractSpec> catalog;
    private readonly Dictionary<CommodityFilter, List<ExchangeContractSpec>> filteredCatalogs;

    private readonly Rectangle accountPageButton;
    private readonly Rectangle createPageButton;
    private readonly Rectangle closeButton;

    private readonly Rectangle transferInputBox;
    private readonly Rectangle depositButton;
    private readonly Rectangle withdrawButton;

    private readonly Rectangle filterAllButton;
    private readonly Rectangle filterArtisanButton;
    private readonly Rectangle filterVegetableButton;
    private readonly Rectangle filterFruitButton;
    private readonly Rectangle commodityDropdownButton;
    private readonly Rectangle commodityDropdownListBox;
    private readonly Rectangle commodityDropdownUpButton;
    private readonly Rectangle commodityDropdownDownButton;
    private readonly Rectangle longButton;
    private readonly Rectangle shortButton;
    private readonly Rectangle term7Button;
    private readonly Rectangle term14Button;
    private readonly Rectangle term28Button;
    private readonly Rectangle lotsMinusButton;
    private readonly Rectangle lotsPlusButton;
    private readonly Rectangle createPositionButton;

    private readonly Rectangle confirmButton;
    private readonly Rectangle cancelConfirmButton;

    private ExchangePage currentPage = ExchangePage.Account;
    private CommodityFilter currentFilter = CommodityFilter.All;
    private string transferAmountText = string.Empty;
    private bool transferInputSelected;
    private string accountMessage = string.Empty;
    private bool accountMessageSucceeded = true;
    private double accountMessageExpiresAt;
    private string createMessage = string.Empty;
    private bool createMessageSucceeded = true;
    private double createMessageExpiresAt;

    private int selectedCatalogIndex;
    private int commodityListScrollIndex;
    private string selectedDirection = ExchangePosition.DirectionLong;
    private int selectedTermDays = 7;
    private int selectedLots = 1;
    private bool confirmCreateOpen;
    private int createPageScrollOffset;


    private sealed class CreateLayout
    {
        public Rectangle Viewport { get; init; }
        public Rectangle FilterAll { get; init; }
        public Rectangle FilterArtisan { get; init; }
        public Rectangle FilterVegetable { get; init; }
        public Rectangle FilterFruit { get; init; }
        public Rectangle CommodityList { get; init; }
        public Rectangle CommodityUp { get; init; }
        public Rectangle CommodityDown { get; init; }
        public Rectangle LongButton { get; init; }
        public Rectangle ShortButton { get; init; }
        public Rectangle Term7Button { get; init; }
        public Rectangle Term14Button { get; init; }
        public Rectangle Term28Button { get; init; }
        public Rectangle LotsMinusButton { get; init; }
        public Rectangle LotsPlusButton { get; init; }
        public Rectangle CreateButton { get; init; }
        public Rectangle ChartBounds { get; init; }
        public int ContentHeight { get; init; }
        public int MaxScroll => Math.Max(0, this.ContentHeight - this.Viewport.Height);
    }

    private static int GetPreferredMenuWidth()
    {
        int availableWidth = Math.Max(640, Game1.uiViewport.Width - 40);
        return Math.Min(1440, availableWidth);
    }

    private static int GetPreferredMenuHeight()
    {
        int availableHeight = Math.Max(520, Game1.uiViewport.Height - 40);
        return Math.Min(900, availableHeight);
    }

    public ExchangeMenu(
        ExchangeService exchangeService,
        ExchangeContractCatalogService? catalogService = null
    )
        : base(
            (Game1.uiViewport.Width - GetPreferredMenuWidth()) / 2,
            Math.Max(12, (Game1.uiViewport.Height - GetPreferredMenuHeight()) / 2),
            GetPreferredMenuWidth(),
            GetPreferredMenuHeight(),
            false
        )
    {
        this.exchangeService = exchangeService;
        this.catalogService = catalogService;
        this.catalog = catalogService?.GetContractCatalog() ?? new List<ExchangeContractSpec>();
        this.filteredCatalogs = this.BuildFilteredCatalogCache(this.catalog);

        int tabY = this.yPositionOnScreen + 105;
        this.accountPageButton = new Rectangle(this.xPositionOnScreen + 60, tabY, 160, 48);
        this.createPageButton = new Rectangle(this.xPositionOnScreen + 235, tabY, 170, 48);
        this.closeButton = new Rectangle(this.xPositionOnScreen + this.width - 92, this.yPositionOnScreen + 38, 48, 48);

        int transferX = this.xPositionOnScreen + this.width - 390;
        int transferY = this.yPositionOnScreen + 218;
        this.transferInputBox = new Rectangle(transferX, transferY, 210, 44);
        this.depositButton = new Rectangle(transferX, transferY + 62, 140, 42);
        this.withdrawButton = new Rectangle(transferX, transferY + 118, 140, 42);

        int createBaseX = this.xPositionOnScreen + 60;
        int filterY = this.yPositionOnScreen + 206;
        this.filterAllButton = new Rectangle(createBaseX, filterY, 92, 34);
        this.filterArtisanButton = new Rectangle(createBaseX + 100, filterY, 112, 34);
        this.filterVegetableButton = new Rectangle(createBaseX + 220, filterY, 102, 34);
        this.filterFruitButton = new Rectangle(createBaseX + 330, filterY, 92, 34);

        int commodityY = this.yPositionOnScreen + 256;
        this.commodityDropdownButton = new Rectangle(createBaseX, commodityY, 380, 40);
        this.commodityDropdownListBox = new Rectangle(
            createBaseX,
            commodityY + 46,
            380,
            CommodityListVisibleRows * CommodityListRowHeight
        );
        this.commodityDropdownUpButton = new Rectangle(
            this.commodityDropdownListBox.Right - 34,
            this.commodityDropdownListBox.Y + 2,
            30,
            26
        );
        this.commodityDropdownDownButton = new Rectangle(
            this.commodityDropdownListBox.Right - 34,
            this.commodityDropdownListBox.Bottom - 28,
            30,
            26
        );

        int controlX = this.xPositionOnScreen + this.width - 360;
        int controlY = this.yPositionOnScreen + 230;
        this.longButton = new Rectangle(controlX, controlY + 36, 92, 40);
        this.shortButton = new Rectangle(controlX + 104, controlY + 36, 92, 40);

        this.term7Button = new Rectangle(controlX, controlY + 116, 64, 40);
        this.term14Button = new Rectangle(controlX + 74, controlY + 116, 72, 40);
        this.term28Button = new Rectangle(controlX + 156, controlY + 116, 72, 40);

        this.lotsMinusButton = new Rectangle(controlX, controlY + 206, 44, 40);
        this.lotsPlusButton = new Rectangle(controlX + 154, controlY + 206, 44, 40);
        this.createPositionButton = new Rectangle(controlX, controlY + 340, 250, 48);

        int confirmX = this.xPositionOnScreen + 260;
        int confirmY = this.yPositionOnScreen + 456;
        this.confirmButton = new Rectangle(confirmX, confirmY, 130, 44);
        this.cancelConfirmButton = new Rectangle(confirmX + 158, confirmY, 130, 44);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.confirmCreateOpen)
        {
            this.HandleConfirmClick(x, y);
            return;
        }

        if (this.closeButton.Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.exitActiveMenu();
            return;
        }

        if (this.accountPageButton.Contains(x, y))
        {
            this.currentPage = ExchangePage.Account;
            this.transferInputSelected = false;
            Game1.playSound("smallSelect");
            return;
        }

        if (this.createPageButton.Contains(x, y))
        {
            this.currentPage = ExchangePage.Create;
            this.transferInputSelected = false;
            this.EnsureSelectedContractIsValid();
            Game1.playSound("smallSelect");
            return;
        }

        if (this.currentPage == ExchangePage.Account)
        {
            this.HandleAccountClick(x, y);
            return;
        }

        this.transferInputSelected = false;
        this.HandleCreateClick(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (this.currentPage == ExchangePage.Create)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            CreateLayout layout = this.GetCreateLayout();

            if (layout.CommodityList.Contains(mouseX, mouseY))
            {
                List<ExchangeContractSpec> contracts = this.GetFilteredCatalog();
                int maxCommodityScroll = Math.Max(0, contracts.Count - CommodityListVisibleRows);
                if (maxCommodityScroll > 0)
                {
                    this.commodityListScrollIndex += direction < 0 ? 1 : -1;
                    this.ClampCommodityListScrollIndex(contracts.Count);
                    Game1.playSound("shiny4");
                }

                return;
            }

            if (layout.Viewport.Contains(mouseX, mouseY))
            {
                this.createPageScrollOffset += direction < 0 ? 48 : -48;
                this.ClampCreatePageScroll(layout);
                return;
            }
        }

        base.receiveScrollWheelAction(direction);
    }


    private void HandleAccountClick(int x, int y)
    {
        if (this.transferInputBox.Contains(x, y))
        {
            this.transferInputSelected = true;
            Game1.playSound("smallSelect");
            return;
        }

        this.transferInputSelected = false;

        if (this.depositButton.Contains(x, y))
        {
            this.RunTransferFromInput(deposit: true);
            return;
        }

        if (this.withdrawButton.Contains(x, y))
        {
            this.RunTransferFromInput(deposit: false);
        }
    }

    private void HandleCreateClick(int x, int y)
    {
        CreateLayout layout = this.GetCreateLayout();

        if (this.HandleFilterClick(x, y, layout))
            return;

        List<ExchangeContractSpec> contracts = this.GetFilteredCatalog();
        if (contracts.Count == 0)
            return;

        this.ClampSelectedCatalogIndex(contracts.Count);

        if (layout.CommodityUp.Contains(x, y))
        {
            this.commodityListScrollIndex--;
            this.ClampCommodityListScrollIndex(contracts.Count);
            Game1.playSound("shiny4");
            return;
        }

        if (layout.CommodityDown.Contains(x, y))
        {
            this.commodityListScrollIndex++;
            this.ClampCommodityListScrollIndex(contracts.Count);
            Game1.playSound("shiny4");
            return;
        }

        if (layout.CommodityList.Contains(x, y))
        {
            int row = (y - layout.CommodityList.Y) / CommodityListRowHeight;
            int index = this.commodityListScrollIndex + row;
            if (index >= 0 && index < contracts.Count)
            {
                this.selectedCatalogIndex = index;
                this.NormalizeSelectedTerm(contracts[this.selectedCatalogIndex]);
                Game1.playSound("smallSelect");
            }
            return;
        }

        if (!layout.Viewport.Contains(x, y))
            return;

        ExchangeContractSpec selected = contracts[this.selectedCatalogIndex];

        if (layout.LongButton.Contains(x, y))
        {
            this.selectedDirection = ExchangePosition.DirectionLong;
            Game1.playSound("smallSelect");
            return;
        }

        if (layout.ShortButton.Contains(x, y))
        {
            this.selectedDirection = ExchangePosition.DirectionShort;
            Game1.playSound("smallSelect");
            return;
        }

        if (layout.Term7Button.Contains(x, y))
        {
            this.TrySelectTerm(selected, 7);
            return;
        }

        if (layout.Term14Button.Contains(x, y))
        {
            this.TrySelectTerm(selected, 14);
            return;
        }

        if (layout.Term28Button.Contains(x, y))
        {
            this.TrySelectTerm(selected, 28);
            return;
        }

        if (layout.LotsMinusButton.Contains(x, y))
        {
            this.selectedLots = Math.Max(1, this.selectedLots - 1);
            Game1.playSound("smallSelect");
            return;
        }

        if (layout.LotsPlusButton.Contains(x, y))
        {
            this.selectedLots = Math.Min(999, this.selectedLots + 1);
            Game1.playSound("smallSelect");
            return;
        }

        if (layout.CreateButton.Contains(x, y))
        {
            this.confirmCreateOpen = true;
            Game1.playSound("smallSelect");
        }
    }


    private bool HandleFilterClick(int x, int y, CreateLayout? layout = null)
    {
        layout ??= this.GetCreateLayout();

        if (layout.FilterAll.Contains(x, y))
            return this.SelectFilter(CommodityFilter.All);

        if (layout.FilterArtisan.Contains(x, y))
            return this.SelectFilter(CommodityFilter.Artisan);

        if (layout.FilterVegetable.Contains(x, y))
            return this.SelectFilter(CommodityFilter.Vegetable);

        if (layout.FilterFruit.Contains(x, y))
            return this.SelectFilter(CommodityFilter.Fruit);

        return false;
    }

    private bool SelectFilter(CommodityFilter filter)
    {
        this.currentFilter = filter;
        this.selectedCatalogIndex = 0;
        this.commodityListScrollIndex = 0;
        this.EnsureSelectedContractIsValid();
        Game1.playSound("smallSelect");
        return true;
    }

    private void HandleConfirmClick(int x, int y)
    {
        Rectangle box = this.GetCreateConfirmationBox();
        (Rectangle confirm, Rectangle cancel) = this.GetCreateConfirmationButtons(box);

        if (confirm.Contains(x, y))
        {
            List<ExchangeContractSpec> contracts = this.GetFilteredCatalog();
            if (contracts.Count > 0)
            {
                this.ClampSelectedCatalogIndex(contracts.Count);
                this.CreateSelectedPosition(contracts[this.selectedCatalogIndex]);
            }
            this.confirmCreateOpen = false;
            return;
        }

        if (cancel.Contains(x, y))
        {
            this.confirmCreateOpen = false;
            Game1.playSound("bigDeSelect");
            return;
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (!this.transferInputSelected)
        {
            base.receiveKeyPress(key);
            return;
        }

        if (key == Keys.Back)
        {
            if (this.transferAmountText.Length > 0)
                this.transferAmountText = this.transferAmountText[..^1];

            Game1.playSound("tinyWhip");
            return;
        }

        if (key == Keys.Enter)
            return;

        int digit = KeyToDigit(key);
        if (digit >= 0 && this.transferAmountText.Length < 9)
        {
            if (this.transferAmountText == "0")
                this.transferAmountText = digit.ToString();
            else
                this.transferAmountText += digit.ToString();

            Game1.playSound("smallSelect");
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);

        IClickableMenu.drawTextureBox(
            b,
            this.xPositionOnScreen,
            this.yPositionOnScreen,
            this.width,
            this.height,
            Color.White
        );

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.title"),
            Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + 60, this.yPositionOnScreen + 55),
            Game1.textColor
        );

        this.DrawPageTabs(b);
        this.DrawButton(b, this.closeButton, I18n.Get("ui.close_x"));

        if (this.currentPage == ExchangePage.Account)
        {
            this.DrawAccountSummary(b);
            this.DrawTransferPanel(b);
        }
        else
        {
            this.DrawCreateContractPage(b);
        }

        this.DrawPageMessage(b);

        if (this.confirmCreateOpen)
            this.DrawCreateConfirmation(b);

        this.drawMouse(b);
    }

    private void DrawPageTabs(SpriteBatch b)
    {
        this.DrawButton(
            b,
            this.accountPageButton,
            this.currentPage == ExchangePage.Account ? I18n.Get("exchange.tab_account_active") : I18n.Get("exchange.tab_account"),
            selected: this.currentPage == ExchangePage.Account
        );

        this.DrawButton(
            b,
            this.createPageButton,
            this.currentPage == ExchangePage.Create ? I18n.Get("exchange.tab_create_active") : I18n.Get("exchange.tab_create"),
            selected: this.currentPage == ExchangePage.Create
        );
    }

    private void DrawAccountSummary(SpriteBatch b)
    {
        ExchangeAccount account = this.exchangeService.GetAccount();

        int x = this.xPositionOnScreen + 70;
        int y = this.yPositionOnScreen + 172;

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.account_title"),
            Game1.smallFont,
            new Vector2(x, y),
            Game1.textColor
        );

        y += 50;

        this.DrawLine(b, I18n.Get("exchange.cash_balance", new { amount = account.CashBalance }), x, y);
        y += 38;
        this.DrawLine(b, I18n.Get("exchange.locked_margin", new { amount = account.LockedMargin }), x, y);
        y += 38;
        this.DrawLine(b, I18n.Get("exchange.available_balance", new { amount = account.AvailableBalance }), x, y);
        y += 38;
        this.DrawLine(b, I18n.Get("exchange.open_positions", new { count = account.Positions.Count }), x, y);

        if (account.Debt > 0)
        {
            y += 38;
            this.DrawLine(b, I18n.Get("exchange.debt", new { amount = account.Debt }), x, y, Color.Red);
        }
    }

    private void DrawTransferPanel(SpriteBatch b)
    {
        int x = this.transferInputBox.X;
        int y = this.transferInputBox.Y - 48;

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.transfer_title"),
            Game1.smallFont,
            new Vector2(x, y),
            Game1.textColor
        );

        this.DrawInputBox(b);
        this.DrawButton(b, this.depositButton, I18n.Get("exchange.deposit"));
        this.DrawButton(b, this.withdrawButton, I18n.Get("exchange.withdraw"));
    }

    private void DrawInputBox(SpriteBatch b)
    {
        Color backgroundColor = string.IsNullOrWhiteSpace(this.transferAmountText)
            ? Color.White * 0.24f
            : Color.White * 0.38f;

        b.Draw(Game1.staminaRect, this.transferInputBox, backgroundColor);
        this.DrawRectangleOutline(
            b,
            this.transferInputBox,
            2,
            this.transferInputSelected ? Color.Goldenrod * 0.95f : Color.Black * 0.50f
        );

        string amountText = string.IsNullOrWhiteSpace(this.transferAmountText)
            ? I18n.Get("exchange.amount_placeholder")
            : this.transferAmountText + "g";

        if (this.transferInputSelected && !string.IsNullOrWhiteSpace(this.transferAmountText) && Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000 < 500)
            amountText += "|";

        Color textColor = string.IsNullOrWhiteSpace(this.transferAmountText)
            ? Game1.textColor * 0.65f
            : Game1.textColor;

        Utility.drawTextWithShadow(
            b,
            amountText,
            Game1.smallFont,
            new Vector2(this.transferInputBox.X + 14, this.transferInputBox.Y + 8),
            textColor
        );
    }

    private void DrawCreateContractPage(SpriteBatch b)
    {
        CreateLayout layout = this.GetCreateLayout();
        this.ClampCreatePageScroll(layout);

        List<ExchangeContractSpec> contracts = this.GetFilteredCatalog();
        int topY = layout.Viewport.Y + CreatePagePadding - this.createPageScrollOffset;

        this.DrawLineIfVisible(
            b,
            I18n.Get("exchange.create_title"),
            layout.Viewport.X + CreatePagePadding,
            topY,
            layout.Viewport,
            Game1.textColor
        );

        this.DrawFilterButtons(b, layout);

        if (contracts.Count == 0)
        {
            string message = this.catalogService is null
                ? I18n.Get("exchange.create_source_missing")
                : I18n.Get("exchange.create_no_commodities");

            this.DrawLineIfVisible(b, message, layout.Viewport.X + CreatePagePadding, topY + 118, layout.Viewport, Game1.textColor);
            this.DrawCreateScrollHint(b, layout);
            return;
        }

        this.ClampSelectedCatalogIndex(contracts.Count);
        this.ClampCommodityListScrollIndex(contracts.Count);

        ExchangeContractSpec selected = contracts[this.selectedCatalogIndex];
        this.NormalizeSelectedTerm(selected);

        int totalQuantity = selected.QuantityPerLot * this.selectedLots;
        int value = selected.MarketUnitPrice * totalQuantity;
        int initialMargin = (int)Math.Ceiling(value * ExchangeService.InitialMarginRate);
        int maintenanceMargin = (int)Math.Ceiling(value * ExchangeService.MaintenanceMarginRate);
        int liquidationPrice = CalculateLiquidationPrice(selected.MarketUnitPrice, this.selectedDirection);
        string expiryDate = this.FormatExpiryDate(this.selectedTermDays);

        // Header row: commodity label, selected commodity name, and direction label share one baseline.
        int headerY = layout.CommodityList.Y - 34;

        this.DrawLabelIfVisible(b, I18n.Get("exchange.create_commodity_label"), layout.CommodityList.X, headerY, layout.Viewport);

        int detailX = layout.CommodityList.Right + 34;
        int detailValueX = detailX + 190;
        this.DrawLineIfVisible(b, selected.DisplayName, detailX, headerY, layout.Viewport, Game1.textColor);
        this.DrawLineIfVisible(b, I18n.Get("exchange.create_direction"), layout.LongButton.X, headerY, layout.Viewport, Game1.textColor);

        this.DrawCommodityList(b, contracts, layout);

        int infoY = layout.CommodityList.Y + 2;
        const int rowHeight = 26;

        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_price"), I18n.Get("exchange.value_gold", new { amount = selected.MarketUnitPrice }), detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_lot_size"), selected.QuantityPerLot.ToString(), detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_total_quantity"), totalQuantity.ToString(), detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_contract_value"), I18n.Get("exchange.value_gold", new { amount = value }), detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight + 6;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_initial_margin_rate"), "20%", detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_initial_margin"), I18n.Get("exchange.value_gold", new { amount = initialMargin }), detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_maintenance_margin_rate"), "12%", detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_maintenance_margin"), I18n.Get("exchange.value_gold", new { amount = maintenanceMargin }), detailX, detailValueX, infoY, layout.Viewport);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_liquidation_price"), I18n.Get("exchange.value_gold", new { amount = liquidationPrice }), detailX, detailValueX, infoY, layout.Viewport, Color.Red);
        infoY += rowHeight;
        this.DrawLabelValueIfVisible(b, I18n.Get("exchange.label_expiry"), expiryDate, detailX, detailValueX, infoY, layout.Viewport);

        this.DrawButtonIfVisible(b, layout.LongButton, I18n.Get("exchange.long"), layout.Viewport, selected: this.selectedDirection == ExchangePosition.DirectionLong);
        this.DrawButtonIfVisible(b, layout.ShortButton, I18n.Get("exchange.short"), layout.Viewport, selected: this.selectedDirection == ExchangePosition.DirectionShort);

        this.DrawLineIfVisible(b, I18n.Get("exchange.create_term"), layout.Term7Button.X, layout.Term7Button.Y - 30, layout.Viewport, Game1.textColor);
        this.DrawButtonIfVisible(b, layout.Term7Button, I18n.Get("exchange.term_7"), layout.Viewport, selected: this.selectedTermDays == 7, enabled: selected.SupportsTerm(7));
        this.DrawButtonIfVisible(b, layout.Term14Button, I18n.Get("exchange.term_14"), layout.Viewport, selected: this.selectedTermDays == 14, enabled: selected.SupportsTerm(14));
        this.DrawButtonIfVisible(b, layout.Term28Button, I18n.Get("exchange.term_28"), layout.Viewport, selected: this.selectedTermDays == 28, enabled: selected.SupportsTerm(28));

        this.DrawLineIfVisible(b, I18n.Get("exchange.create_lots_label"), layout.LotsMinusButton.X, layout.LotsMinusButton.Y - 30, layout.Viewport, Game1.textColor);
        this.DrawButtonIfVisible(b, layout.LotsMinusButton, "-", layout.Viewport);
        Rectangle lotsValueBox = new(layout.LotsMinusButton.Right + 12, layout.LotsMinusButton.Y, 74, layout.LotsMinusButton.Height);
        if (this.IsVisible(lotsValueBox, layout.Viewport))
            this.DrawCenteredText(b, this.selectedLots.ToString(), lotsValueBox);
        this.DrawButtonIfVisible(b, layout.LotsPlusButton, "+", layout.Viewport);

        this.DrawLineIfVisible(b, I18n.Get("exchange.create_available", new { amount = this.exchangeService.GetAccount().AvailableBalance }), layout.CreateButton.X, layout.CreateButton.Y - 30, layout.Viewport, Game1.textColor);
        this.DrawButtonIfVisible(b, layout.CreateButton, I18n.Get("exchange.create_position"), layout.Viewport);

        this.DrawDetailedPriceHistory(b, selected, liquidationPrice, layout.ChartBounds, layout.Viewport);
        this.DrawCreateScrollHint(b, layout);
    }


    private void DrawFilterButtons(SpriteBatch b, CreateLayout layout)
    {
        int allCount = this.GetFilteredCatalog(CommodityFilter.All).Count;
        int artisanCount = this.GetFilteredCatalog(CommodityFilter.Artisan).Count;
        int vegetableCount = this.GetFilteredCatalog(CommodityFilter.Vegetable).Count;
        int fruitCount = this.GetFilteredCatalog(CommodityFilter.Fruit).Count;

        this.DrawButtonIfVisible(b, layout.FilterAll, $"{I18n.Get("exchange.filter_all")} {allCount}", layout.Viewport, selected: this.currentFilter == CommodityFilter.All);
        this.DrawButtonIfVisible(b, layout.FilterArtisan, $"{I18n.Get("exchange.filter_artisan")} {artisanCount}", layout.Viewport, selected: this.currentFilter == CommodityFilter.Artisan);
        this.DrawButtonIfVisible(b, layout.FilterVegetable, $"{I18n.Get("exchange.filter_vegetable")} {vegetableCount}", layout.Viewport, selected: this.currentFilter == CommodityFilter.Vegetable);
        this.DrawButtonIfVisible(b, layout.FilterFruit, $"{I18n.Get("exchange.filter_fruit")} {fruitCount}", layout.Viewport, selected: this.currentFilter == CommodityFilter.Fruit);
    }


    private void DrawCommodityList(SpriteBatch b, List<ExchangeContractSpec> contracts, CreateLayout layout)
    {
        Rectangle box = layout.CommodityList;
        if (!this.IsPartiallyVisible(box, layout.Viewport))
            return;

        this.ClampCommodityListScrollIndex(contracts.Count);

        int visibleRows = Math.Min(CommodityListVisibleRows, contracts.Count);
        for (int i = 0; i < visibleRows; i++)
        {
            int index = this.commodityListScrollIndex + i;
            if (index < 0 || index >= contracts.Count)
                continue;

            Rectangle row = new(box.X, box.Y + i * CommodityListRowHeight + 3, box.Width - 38, CommodityListRowHeight - 6);
            if (!this.IsVisible(row, layout.Viewport))
                continue;

            bool selected = index == this.selectedCatalogIndex;
            if (selected)
            {
                b.Draw(Game1.staminaRect, row, Color.Goldenrod * 0.28f);
                b.Draw(Game1.staminaRect, new Rectangle(row.X, row.Bottom - 2, row.Width, 2), Color.SaddleBrown * 0.55f);
            }

            string label = contracts[index].DisplayName;
            Utility.drawTextWithShadow(
                b,
                label,
                Game1.smallFont,
                new Vector2(row.X + 8, row.Y + 3),
                selected ? Game1.textColor : Game1.textColor * 0.82f
            );
        }

        if (contracts.Count > CommodityListVisibleRows)
        {
            int maxScroll = Math.Max(0, contracts.Count - CommodityListVisibleRows);
            Rectangle track = new(box.Right - 12, box.Y + 4, 6, box.Height - 8);
            if (this.IsVisible(track, layout.Viewport))
            {
                b.Draw(Game1.staminaRect, track, Color.Black * 0.12f);
                int thumbHeight = Math.Max(28, (int)(track.Height * (CommodityListVisibleRows / (float)contracts.Count)));
                int thumbTravel = Math.Max(1, track.Height - thumbHeight);
                int thumbY = track.Y + (int)(thumbTravel * (this.commodityListScrollIndex / (float)maxScroll));
                b.Draw(Game1.staminaRect, new Rectangle(track.X, thumbY, track.Width, thumbHeight), Color.SaddleBrown * 0.45f);
            }
        }
    }


    private void DrawCreateConfirmation(SpriteBatch b)
    {
        List<ExchangeContractSpec> contracts = this.GetFilteredCatalog();
        if (contracts.Count == 0)
            return;

        this.ClampSelectedCatalogIndex(contracts.Count);
        ExchangeContractSpec selected = contracts[this.selectedCatalogIndex];

        int totalQuantity = selected.QuantityPerLot * this.selectedLots;
        int value = selected.MarketUnitPrice * totalQuantity;
        int initialMargin = (int)Math.Ceiling(value * ExchangeService.InitialMarginRate);
        int maintenanceMargin = (int)Math.Ceiling(value * ExchangeService.MaintenanceMarginRate);
        int liquidationPrice = CalculateLiquidationPrice(selected.MarketUnitPrice, this.selectedDirection);

        b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.45f);

        Rectangle box = this.GetCreateConfirmationBox();
        IClickableMenu.drawTextureBox(b, box.X, box.Y, box.Width, box.Height, Color.White);

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.confirm_title"),
            Game1.dialogueFont,
            new Vector2(box.X + 38, box.Y + 28),
            Game1.textColor
        );

        int labelX = box.X + 50;
        int valueX = box.X + 280;
        int y = box.Y + 92;
        const int rowHeight = 27;

        this.DrawLabelValue(b, I18n.Get("exchange.confirm_contract"), selected.DisplayName, labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.confirm_direction"), this.selectedDirection == ExchangePosition.DirectionLong ? I18n.Get("exchange.long") : I18n.Get("exchange.short"), labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.confirm_term"), I18n.Get("exchange.value_days", new { days = this.selectedTermDays }), labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.confirm_quantity"), totalQuantity.ToString(), labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_price"), I18n.Get("exchange.value_gold", new { amount = selected.MarketUnitPrice }), labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_contract_value"), I18n.Get("exchange.value_gold", new { amount = value }), labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_initial_margin_rate"), "20%", labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_initial_margin"), I18n.Get("exchange.value_gold", new { amount = initialMargin }), labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_maintenance_margin_rate"), "12%", labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_maintenance_margin"), I18n.Get("exchange.value_gold", new { amount = maintenanceMargin }), labelX, valueX, y);
        y += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_liquidation_price"), I18n.Get("exchange.value_gold", new { amount = liquidationPrice }), labelX, valueX, y, Color.Red);

        (Rectangle confirm, Rectangle cancel) = this.GetCreateConfirmationButtons(box);
        this.DrawButton(b, confirm, I18n.Get("exchange.confirm_create"));
        this.DrawButton(b, cancel, I18n.Get("exchange.confirm_cancel"));
    }

    private Rectangle GetCreateConfirmationBox()
    {
        int boxWidth = Math.Min(760, Math.Max(620, Game1.uiViewport.Width - 180));
        int boxHeight = Math.Min(560, Math.Max(500, Game1.uiViewport.Height - 120));
        return new Rectangle(
            (Game1.uiViewport.Width - boxWidth) / 2,
            (Game1.uiViewport.Height - boxHeight) / 2,
            boxWidth,
            boxHeight
        );
    }

    private (Rectangle Confirm, Rectangle Cancel) GetCreateConfirmationButtons(Rectangle box)
    {
        int buttonWidth = 150;
        int buttonHeight = 44;
        int gap = 26;
        int y = box.Bottom - 72;
        int x = box.X + (box.Width - buttonWidth * 2 - gap) / 2;

        return (
            new Rectangle(x, y, buttonWidth, buttonHeight),
            new Rectangle(x + buttonWidth + gap, y, buttonWidth, buttonHeight)
        );
    }

    private void DrawPageMessage(SpriteBatch b)
    {
        this.ExpireMessages();

        if (this.currentPage == ExchangePage.Account)
        {
            if (string.IsNullOrWhiteSpace(this.accountMessage))
                return;

            Utility.drawTextWithShadow(
                b,
                this.accountMessage,
                Game1.smallFont,
                new Vector2(this.transferInputBox.X, this.withdrawButton.Bottom + 20),
                this.accountMessageSucceeded ? Game1.textColor : Color.Red
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(this.createMessage))
            return;

        CreateLayout layout = this.GetCreateLayout();
        Rectangle messageBounds = new(
            Math.Max(layout.Viewport.X + 20, layout.Viewport.Right - 430),
            layout.CreateButton.Bottom + 12,
            400,
            62
        );
        if (!this.IsPartiallyVisible(messageBounds, layout.Viewport))
            return;

        this.DrawWrappedTextRightAligned(
            b,
            this.createMessage,
            messageBounds,
            this.createMessageSucceeded ? Game1.textColor : Color.Red,
            layout.Viewport
        );
    }

    private void SetAccountMessage(string message, bool succeeded)
    {
        this.accountMessage = message;
        this.accountMessageSucceeded = succeeded;
        this.accountMessageExpiresAt = this.GetUiTimeMilliseconds() + (succeeded ? 2600 : 3800);
    }

    private void SetCreateMessage(string message, bool succeeded)
    {
        this.createMessage = message;
        this.createMessageSucceeded = succeeded;
        this.createMessageExpiresAt = this.GetUiTimeMilliseconds() + (succeeded ? 2600 : 3800);
    }

    private void ExpireMessages()
    {
        double now = this.GetUiTimeMilliseconds();
        if (!string.IsNullOrWhiteSpace(this.accountMessage) && now >= this.accountMessageExpiresAt)
            this.accountMessage = string.Empty;

        if (!string.IsNullOrWhiteSpace(this.createMessage) && now >= this.createMessageExpiresAt)
            this.createMessage = string.Empty;
    }

    private double GetUiTimeMilliseconds()
    {
        return Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0d;
    }

    private void DrawButton(SpriteBatch b, Rectangle bounds, string label, bool selected = false, bool enabled = true)
    {
        Color backgroundColor = selected
            ? Color.Wheat * 0.60f
            : Color.White * (enabled ? 0.30f : 0.14f);

        b.Draw(Game1.staminaRect, bounds, backgroundColor);
        this.DrawRectangleOutline(
            b,
            bounds,
            selected ? 3 : 2,
            enabled ? Color.Black * 0.45f : Color.Gray * 0.35f
        );

        this.DrawCenteredText(b, label, bounds, enabled ? Game1.textColor : Color.Gray);
    }

    private void DrawCenteredText(SpriteBatch b, string label, Rectangle bounds)
    {
        this.DrawCenteredText(b, label, bounds, Game1.textColor);
    }

    private void DrawCenteredText(SpriteBatch b, string label, Rectangle bounds, Color color)
    {
        Vector2 size = Game1.smallFont.MeasureString(label);
        Utility.drawTextWithShadow(
            b,
            label,
            Game1.smallFont,
            new Vector2(
                bounds.X + (bounds.Width - size.X) / 2f,
                bounds.Y + (bounds.Height - size.Y) / 2f + 2f
            ),
            color
        );
    }

    private void DrawLabel(SpriteBatch b, string text, int x, int y)
    {
        Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(x, y), Game1.textColor);
    }

    private void DrawLabelValue(SpriteBatch b, string label, string value, int labelX, int valueX, int y)
    {
        this.DrawLabelValue(b, label, value, labelX, valueX, y, Game1.textColor);
    }

    private void DrawLabelValue(SpriteBatch b, string label, string value, int labelX, int valueX, int y, Color valueColor)
    {
        Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(labelX, y), Game1.textColor * 0.78f);
        Utility.drawTextWithShadow(b, value, Game1.smallFont, new Vector2(valueX, y), valueColor);
    }

    private void DrawLine(SpriteBatch b, string text, int x, int y)
    {
        this.DrawLine(b, text, x, y, Game1.textColor);
    }

    private void DrawLine(SpriteBatch b, string text, int x, int y, Color color)
    {
        Utility.drawTextWithShadow(
            b,
            text,
            Game1.smallFont,
            new Vector2(x, y),
            color
        );
    }

    private void DrawRectangleOutline(SpriteBatch b, Rectangle bounds, int thickness, Color color)
    {
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }

    private void RunTransferFromInput(bool deposit)
    {
        if (!this.TryGetTransferAmount(out int amount, out string error))
        {
            this.SetAccountMessage(error, succeeded: false);
            Game1.playSound("cancel");
            return;
        }

        string message;
        bool ok = deposit
            ? this.exchangeService.TryDeposit(amount, out message)
            : this.exchangeService.TryWithdraw(amount, out message);

        this.SetAccountMessage(message, ok);
        if (ok)
        {
            this.transferAmountText = string.Empty;
            this.transferInputSelected = false;
        }
        Game1.playSound(ok ? "coin" : "cancel");
    }

    private bool TryGetTransferAmount(out int amount, out string error)
    {
        amount = 0;
        error = string.Empty;

        string raw = this.transferAmountText.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            error = I18n.Get("exchange.error_enter_amount");
            return false;
        }

        if (!int.TryParse(raw, out amount) || amount <= 0)
        {
            amount = 0;
            error = I18n.Get("exchange.error_invalid_amount");
            return false;
        }

        return true;
    }

    private void CreateSelectedPosition(ExchangeContractSpec selected)
    {
        bool ok = this.exchangeService.TryOpenPosition(
            selected,
            this.selectedDirection,
            this.selectedTermDays,
            this.selectedLots,
            out _,
            out string message
        );

        if (ok)
        {
            this.SetAccountMessage(message, succeeded: true);
            this.currentPage = ExchangePage.Account;
        }
        else
        {
            this.SetCreateMessage(message, succeeded: false);
        }

        Game1.playSound(ok ? "coin" : "cancel");
    }

    private void TrySelectTerm(ExchangeContractSpec selected, int termDays)
    {
        if (!selected.SupportsTerm(termDays))
        {
            this.SetCreateMessage(I18n.Get("exchange.error_unsupported_term"), succeeded: false);
            Game1.playSound("cancel");
            return;
        }

        this.selectedTermDays = termDays;
        this.createMessage = string.Empty;
        Game1.playSound("smallSelect");
    }

    private void EnsureSelectedContractIsValid()
    {
        List<ExchangeContractSpec> contracts = this.GetFilteredCatalog();
        if (contracts.Count == 0)
            return;

        this.ClampSelectedCatalogIndex(contracts.Count);
        this.NormalizeSelectedTerm(contracts[this.selectedCatalogIndex]);
        this.EnsureDropdownSelectionVisible(contracts.Count);
    }

    private void EnsureDropdownSelectionVisible(int count)
    {
        if (count <= 0)
        {
            this.commodityListScrollIndex = 0;
            return;
        }

        int maxScroll = Math.Max(0, count - CommodityListVisibleRows);
        if (this.selectedCatalogIndex < this.commodityListScrollIndex)
            this.commodityListScrollIndex = this.selectedCatalogIndex;
        else if (this.selectedCatalogIndex >= this.commodityListScrollIndex + CommodityListVisibleRows)
            this.commodityListScrollIndex = this.selectedCatalogIndex - CommodityListVisibleRows + 1;

        this.commodityListScrollIndex = Math.Clamp(this.commodityListScrollIndex, 0, maxScroll);
    }

    private void ClampSelectedCatalogIndex(int count)
    {
        if (count <= 0)
        {
            this.selectedCatalogIndex = 0;
            return;
        }

        if (this.selectedCatalogIndex < 0)
            this.selectedCatalogIndex = 0;

        if (this.selectedCatalogIndex >= count)
            this.selectedCatalogIndex = count - 1;
    }

    private void NormalizeSelectedTerm(ExchangeContractSpec selected)
    {
        if (selected.SupportsTerm(this.selectedTermDays))
            return;

        int fallback = selected.GetFirstSupportedTerm();
        if (fallback > 0)
            this.selectedTermDays = fallback;
    }

    private Dictionary<CommodityFilter, List<ExchangeContractSpec>> BuildFilteredCatalogCache(List<ExchangeContractSpec> source)
    {
        List<ExchangeContractSpec> all = source.ToList();

        return new Dictionary<CommodityFilter, List<ExchangeContractSpec>>
        {
            [CommodityFilter.All] = all,
            [CommodityFilter.Artisan] = all.Where(c => c.Category is "ArtisanGoods" or "FlavoredArtisan").ToList(),
            [CommodityFilter.Vegetable] = all.Where(c => string.Equals(c.Category, "Vegetable", StringComparison.OrdinalIgnoreCase)).ToList(),
            [CommodityFilter.Fruit] = all.Where(c => string.Equals(c.Category, "Fruit", StringComparison.OrdinalIgnoreCase)).ToList()
        };
    }

    private List<ExchangeContractSpec> GetCatalog()
    {
        return this.catalog;
    }

    private List<ExchangeContractSpec> GetFilteredCatalog()
    {
        return this.GetFilteredCatalog(this.currentFilter);
    }

    private List<ExchangeContractSpec> GetFilteredCatalog(CommodityFilter filter)
    {
        return this.filteredCatalogs.TryGetValue(filter, out List<ExchangeContractSpec>? list)
            ? list
            : this.catalog;
    }

    private void ClampCommodityListScrollIndex(int count)
    {
        int maxScroll = Math.Max(0, count - CommodityListVisibleRows);
        this.commodityListScrollIndex = Math.Clamp(this.commodityListScrollIndex, 0, maxScroll);
    }

    private void DrawDetailedPriceHistory(SpriteBatch b, ExchangeContractSpec selected, int liquidationPrice, Rectangle outerBounds, Rectangle viewport)
    {
        Rectangle clip = IntersectRectangles(outerBounds, viewport);
        if (clip.Width <= 0 || clip.Height <= 0)
            return;

        if (outerBounds.Y - 32 >= viewport.Y && outerBounds.Y - 32 <= viewport.Bottom - 30)
        {
            Utility.drawTextWithShadow(
                b,
                I18n.Get("exchange.history_title"),
                Game1.smallFont,
                new Vector2(outerBounds.X, outerBounds.Y - 32),
                Game1.textColor
            );
        }

        Rectangle chart = new(outerBounds.X + 70, outerBounds.Y + 8, outerBounds.Width - 88, outerBounds.Height - 52);
        Rectangle chartClip = IntersectRectangles(chart, viewport);
        if (chartClip.Width <= 0 || chartClip.Height <= 0)
            return;

        b.Draw(Game1.staminaRect, chartClip, Color.White * 0.14f);
        this.DrawRectangleOutlineClipped(b, chart, 1, Color.Black * 0.28f, viewport);

        if (this.catalogService is null)
        {
            this.DrawLineIfVisible(b, I18n.Get("exchange.history_no_data"), chart.X + 18, chart.Y + 90, viewport, Game1.textColor * 0.65f);
            return;
        }

        List<MarketPriceHistoryPoint> points = this.catalogService
            .GetMarketPriceHistory(selected.MarketCommodityKey)
            .OrderBy(p => p.DateIndex)
            .ToList();

        if (points.Count < 2)
        {
            this.DrawLineIfVisible(b, I18n.Get("exchange.history_no_data"), chart.X + 18, chart.Y + 90, viewport, Game1.textColor * 0.65f);
            return;
        }

        int min = Math.Max(0, Math.Min(points.Min(p => p.MarketUnitPrice), liquidationPrice));
        int max = Math.Max(1, Math.Max(points.Max(p => p.MarketUnitPrice), liquidationPrice));
        if (max <= min)
            max = min + 1;

        int tickCount = 6;
        for (int i = 0; i < tickCount; i++)
        {
            float t = i / (float)(tickCount - 1);
            int price = (int)Math.Round(max - (max - min) * t, MidpointRounding.AwayFromZero);
            int y = chart.Y + 8 + (int)((chart.Height - 16) * t);

            if (y >= viewport.Y && y <= viewport.Bottom)
            {
                int x1 = Math.Max(chart.X, viewport.X);
                int x2 = Math.Min(chart.Right, viewport.Right);
                if (x2 > x1)
                    b.Draw(Game1.staminaRect, new Rectangle(x1, y, x2 - x1, 1), Color.Black * 0.14f);

                Utility.drawTextWithShadow(
                    b,
                    price + "g",
                    Game1.smallFont,
                    new Vector2(outerBounds.X + 4, y - 12),
                    Game1.textColor * 0.70f
                );
            }
        }

        float liquidationY = chart.Bottom - 8 - (chart.Height - 16) * ((liquidationPrice - min) / (float)(max - min));
        if (liquidationY >= chart.Y && liquidationY <= chart.Bottom)
        {
            this.DrawClippedPixelLine(
                b,
                new Vector2(chart.X, liquidationY),
                new Vector2(chart.Right, liquidationY),
                Color.Red * 0.72f,
                2f,
                chartClip
            );
            this.DrawLineIfVisible(
                b,
                I18n.Get("exchange.history_liquidation_line"),
                chart.Right - 96,
                (int)liquidationY - 22,
                viewport,
                Color.Red
            );
        }

        Vector2? previous = null;
        for (int i = 0; i < points.Count; i++)
        {
            float px = chart.X + 10 + (chart.Width - 20) * (points.Count == 1 ? 0f : i / (float)(points.Count - 1));
            float normalized = (points[i].MarketUnitPrice - min) / (float)(max - min);
            float py = chart.Bottom - 8 - (chart.Height - 16) * normalized;
            Vector2 currentPoint = new(px, py);

            if (previous.HasValue)
                this.DrawClippedPixelLine(b, previous.Value, currentPoint, Color.Black * 0.70f, 2f, chartClip);

            if (chartClip.Contains((int)px, (int)py))
                b.Draw(Game1.staminaRect, new Rectangle((int)px - 2, (int)py - 2, 4, 4), Color.Black * 0.75f);

            previous = currentPoint;
        }

        int labelCount = Math.Min(5, points.Count);
        for (int i = 0; i < labelCount; i++)
        {
            int pointIndex = labelCount == 1 ? 0 : (int)Math.Round(i * (points.Count - 1) / (float)(labelCount - 1));
            MarketPriceHistoryPoint point = points[pointIndex];
            float px = chart.X + 10 + (chart.Width - 20) * (points.Count == 1 ? 0f : pointIndex / (float)(points.Count - 1));
            string date = I18n.Date(point.Year, point.Season, point.Day);
            Vector2 size = Game1.smallFont.MeasureString(date);
            int labelY = chart.Bottom + 10;
            if (labelY >= viewport.Y && labelY <= viewport.Bottom - 24)
            {
                Utility.drawTextWithShadow(
                    b,
                    date,
                    Game1.smallFont,
                    new Vector2(px - size.X / 2f, labelY),
                    Game1.textColor * 0.68f
                );
            }
        }
    }

    private CreateLayout GetCreateLayout()
    {
        Rectangle viewport = new(
            this.xPositionOnScreen + 58,
            this.yPositionOnScreen + 166,
            this.width - 116,
            this.height - 218
        );

        int contentX = viewport.X + CreatePagePadding;
        int contentY = viewport.Y + CreatePagePadding - this.createPageScrollOffset;

        int titleY = contentY;
        int filterY = titleY + 52;

        Rectangle filterAll = new(contentX, filterY, 104, 34);
        Rectangle filterArtisan = new(filterAll.Right + 10, filterY, 128, 34);
        Rectangle filterVegetable = new(filterArtisan.Right + 10, filterY, 112, 34);
        Rectangle filterFruit = new(filterVegetable.Right + 10, filterY, 104, 34);

        int headerY = filterAll.Bottom + 30;
        int listY = headerY + 32;

        int rightColumnWidth = 250;
        int rightColumnX = viewport.Right - CreatePagePadding - rightColumnWidth;
        int listWidth = Math.Min(390, Math.Max(300, (viewport.Width - CreatePagePadding * 2 - rightColumnWidth - 60) / 3));
        int detailX = contentX + listWidth + 34;
        int detailRight = rightColumnX - 28;
        int detailWidth = Math.Max(260, detailRight - detailX);

        Rectangle commodityList = new(contentX, listY, listWidth, CommodityListVisibleRows * CommodityListRowHeight);
        Rectangle commodityUp = new(commodityList.Right - 34, commodityList.Y + 2, 30, 26);
        Rectangle commodityDown = new(commodityList.Right - 34, commodityList.Bottom - 28, 30, 26);

        int controlY = listY + 30;
        Rectangle longButton = new(rightColumnX, controlY, 92, 38);
        Rectangle shortButton = new(rightColumnX + 104, controlY, 92, 38);

        Rectangle term7 = new(rightColumnX, listY + 94, 64, 38);
        Rectangle term14 = new(term7.Right + 10, term7.Y, 72, 38);
        Rectangle term28 = new(term14.Right + 10, term7.Y, 72, 38);

        Rectangle lotsMinus = new(rightColumnX, listY + 158, 44, 38);
        Rectangle lotsPlus = new(rightColumnX + 154, lotsMinus.Y, 44, 38);
        Rectangle createButton = new(rightColumnX, listY + 236, rightColumnWidth, 44);

        int detailBlockBottom = listY + CommodityListVisibleRows * CommodityListRowHeight + 8;
        int upperBlockBottom = Math.Max(commodityList.Bottom, Math.Max(detailBlockBottom, createButton.Bottom));

        int chartY = upperBlockBottom + 86;
        Rectangle chart = new(contentX, chartY, viewport.Width - CreatePagePadding * 2, 340);

        int contentHeight = chart.Bottom + this.createPageScrollOffset - viewport.Y + CreatePagePadding;

        return new CreateLayout
        {
            Viewport = viewport,
            FilterAll = filterAll,
            FilterArtisan = filterArtisan,
            FilterVegetable = filterVegetable,
            FilterFruit = filterFruit,
            CommodityList = commodityList,
            CommodityUp = commodityUp,
            CommodityDown = commodityDown,
            LongButton = longButton,
            ShortButton = shortButton,
            Term7Button = term7,
            Term14Button = term14,
            Term28Button = term28,
            LotsMinusButton = lotsMinus,
            LotsPlusButton = lotsPlus,
            CreateButton = createButton,
            ChartBounds = chart,
            ContentHeight = contentHeight
        };
    }


    private void ClampCreatePageScroll(CreateLayout? layout = null)
    {
        layout ??= this.GetCreateLayout();
        this.createPageScrollOffset = Math.Clamp(this.createPageScrollOffset, 0, layout.MaxScroll);
    }

    private bool IsVisible(Rectangle bounds, Rectangle viewport)
    {
        return bounds.Y >= viewport.Y && bounds.Bottom <= viewport.Bottom;
    }

    private bool IsPartiallyVisible(Rectangle bounds, Rectangle viewport)
    {
        return bounds.Bottom >= viewport.Y && bounds.Y <= viewport.Bottom;
    }

    private void DrawButtonIfVisible(SpriteBatch b, Rectangle bounds, string label, Rectangle viewport, bool selected = false, bool enabled = true)
    {
        if (!this.IsVisible(bounds, viewport))
            return;

        this.DrawButton(b, bounds, label, selected, enabled);
    }

    private void DrawLineIfVisible(SpriteBatch b, string text, int x, int y, Rectangle viewport, Color color)
    {
        if (y < viewport.Y + 2 || y > viewport.Bottom - 30)
            return;

        this.DrawLine(b, text, x, y, color);
    }

    private void DrawLabelIfVisible(SpriteBatch b, string text, int x, int y, Rectangle viewport)
    {
        if (y < viewport.Y + 2 || y > viewport.Bottom - 30)
            return;

        this.DrawLabel(b, text, x, y);
    }

    private void DrawLabelValueIfVisible(SpriteBatch b, string label, string value, int labelX, int valueX, int y, Rectangle viewport)
    {
        this.DrawLabelValueIfVisible(b, label, value, labelX, valueX, y, viewport, Game1.textColor);
    }

    private void DrawLabelValueIfVisible(SpriteBatch b, string label, string value, int labelX, int valueX, int y, Rectangle viewport, Color valueColor)
    {
        if (y < viewport.Y + 2 || y > viewport.Bottom - 30)
            return;

        this.DrawLabelValue(b, label, value, labelX, valueX, y, valueColor);
    }

    private void DrawCreateScrollHint(SpriteBatch b, CreateLayout layout)
    {
        if (layout.MaxScroll <= 0)
            return;

        Rectangle track = new(layout.Viewport.Right - 10, layout.Viewport.Y + 6, 6, layout.Viewport.Height - 12);
        b.Draw(Game1.staminaRect, track, Color.Black * 0.16f);
        int thumbHeight = Math.Max(36, (int)(track.Height * (layout.Viewport.Height / (float)layout.ContentHeight)));
        int thumbTravel = Math.Max(1, track.Height - thumbHeight);
        int thumbY = track.Y + (int)(thumbTravel * (this.createPageScrollOffset / (float)layout.MaxScroll));
        b.Draw(Game1.staminaRect, new Rectangle(track.X, thumbY, track.Width, thumbHeight), Color.SaddleBrown * 0.55f);
    }

    private void DrawPixelLine(SpriteBatch b, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        b.Draw(
            Game1.staminaRect,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), Math.Max(1, (int)thickness)),
            null,
            color,
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0f
        );
    }

    private static Rectangle IntersectRectangles(Rectangle a, Rectangle b)
    {
        int x = Math.Max(a.X, b.X);
        int y = Math.Max(a.Y, b.Y);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);

        return right <= x || bottom <= y
            ? Rectangle.Empty
            : new Rectangle(x, y, right - x, bottom - y);
    }

    private void DrawRectangleOutlineClipped(SpriteBatch b, Rectangle bounds, int thickness, Color color, Rectangle clip)
    {
        this.DrawFilledRectangleClipped(b, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color, clip);
        this.DrawFilledRectangleClipped(b, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color, clip);
        this.DrawFilledRectangleClipped(b, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color, clip);
        this.DrawFilledRectangleClipped(b, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color, clip);
    }

    private void DrawFilledRectangleClipped(SpriteBatch b, Rectangle bounds, Color color, Rectangle clip)
    {
        Rectangle visible = IntersectRectangles(bounds, clip);
        if (visible.Width > 0 && visible.Height > 0)
            b.Draw(Game1.staminaRect, visible, color);
    }

    private void DrawClippedPixelLine(SpriteBatch b, Vector2 start, Vector2 end, Color color, float thickness, Rectangle clip)
    {
        if (!TryClipLineToRectangle(start, end, clip, out Vector2 clippedStart, out Vector2 clippedEnd))
            return;

        this.DrawPixelLine(b, clippedStart, clippedEnd, color, thickness);
    }

    private static bool TryClipLineToRectangle(Vector2 start, Vector2 end, Rectangle clip, out Vector2 clippedStart, out Vector2 clippedEnd)
    {
        float x0 = start.X;
        float y0 = start.Y;
        float x1 = end.X;
        float y1 = end.Y;
        float dx = x1 - x0;
        float dy = y1 - y0;
        float t0 = 0f;
        float t1 = 1f;

        bool Clip(float p, float q)
        {
            if (Math.Abs(p) < 0.0001f)
                return q >= 0f;

            float r = q / p;
            if (p < 0f)
            {
                if (r > t1)
                    return false;
                if (r > t0)
                    t0 = r;
            }
            else
            {
                if (r < t0)
                    return false;
                if (r < t1)
                    t1 = r;
            }

            return true;
        }

        if (!Clip(-dx, x0 - clip.Left) || !Clip(dx, clip.Right - x0) || !Clip(-dy, y0 - clip.Top) || !Clip(dy, clip.Bottom - y0))
        {
            clippedStart = start;
            clippedEnd = end;
            return false;
        }

        clippedStart = new Vector2(x0 + t0 * dx, y0 + t0 * dy);
        clippedEnd = new Vector2(x0 + t1 * dx, y0 + t1 * dy);
        return true;
    }

    private void DrawWrappedTextRightAligned(SpriteBatch b, string text, Rectangle bounds, Color color, Rectangle viewport)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        List<string> lines = this.WrapTextToWidth(text, bounds.Width);
        int y = bounds.Y;
        foreach (string line in lines.Take(2))
        {
            if (y >= viewport.Y && y <= viewport.Bottom - 26)
            {
                Vector2 size = Game1.smallFont.MeasureString(line);
                Utility.drawTextWithShadow(
                    b,
                    line,
                    Game1.smallFont,
                    new Vector2(bounds.Right - size.X, y),
                    color
                );
            }
            y += 28;
        }
    }

    private List<string> WrapTextToWidth(string text, int maxWidth)
    {
        List<string> result = new();
        string current = string.Empty;

        foreach (char c in text)
        {
            string next = current + c;
            if (current.Length > 0 && Game1.smallFont.MeasureString(next).X > maxWidth)
            {
                result.Add(current);
                current = c.ToString();
            }
            else
            {
                current = next;
            }
        }

        if (!string.IsNullOrEmpty(current))
            result.Add(current);

        return result;
    }

    private string FormatExpiryDate(int daysFromNow)
    {
        string[] seasons = { "spring", "summer", "fall", "winter" };
        int seasonIndex = Array.FindIndex(seasons, s => string.Equals(s, Game1.currentSeason, StringComparison.OrdinalIgnoreCase));
        if (seasonIndex < 0)
            seasonIndex = 0;

        int year = Game1.year;
        int day = Game1.dayOfMonth + Math.Max(0, daysFromNow);

        while (day > 28)
        {
            day -= 28;
            seasonIndex++;
            if (seasonIndex >= seasons.Length)
            {
                seasonIndex = 0;
                year++;
            }
        }

        return I18n.Date(year, seasons[seasonIndex], day);
    }

    private static int CalculateWarningPrice(int openPrice, string direction)
    {
        double factor = direction == ExchangePosition.DirectionShort ? 1.08 : 0.92;
        return Math.Max(0, (int)Math.Round(openPrice * factor, MidpointRounding.AwayFromZero));
    }

    private static int CalculateLiquidationPrice(int openPrice, string direction)
    {
        double factor = direction == ExchangePosition.DirectionShort ? 1.20 : 0.80;
        return Math.Max(0, (int)Math.Round(openPrice * factor, MidpointRounding.AwayFromZero));
    }

    private static int KeyToDigit(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return (int)key - (int)Keys.D0;

        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            return (int)key - (int)Keys.NumPad0;

        return -1;
    }
}
