using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        Create,
        Positions
    }

    private enum CommodityFilter
    {
        All,
        Artisan,
        Vegetable,
        Fruit
    }

    private enum DeliveryModalKind
    {
        None,
        LongDelivery,
        ShortDelivery,
        DeliveryDefault
    }

    private const int CommodityListVisibleRows = 9;
    private const int CommodityListRowHeight = 32;
    private const int PositionsListVisibleRows = 8;
    private const int PositionsListRowHeight = 44;
    private const int CreatePagePadding = 18;
    private const int CreatePageSectionGap = 22;

    private readonly ExchangeService exchangeService;
    private readonly ExchangeContractCatalogService? catalogService;
    private readonly List<ExchangeContractSpec> catalog;
    private readonly Dictionary<CommodityFilter, List<ExchangeContractSpec>> filteredCatalogs;

    private readonly Rectangle accountPageButton;
    private readonly Rectangle createPageButton;
    private readonly Rectangle positionsPageButton;
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
    private string positionsMessage = string.Empty;
    private bool positionsMessageSucceeded = true;
    private double positionsMessageExpiresAt;

    private int selectedCatalogIndex;
    private int commodityListScrollIndex;
    private int selectedPositionIndex;
    private int positionsListScrollIndex;
    private string selectedDirection = ExchangePosition.DirectionLong;
    private int selectedTermDays = 7;
    private int selectedLots = 1;
    private bool confirmCreateOpen;
    private int createPageScrollOffset;
    private string pendingDeliveryDefaultContractId = string.Empty;
    private DeliveryModalKind deliveryModalKind = DeliveryModalKind.None;
    private string deliveryModalContractId = string.Empty;


    private sealed class PositionsLayout
    {
        public Rectangle Viewport { get; init; }
        public Rectangle ListBounds { get; init; }
        public Rectangle UpButton { get; init; }
        public Rectangle DownButton { get; init; }
        public Rectangle DetailBounds { get; init; }
        public Rectangle TopUpButton { get; init; }
        public Rectangle ClosePositionButton { get; init; }
        public Rectangle DepositDeliveryButton { get; init; }
        public Rectangle ExecuteDeliveryButton { get; init; }
        public Rectangle ClaimDeliveryButton { get; init; }
    }

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
        this.accountPageButton = new Rectangle(this.xPositionOnScreen + 60, tabY, 140, 48);
        this.createPageButton = new Rectangle(this.accountPageButton.Right + 14, tabY, 140, 48);
        this.positionsPageButton = new Rectangle(this.createPageButton.Right + 14, tabY, 160, 48);
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

        if (this.deliveryModalKind != DeliveryModalKind.None)
        {
            this.HandleDeliveryModalClick(x, y);
            return;
        }

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

        if (this.positionsPageButton.Contains(x, y))
        {
            this.currentPage = ExchangePage.Positions;
            this.transferInputSelected = false;
            this.ClampSelectedPositionIndex();
            Game1.playSound("smallSelect");
            return;
        }

        if (this.currentPage == ExchangePage.Account)
        {
            this.HandleAccountClick(x, y);
            return;
        }

        if (this.currentPage == ExchangePage.Positions)
        {
            this.transferInputSelected = false;
            this.HandlePositionsClick(x, y);
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

        if (this.currentPage == ExchangePage.Positions)
        {
            PositionsLayout layout = this.GetPositionsLayout();
            int count = this.GetSortedPositions().Count;
            int visibleRows = this.GetPositionsVisibleRows(layout);
            int maxScroll = Math.Max(0, count - visibleRows);
            if (maxScroll > 0)
            {
                this.positionsListScrollIndex += direction < 0 ? 1 : -1;
                this.ClampPositionsListScrollIndex(count);
                Game1.playSound("shiny4");
            }

            return;
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


    private void HandleDeliveryModalClick(int x, int y)
    {
        Rectangle box = this.GetDeliveryModalBox();
        (Rectangle confirm, Rectangle cancel) = this.GetDeliveryModalButtons(box);

        if (cancel.Contains(x, y))
        {
            this.CloseDeliveryModal();
            Game1.playSound("bigDeSelect");
            return;
        }

        if (!confirm.Contains(x, y))
            return;

        string contractId = this.deliveryModalContractId;
        this.CloseDeliveryModal();

        bool success = this.exchangeService.TryExecuteDelivery(contractId, out string message);
        this.SelectPositionByContractId(contractId);
        this.SetPositionsMessage(message, success);
        Game1.playSound(success ? "coin" : "cancel");
    }


    private void HandlePositionsClick(int x, int y)
    {
        PositionsLayout layout = this.GetPositionsLayout();
        List<ExchangePosition> positions = this.GetSortedPositions();
        if (positions.Count == 0)
            return;

        this.ClampSelectedPositionIndex(positions.Count);

        ExchangePosition selectedPosition = positions[this.selectedPositionIndex];
        int requiredTopUp = this.exchangeService.GetRequiredMarginTopUp(selectedPosition);
        bool selectedPositionIsMarginCall = string.Equals(
            selectedPosition.Status,
            ExchangePosition.StatusMarginCall,
            StringComparison.OrdinalIgnoreCase
        );
        if (layout.TopUpButton.Contains(x, y) && selectedPositionIsMarginCall && requiredTopUp > 0)
        {
            bool success = this.exchangeService.TryTopUpMargin(selectedPosition.ContractId, out string message);
            this.SetPositionsMessage(message, success);
            Game1.playSound(success ? "coin" : "cancel");
            return;
        }

        if (layout.ClosePositionButton.Contains(x, y) && this.CanClosePositionForUi(selectedPosition))
        {
            bool success = this.exchangeService.TryClosePosition(selectedPosition.ContractId, out string message);
            this.SetPositionsMessage(message, success);
            Game1.playSound(success ? "coin" : "cancel");
            return;
        }

        if (layout.DepositDeliveryButton.Contains(x, y) && this.CanDepositDeliveryForUi(selectedPosition))
        {
            bool success = this.exchangeService.TryDepositDeliveryGoods(selectedPosition.ContractId, out string message);
            if (success && string.Equals(this.pendingDeliveryDefaultContractId, selectedPosition.ContractId, StringComparison.OrdinalIgnoreCase))
                this.pendingDeliveryDefaultContractId = string.Empty;

            this.SetPositionsMessage(message, success);
            Game1.playSound(success ? "coin" : "cancel");
            return;
        }

        if (layout.ExecuteDeliveryButton.Contains(x, y) && this.CanExecuteDeliveryForUi(selectedPosition))
        {
            this.OpenDeliveryModal(selectedPosition);
            return;
        }

        if (layout.ClaimDeliveryButton.Contains(x, y) && this.CanClaimDeliveryForUi(selectedPosition))
        {
            bool success = this.exchangeService.TryClaimDeliveredGoods(selectedPosition.ContractId, out string message);
            this.SetPositionsMessage(message, success);
            Game1.playSound(success ? "coin" : "cancel");
            return;
        }

        if (layout.UpButton.Contains(x, y))
        {
            this.positionsListScrollIndex--;
            this.ClampPositionsListScrollIndex(positions.Count);
            Game1.playSound("shiny4");
            return;
        }

        if (layout.DownButton.Contains(x, y))
        {
            this.positionsListScrollIndex++;
            this.ClampPositionsListScrollIndex(positions.Count);
            Game1.playSound("shiny4");
            return;
        }

        if (layout.ListBounds.Contains(x, y))
        {
            int row = (y - layout.ListBounds.Y) / PositionsListRowHeight;
            int index = this.positionsListScrollIndex + row;
            if (index >= 0 && index < positions.Count)
            {
                this.selectedPositionIndex = index;
                this.EnsurePositionSelectionVisible(positions.Count);
                Game1.playSound("smallSelect");
            }
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
        else if (this.currentPage == ExchangePage.Positions)
        {
            this.DrawPositionsPage(b);
        }
        else
        {
            this.DrawCreateContractPage(b);
        }

        this.DrawPageMessage(b);

        if (this.confirmCreateOpen)
            this.DrawCreateConfirmation(b);

        if (this.deliveryModalKind != DeliveryModalKind.None)
            this.DrawDeliveryModal(b);

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

        this.DrawButton(
            b,
            this.positionsPageButton,
            this.currentPage == ExchangePage.Positions ? I18n.Get("exchange.tab_positions_active") : I18n.Get("exchange.tab_positions"),
            selected: this.currentPage == ExchangePage.Positions
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

        this.DrawAccountHistory(b, account, x, y + 50);
    }

    private void DrawAccountHistory(SpriteBatch b, ExchangeAccount account, int x, int y)
    {
        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.account_history_title"),
            Game1.smallFont,
            new Vector2(x, y),
            Game1.textColor
        );

        y += 34;
        int bottomLimit = this.yPositionOnScreen + this.height - 70;
        int maxRows = Math.Max(1, Math.Min(10, (bottomLimit - y) / 28));
        List<ExchangeAccountHistoryEntry> entries = account.AccountHistory
            .AsEnumerable()
            .Reverse()
            .Take(maxRows)
            .ToList();

        if (entries.Count == 0)
        {
            this.DrawLine(b, I18n.Get("exchange.account_history_empty"), x, y, Game1.textColor * 0.65f);
            return;
        }

        int rightLimit = this.xPositionOnScreen + this.width - 82;
        int maxWidth = Math.Max(360, rightLimit - x);
        foreach (ExchangeAccountHistoryEntry entry in entries)
        {
            string date = entry.Year > 0
                ? I18n.Date(entry.Year, entry.Season, entry.Day)
                : string.Empty;
            string description = this.FormatAccountHistoryDescription(entry.Description);
            string line = string.IsNullOrWhiteSpace(date)
                ? description
                : $"{date}  {description}";

            this.DrawLine(
                b,
                this.TrimToWidth(line, maxWidth),
                x,
                y,
                entry.Amount < 0 ? Color.Red : Game1.textColor * 0.76f
            );
            y += 28;
        }
    }

    private string FormatAccountHistoryDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return description;

        string text = description;
        text = text.Replace("Transfer to Exchange Account", I18n.Get("ledger.exchange_transfer_to"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Transfer from Exchange Account", I18n.Get("ledger.exchange_transfer_from"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Exchange debt collected from farm wallet", I18n.Get("ledger.exchange_debt_collection"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Open Long", I18n.Get("ledger.exchange_open_long"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Open Short", I18n.Get("ledger.exchange_open_short"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace(": margin ", "：" + I18n.Get("ledger.margin") + " ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" margin ", " " + I18n.Get("ledger.margin") + " ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("available cash", I18n.Get("exchange.history_available_cash"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("at ", I18n.Get("exchange.history_at_price_prefix"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Forced Liquidation", I18n.Get("exchange.status_forced_liquidated"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Delivery Default", I18n.Get("exchange.status_delivery_default"), StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Close Position", I18n.Get("exchange.status_closed"), StringComparison.OrdinalIgnoreCase);

        string defaultSummary = this.TrySummarizeDeliveryDefaultHistory(text);
        if (!string.IsNullOrWhiteSpace(defaultSummary))
            return defaultSummary;

        string deliverySummary = this.TrySummarizePhysicalDeliveryHistory(text);
        if (!string.IsNullOrWhiteSpace(deliverySummary))
            return deliverySummary;

        string liquidationSummary = this.TrySummarizeForcedLiquidationHistory(text);
        if (!string.IsNullOrWhiteSpace(liquidationSummary))
            return liquidationSummary;

        return text;
    }

    private string TrySummarizeDeliveryDefaultHistory(string text)
    {
        int defaultIndex = text.IndexOf(I18n.Get("exchange.status_delivery_default"), StringComparison.OrdinalIgnoreCase);
        if (defaultIndex < 0)
            return string.Empty;

        string costLabel = I18n.Get("exchange.history_total_cost_label");
        int costIndex = text.IndexOf(costLabel, StringComparison.OrdinalIgnoreCase);
        int costLabelLength = costLabel.Length;
        if (costIndex < 0)
        {
            const string englishCostLabel = "total cost";
            costIndex = text.IndexOf(englishCostLabel, StringComparison.OrdinalIgnoreCase);
            costLabelLength = englishCostLabel.Length;
        }
        if (costIndex < 0)
            return string.Empty;

        string cost = this.ExtractGoldAmountAfterLabel(text, costIndex + costLabelLength);
        if (string.IsNullOrWhiteSpace(cost))
            return string.Empty;

        string prefix = text[..defaultIndex].TrimEnd('：', ':', ' ', '，', ',');
        string separator = string.IsNullOrWhiteSpace(prefix) ? string.Empty : "：";
        return $"{prefix}{separator}{I18n.Get("exchange.status_delivery_default")}，{costLabel} {cost}。";
    }


    private string TrySummarizePhysicalDeliveryHistory(string text)
    {
        bool isLong = text.Contains("多头", StringComparison.OrdinalIgnoreCase) || text.Contains("long delivery", StringComparison.OrdinalIgnoreCase);
        bool isShort = text.Contains("空头", StringComparison.OrdinalIgnoreCase) || text.Contains("short delivery", StringComparison.OrdinalIgnoreCase);
        bool isDelivery = text.Contains("完成交割", StringComparison.OrdinalIgnoreCase)
            || text.Contains("交割完成", StringComparison.OrdinalIgnoreCase)
            || text.Contains("delivery completed", StringComparison.OrdinalIgnoreCase);

        if (!isDelivery || (!isLong && !isShort))
            return string.Empty;

        int prefixEnd = text.IndexOf("：", StringComparison.OrdinalIgnoreCase);
        if (prefixEnd < 0)
            prefixEnd = text.IndexOf(":", StringComparison.OrdinalIgnoreCase);
        if (prefixEnd < 0)
            return string.Empty;

        string amountLabel = isLong ? "支付" : "收到";
        int amountIndex = text.IndexOf(amountLabel, StringComparison.OrdinalIgnoreCase);
        if (amountIndex < 0 && isShort)
        {
            amountLabel = "收入";
            amountIndex = text.IndexOf(amountLabel, StringComparison.OrdinalIgnoreCase);
        }
        if (amountIndex < 0)
        {
            amountLabel = isLong ? "paid" : "received";
            amountIndex = text.IndexOf(amountLabel, StringComparison.OrdinalIgnoreCase);
        }
        if (amountIndex < 0)
            return string.Empty;

        string amount = this.ExtractGoldAmountAfterLabel(text, amountIndex + amountLabel.Length);
        if (string.IsNullOrWhiteSpace(amount))
            return string.Empty;

        string prefix = text[..prefixEnd].TrimEnd('：', ':', ' ', '，', ',');
        string kind = isLong ? I18n.Get("exchange.history_long_delivery_short_label") : I18n.Get("exchange.history_short_delivery_short_label");
        string cashLabel = isLong ? I18n.Get("exchange.history_paid_label") : I18n.Get("exchange.history_received_label");
        return $"{prefix}：{kind}，{cashLabel} {amount}。";
    }

    private string TrySummarizeForcedLiquidationHistory(string text)
    {
        int liquidationIndex = text.IndexOf(I18n.Get("exchange.status_forced_liquidated"), StringComparison.OrdinalIgnoreCase);
        if (liquidationIndex < 0 && text.IndexOf("强平", StringComparison.OrdinalIgnoreCase) < 0)
            return string.Empty;

        string settlementLabel = I18n.Get("exchange.history_settlement_label");
        int settlementIndex = text.IndexOf(settlementLabel, StringComparison.OrdinalIgnoreCase);
        int settlementLabelLength = settlementLabel.Length;
        if (settlementIndex < 0)
        {
            const string profitLossLabel = "盈亏";
            settlementIndex = text.IndexOf(profitLossLabel, StringComparison.OrdinalIgnoreCase);
            settlementLabelLength = profitLossLabel.Length;
        }
        if (settlementIndex < 0)
        {
            const string englishSettlementLabel = "P/L";
            settlementIndex = text.IndexOf(englishSettlementLabel, StringComparison.OrdinalIgnoreCase);
            settlementLabelLength = englishSettlementLabel.Length;
        }
        if (settlementIndex < 0)
            return string.Empty;

        string settlement = this.ExtractGoldAmountAfterLabel(text, settlementIndex + settlementLabelLength);
        if (string.IsNullOrWhiteSpace(settlement))
            return string.Empty;

        int prefixEnd = liquidationIndex >= 0 ? liquidationIndex : text.IndexOf("：", StringComparison.OrdinalIgnoreCase);
        if (prefixEnd < 0)
            prefixEnd = text.IndexOf(":", StringComparison.OrdinalIgnoreCase);
        if (prefixEnd < 0)
            prefixEnd = text.IndexOf("强平", StringComparison.OrdinalIgnoreCase);
        string prefix = text[..prefixEnd].TrimEnd('：', ':', ' ', '，', ',');
        string separator = string.IsNullOrWhiteSpace(prefix) ? string.Empty : "：";
        return $"{prefix}{separator}{I18n.Get("exchange.status_forced_liquidated")}，{settlementLabel} {settlement}。";
    }

    private string ExtractGoldAmountAfterLabel(string text, int startIndex)
    {
        if (startIndex < 0 || startIndex >= text.Length)
            return string.Empty;

        int index = startIndex;
        while (index < text.Length && (char.IsWhiteSpace(text[index]) || text[index] == '：' || text[index] == ':' || text[index] == '='))
            index++;

        int amountStart = index;
        if (index < text.Length && (text[index] == '+' || text[index] == '-'))
            index++;
        while (index < text.Length && char.IsDigit(text[index]))
            index++;
        if (index < text.Length && (text[index] == 'g' || text[index] == 'G'))
            index++;

        return index > amountStart ? text[amountStart..index] : string.Empty;
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


    private void DrawPositionsPage(SpriteBatch b)
    {
        PositionsLayout layout = this.GetPositionsLayout();
        List<ExchangePosition> positions = this.GetSortedPositions();

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.positions_title"),
            Game1.smallFont,
            new Vector2(layout.Viewport.X + 18, layout.Viewport.Y + 14),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.positions_count", new { count = positions.Count }),
            Game1.smallFont,
            new Vector2(layout.DetailBounds.X, layout.Viewport.Y + 14),
            Game1.textColor * 0.78f
        );

        if (positions.Count == 0)
        {
            Utility.drawTextWithShadow(
                b,
                I18n.Get("exchange.positions_empty"),
                Game1.smallFont,
                new Vector2(layout.Viewport.X + 18, layout.Viewport.Y + 86),
                Game1.textColor * 0.78f
            );
            return;
        }

        this.ClampSelectedPositionIndex(positions.Count, ensureVisible: false);
        this.ClampPositionsListScrollIndex(positions.Count);
        this.DrawPositionsList(b, positions, layout);
        this.DrawSelectedPositionDetails(b, positions[this.selectedPositionIndex], layout);
    }

    private void DrawPositionsList(SpriteBatch b, List<ExchangePosition> positions, PositionsLayout layout)
    {
        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.positions_list_header"),
            Game1.smallFont,
            new Vector2(layout.ListBounds.X, layout.ListBounds.Y - 34),
            Game1.textColor
        );

        int visibleRows = this.GetPositionsVisibleRows(layout);
        b.Draw(Game1.staminaRect, layout.ListBounds, Color.White * 0.10f);
        this.DrawRectangleOutline(b, layout.ListBounds, 1, Color.Black * 0.24f);

        int maxVisible = Math.Min(visibleRows, positions.Count - this.positionsListScrollIndex);
        for (int row = 0; row < maxVisible; row++)
        {
            int index = this.positionsListScrollIndex + row;
            ExchangePosition position = positions[index];
            Rectangle rowBounds = new(
                layout.ListBounds.X + 4,
                layout.ListBounds.Y + row * PositionsListRowHeight + 3,
                layout.ListBounds.Width - 24,
                PositionsListRowHeight - 6
            );

            if (index == this.selectedPositionIndex)
            {
                b.Draw(Game1.staminaRect, rowBounds, Color.Wheat * 0.55f);
                this.DrawRectangleOutline(b, rowBounds, 1, Color.SaddleBrown * 0.35f);
            }

            string title = I18n.Get("exchange.positions_row_title", new
            {
                name = this.GetPositionDisplayName(position),
                direction = this.FormatDirection(position.Direction),
                lots = position.Lots
            });
            int daysLeft = this.GetDaysLeft(position);
            string meta = I18n.Get("exchange.positions_row_status", new
            {
                days = daysLeft,
                status = this.FormatPositionLifecycleStatus(position, daysLeft)
            });

            Vector2 metaSize = Game1.smallFont.MeasureString(meta);
            int metaX = rowBounds.Right - 8 - (int)metaSize.X;
            int textY = rowBounds.Y + Math.Max(0, (rowBounds.Height - (int)Game1.smallFont.MeasureString("Ag").Y) / 2);
            int titleMaxWidth = Math.Max(110, metaX - rowBounds.X - 18);

            Utility.drawTextWithShadow(
                b,
                this.TrimToWidth(title, titleMaxWidth),
                Game1.smallFont,
                new Vector2(rowBounds.X + 8, textY),
                Game1.textColor
            );
            Utility.drawTextWithShadow(
                b,
                this.TrimToWidth(meta, Math.Max(90, rowBounds.Right - (rowBounds.X + titleMaxWidth + 22))),
                Game1.smallFont,
                new Vector2(Math.Max(rowBounds.X + titleMaxWidth + 18, metaX), textY),
                Game1.textColor * 0.68f
            );
        }

        int maxScroll = Math.Max(0, positions.Count - visibleRows);
        if (maxScroll > 0)
        {
            this.DrawButton(b, layout.UpButton, "▲");
            this.DrawButton(b, layout.DownButton, "▼");

            Rectangle track = new(layout.ListBounds.Right - 12, layout.ListBounds.Y + 34, 5, layout.ListBounds.Height - 68);
            b.Draw(Game1.staminaRect, track, Color.Black * 0.12f);
            int thumbHeight = Math.Max(28, (int)(track.Height * (visibleRows / (float)positions.Count)));
            int thumbY = track.Y + (int)((track.Height - thumbHeight) * (this.positionsListScrollIndex / (float)maxScroll));
            b.Draw(Game1.staminaRect, new Rectangle(track.X, thumbY, track.Width, thumbHeight), Color.SaddleBrown * 0.45f);
        }
    }

    private void DrawSelectedPositionDetails(SpriteBatch b, ExchangePosition position, PositionsLayout layout)
    {
        int currentPrice = this.GetPositionDisplayPrice(position);
        int unrealized = this.CalculateUnrealizedProfit(position, currentPrice);
        int currentValue = currentPrice * Math.Max(0, position.TotalQuantity);
        int daysLeft = this.GetDaysLeft(position);

        int detailX = layout.DetailBounds.X;
        int detailY = layout.DetailBounds.Y;
        int columnGap = 56;
        int columnWidth = Math.Max(260, (layout.DetailBounds.Width - columnGap) / 2);
        int leftLabelX = detailX;
        int leftValueX = detailX + Math.Min(180, Math.Max(138, columnWidth / 2));
        int rightLabelX = detailX + columnWidth + columnGap;
        int rightValueX = rightLabelX + Math.Min(180, Math.Max(138, columnWidth / 2));
        const int rowHeight = 30;

        Utility.drawTextWithShadow(
            b,
            this.GetPositionDisplayName(position),
            Game1.smallFont,
            new Vector2(detailX, detailY - 10),
            Game1.textColor
        );

        int leftY = detailY + 34;
        this.DrawLabelValue(b, I18n.Get("exchange.position_id"), position.ContractId, leftLabelX, leftValueX, leftY);
        leftY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.position_status"), this.FormatPositionLifecycleStatus(position, daysLeft), leftLabelX, leftValueX, leftY, this.GetPositionStatusColor(position, daysLeft));
        leftY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.confirm_direction"), this.FormatDirection(position.Direction), leftLabelX, leftValueX, leftY);
        leftY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.confirm_term"), I18n.Get("exchange.value_days", new { days = position.TermDays }), leftLabelX, leftValueX, leftY);
        leftY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.create_lots_label"), position.Lots.ToString(), leftLabelX, leftValueX, leftY);
        leftY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_total_quantity"), position.TotalQuantity.ToString(), leftLabelX, leftValueX, leftY);
        leftY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.position_open_price"), I18n.Get("exchange.value_gold", new { amount = position.OpenPrice }), leftLabelX, leftValueX, leftY);
        leftY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.position_current_price"), I18n.Get("exchange.value_gold", new { amount = currentPrice }), leftLabelX, leftValueX, leftY);

        int rightY = detailY + 34;
        this.DrawLabelValue(b, I18n.Get("exchange.position_unrealized"), I18n.Get("exchange.value_signed_gold", new { amount = unrealized }), rightLabelX, rightValueX, rightY, unrealized < 0 ? Color.Red : Game1.textColor);
        rightY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.position_current_value"), I18n.Get("exchange.value_gold", new { amount = currentValue }), rightLabelX, rightValueX, rightY);
        rightY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_initial_margin"), I18n.Get("exchange.value_gold", new { amount = position.InitialMarginRequired }), rightLabelX, rightValueX, rightY);
        rightY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_maintenance_margin"), I18n.Get("exchange.value_gold", new { amount = position.MaintenanceMarginRequired }), rightLabelX, rightValueX, rightY);
        rightY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.position_margin"), I18n.Get("exchange.value_gold", new { amount = position.PositionMargin }), rightLabelX, rightValueX, rightY);
        rightY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.position_open_date"), I18n.Date(position.OpenYear, position.OpenSeason, position.OpenDay), rightLabelX, rightValueX, rightY);
        rightY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.label_expiry"), this.FormatDateIndex(position.ExpiryDateIndex), rightLabelX, rightValueX, rightY);
        rightY += rowHeight;
        this.DrawLabelValue(b, I18n.Get("exchange.position_days_left"), I18n.Get("exchange.value_days", new { days = daysLeft }), rightLabelX, rightValueX, rightY);
        rightY += rowHeight;
        if (this.ShouldShowDeliveryStorageRow(position))
        {
            int stored = this.exchangeService.GetDeliveryStorageQuantity(position);
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_storage_quantity"), I18n.Get("exchange.delivery_storage_value", new { stored, required = position.TotalQuantity }), rightLabelX, rightValueX, rightY);
            rightY += rowHeight;
        }

        int requiredTopUp = this.exchangeService.GetRequiredMarginTopUp(position);
        bool isMarginCall = string.Equals(position.Status, ExchangePosition.StatusMarginCall, StringComparison.OrdinalIgnoreCase);
        bool showTopUpButton = position.IsOpenLike() && isMarginCall && requiredTopUp > 0;
        bool showCloseButton = this.CanClosePositionForUi(position);
        bool showDepositDeliveryButton = this.CanDepositDeliveryForUi(position);
        bool showExecuteDeliveryButton = this.CanExecuteDeliveryForUi(position);
        bool showClaimDeliveryButton = this.CanClaimDeliveryForUi(position);
        bool hasBottomActionButtons = showTopUpButton || showCloseButton || showDepositDeliveryButton || showExecuteDeliveryButton || showClaimDeliveryButton;

        int contentBottom = Math.Max(leftY, rightY);
        int buttonsTop = Math.Min(
            Math.Min(layout.TopUpButton.Y, layout.ClosePositionButton.Y),
            Math.Min(layout.DepositDeliveryButton.Y, layout.ExecuteDeliveryButton.Y)
        );
        int chartBottom = hasBottomActionButtons
            ? buttonsTop - 12
            : layout.DetailBounds.Bottom - 2;
        int availableChartHeight = Math.Max(0, chartBottom - contentBottom - 18);
        int chartHeight = Math.Min(118, availableChartHeight);
        if (chartHeight >= 64)
        {
            Rectangle chartBounds = new(
                layout.DetailBounds.X,
                chartBottom - chartHeight,
                layout.DetailBounds.Width,
                chartHeight
            );
            this.DrawPositionMiniPriceChart(b, position, chartBounds);
        }

        if (showTopUpButton)
        {
            int noticeY = layout.TopUpButton.Y - 32;
            Utility.drawTextWithShadow(
                b,
                I18n.Get("exchange.margin_topup_required", new { amount = requiredTopUp }),
                Game1.smallFont,
                new Vector2(layout.TopUpButton.X, noticeY),
                Color.OrangeRed
            );

            this.DrawButton(b, layout.TopUpButton, I18n.Get("exchange.margin_topup_button"), selected: true, enabled: true);
        }

        if (showCloseButton)
            this.DrawButton(b, layout.ClosePositionButton, I18n.Get("exchange.close_position_button"), selected: false, enabled: true);

        if (showDepositDeliveryButton)
            this.DrawButton(b, layout.DepositDeliveryButton, I18n.Get("exchange.delivery_deposit_button"), selected: false, enabled: true);

        if (showExecuteDeliveryButton)
        {
            string executeLabel = this.NeedsDeliveryDefaultConfirmation(position)
                && string.Equals(this.pendingDeliveryDefaultContractId, position.ContractId, StringComparison.OrdinalIgnoreCase)
                    ? I18n.Get("exchange.delivery_default_confirm_button")
                    : I18n.Get("exchange.delivery_execute_button");

            this.DrawButton(b, layout.ExecuteDeliveryButton, executeLabel, selected: true, enabled: true);
        }

        if (showClaimDeliveryButton)
            this.DrawButton(b, layout.ClaimDeliveryButton, I18n.Get("exchange.delivery_claim_button"), selected: false, enabled: true);
    }

    private void DrawPositionMiniPriceChart(SpriteBatch b, ExchangePosition position, Rectangle bounds)
    {
        if (this.catalogService is null || bounds.Width < 180 || bounds.Height < 64)
            return;

        List<MarketPriceHistoryPoint> points = this.catalogService
            .GetMarketPriceHistory(position.MarketCommodityKey)
            .OrderBy(p => p.DateIndex)
            .TakeLast(28)
            .ToList();

        if (points.Count < 2)
            return;

        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.position_chart_title"),
            Game1.smallFont,
            new Vector2(bounds.X, bounds.Y - 26),
            Game1.textColor * 0.78f
        );

        Rectangle chart = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        b.Draw(Game1.staminaRect, chart, Color.White * 0.10f);
        this.DrawRectangleOutline(b, chart, 1, Color.Black * 0.22f);

        int linePrice = CalculateLiquidationPrice(position.OpenPrice, position.Direction);
        int min = Math.Max(0, Math.Min(points.Min(p => p.MarketUnitPrice), linePrice));
        int max = Math.Max(1, Math.Max(points.Max(p => p.MarketUnitPrice), linePrice));
        if (max <= min)
            max = min + 1;

        float ToY(int price)
        {
            float normalized = (price - min) / (float)(max - min);
            return chart.Bottom - 8 - (chart.Height - 16) * normalized;
        }

        float riskY = ToY(linePrice);
        if (riskY >= chart.Y && riskY <= chart.Bottom)
        {
            this.DrawPixelLine(
                b,
                new Vector2(chart.X + 4, riskY),
                new Vector2(chart.Right - 4, riskY),
                Color.Red * 0.68f,
                2f
            );
            Utility.drawTextWithShadow(
                b,
                I18n.Get("exchange.history_liquidation_line"),
                Game1.smallFont,
                new Vector2(chart.Right - 96, riskY - 24),
                Color.Red
            );
        }

        Vector2? previous = null;
        for (int i = 0; i < points.Count; i++)
        {
            float px = chart.X + 8 + (chart.Width - 16) * (points.Count == 1 ? 0f : i / (float)(points.Count - 1));
            float py = ToY(points[i].MarketUnitPrice);
            Vector2 point = new(px, py);
            if (previous.HasValue)
                this.DrawPixelLine(b, previous.Value, point, Game1.textColor * 0.72f, 2f);
            previous = point;
        }

        int current = points[^1].MarketUnitPrice;
        Utility.drawTextWithShadow(
            b,
            I18n.Get("exchange.position_chart_current", new { current, risk = linePrice }),
            Game1.smallFont,
            new Vector2(chart.X + 10, chart.Bottom - 30),
            Game1.textColor * 0.78f
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


    private void DrawDeliveryModal(SpriteBatch b)
    {
        ExchangePosition? position = this.FindPositionByContractId(this.deliveryModalContractId);
        if (position is null)
        {
            this.CloseDeliveryModal();
            return;
        }

        ExchangeService.DeliveryPreview preview = this.exchangeService.GetDeliveryPreview(position);

        b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.45f);

        Rectangle box = this.GetDeliveryModalBox();
        IClickableMenu.drawTextureBox(b, box.X, box.Y, box.Width, box.Height, Color.White);

        bool isDefault = this.deliveryModalKind == DeliveryModalKind.DeliveryDefault;
        bool isLong = this.deliveryModalKind == DeliveryModalKind.LongDelivery;
        bool isShort = this.deliveryModalKind == DeliveryModalKind.ShortDelivery;

        string title = isDefault
            ? I18n.Get("exchange.delivery_modal_default_title")
            : I18n.Get("exchange.delivery_modal_title");

        Utility.drawTextWithShadow(
            b,
            title,
            Game1.dialogueFont,
            new Vector2(box.X + 36, box.Y + 26),
            isDefault ? Color.DarkRed : Game1.textColor
        );

        Rectangle slot = new(box.X + 46, box.Y + 98, 86, 86);
        this.DrawDeliveryItemSlot(b, position, slot, preview.Quantity);

        int labelX = box.X + 160;
        int valueX = box.X + 378;
        int y = box.Y + 98;
        const int row = 28;

        this.DrawLabelValue(b, I18n.Get("exchange.confirm_contract"), position.ContractId, labelX, valueX, y);
        y += row;
        this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_goods"), I18n.Get("exchange.delivery_modal_goods_value", new { name = this.GetPositionDisplayName(position), quantity = preview.Quantity }), labelX, valueX, y);
        y += row;
        this.DrawLabelValue(b, I18n.Get("exchange.label_price"), I18n.Get("exchange.value_gold", new { amount = preview.DeliveryPrice }), labelX, valueX, y);
        y += row;

        if (isLong)
        {
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_pay"), I18n.Get("exchange.value_gold", new { amount = preview.DeliveryValue }), labelX, valueX, y);
            y += row;
            Utility.drawTextWithShadow(
                b,
                I18n.Get("exchange.delivery_modal_long_note"),
                Game1.smallFont,
                new Vector2(labelX, y + 12),
                Game1.textColor * 0.82f
            );
        }
        else if (isShort)
        {
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_stored"), I18n.Get("exchange.delivery_storage_value", new { stored = preview.StoredQuantity, required = preview.Quantity }), labelX, valueX, y);
            y += row;
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_receive"), I18n.Get("exchange.value_gold", new { amount = preview.DeliveryValue }), labelX, valueX, y);
            y += row;
            Utility.drawTextWithShadow(
                b,
                I18n.Get("exchange.delivery_modal_short_note"),
                Game1.smallFont,
                new Vector2(labelX, y + 12),
                Game1.textColor * 0.82f
            );
        }
        else
        {
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_stored"), I18n.Get("exchange.delivery_storage_value", new { stored = preview.StoredQuantity, required = preview.Quantity }), labelX, valueX, y);
            y += row;
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_shortage"), I18n.Get("exchange.delivery_modal_goods_value", new { name = this.GetPositionDisplayName(position), quantity = preview.ShortageQuantity }), labelX, valueX, y, Color.DarkRed);
            y += row;
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_shortage_value"), I18n.Get("exchange.value_gold", new { amount = preview.ShortageValue }), labelX, valueX, y);
            y += row;
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_fixed_fee"), I18n.Get("exchange.value_gold", new { amount = preview.FixedFee }), labelX, valueX, y);
            y += row;
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_penalty"), I18n.Get("exchange.value_gold", new { amount = preview.Penalty }), labelX, valueX, y);
            y += row;
            this.DrawLabelValue(b, I18n.Get("exchange.delivery_modal_default_cost"), I18n.Get("exchange.value_gold", new { amount = preview.DefaultCost }), labelX, valueX, y, Color.DarkRed);
        }

        (Rectangle confirm, Rectangle cancel) = this.GetDeliveryModalButtons(box);
        string confirmLabel = isDefault
            ? I18n.Get("exchange.delivery_default_confirm_button")
            : I18n.Get("exchange.delivery_modal_confirm");

        this.DrawButton(b, confirm, confirmLabel, selected: true, enabled: true);
        this.DrawButton(b, cancel, I18n.Get("exchange.confirm_cancel"));
    }

    private void DrawDeliveryItemSlot(SpriteBatch b, ExchangePosition position, Rectangle slot, int quantity)
    {
        b.Draw(Game1.staminaRect, slot, Color.White * 0.45f);
        this.DrawRectangleOutline(b, slot, 2, Color.Black * 0.45f);

        Item? item = this.CreateDeliveryPreviewItem(position, quantity);
        if (item is not null)
        {
            item.drawInMenu(
                b,
                new Vector2(slot.X + 11, slot.Y + 10),
                1f,
                1f,
                0.92f,
                StackDrawType.Draw,
                Color.White,
                true
            );
            return;
        }

        this.DrawCenteredText(b, this.GetPositionDisplayName(position), slot, Game1.textColor);
    }

    private Item? CreateDeliveryPreviewItem(ExchangePosition position, int quantity)
    {
        if (position is null || string.IsNullOrWhiteSpace(position.ItemId))
            return null;

        try
        {
            Item item = ItemRegistry.Create(position.ItemId);
            this.ApplyArtisanIdentityToItem(item, position);
            item.Stack = Math.Max(1, Math.Min(999, quantity));
            if (item is StardewValley.Object obj)
                obj.Quality = 0;
            return item;
        }
        catch
        {
            return null;
        }
    }

    private string GetPositionDisplayName(ExchangePosition position)
    {
        ExchangeContractSpec? spec = this.catalog.FirstOrDefault(spec => string.Equals(spec.MarketCommodityKey, position.MarketCommodityKey, StringComparison.OrdinalIgnoreCase));
        if (spec is not null && !string.IsNullOrWhiteSpace(spec.DisplayName))
            return spec.DisplayName;

        Item? item = this.CreateDeliveryPreviewItem(position, 1);
        if (item is not null && !string.IsNullOrWhiteSpace(item.DisplayName))
            return item.DisplayName;

        return position.DisplayName;
    }

    private static bool IsFlavoredArtisanPosition(ExchangePosition position)
    {
        return position is not null
            && !string.IsNullOrWhiteSpace(position.ParentItemId)
            && !string.IsNullOrWhiteSpace(position.MarketCommodityKey)
            && position.MarketCommodityKey.StartsWith("Artisan:", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyArtisanIdentityToItem(Item item, ExchangePosition position)
    {
        if (item is null || !IsFlavoredArtisanPosition(position))
            return;

        string? preserveType = TryReadPreserveTypeFromMarketKey(position.MarketCommodityKey);
        int? parentIndex = TryReadObjectIndex(position.ParentItemId);

        if (string.IsNullOrWhiteSpace(preserveType) || parentIndex is null)
            return;

        TryWriteMemberValue(item, "preserve", preserveType);
        TryWriteMemberValue(item, "preservedParentSheetIndex", parentIndex.Value);
    }

    private static string? TryReadPreserveTypeFromMarketKey(string marketCommodityKey)
    {
        if (string.IsNullOrWhiteSpace(marketCommodityKey))
            return null;

        string[] parts = marketCommodityKey.Split(':');
        return parts.Length >= 3 ? parts[1] : null;
    }

    private static int? TryReadObjectIndex(string qualifiedItemId)
    {
        if (string.IsNullOrWhiteSpace(qualifiedItemId))
            return null;

        string digits = new(qualifiedItemId.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int parsed) ? parsed : null;
    }

    private static void TryWriteMemberValue(Item item, string memberName, object value)
    {
        Type? type = item.GetType();

        while (type is not null)
        {
            FieldInfo? field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
            {
                if (TryWriteNetFieldValue(field.GetValue(item), value))
                    return;

                object? converted = TryConvertMemberValue(field.FieldType, value);
                if (converted is not null)
                {
                    field.SetValue(item, converted);
                    return;
                }
            }

            PropertyInfo? property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                object? currentValue = null;
                try { currentValue = property.GetValue(item); } catch { }

                if (TryWriteNetFieldValue(currentValue, value))
                    return;

                if (property.CanWrite)
                {
                    object? converted = TryConvertMemberValue(property.PropertyType, value);
                    if (converted is not null)
                    {
                        property.SetValue(item, converted);
                        return;
                    }
                }
            }

            type = type.BaseType;
        }
    }

    private static bool TryWriteNetFieldValue(object? target, object value)
    {
        if (target is null)
            return false;

        PropertyInfo? valueProperty = target.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty is null || !valueProperty.CanWrite)
            return false;

        object? converted = TryConvertMemberValue(valueProperty.PropertyType, value);
        if (converted is null)
            return false;

        valueProperty.SetValue(target, converted);
        return true;
    }

    private static object? TryConvertMemberValue(Type targetType, object value)
    {
        Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (effectiveType == typeof(string))
                return value.ToString();

            if (effectiveType == typeof(int))
                return Convert.ToInt32(value);

            if (effectiveType.IsEnum)
            {
                string? text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text) && Enum.TryParse(effectiveType, text, ignoreCase: true, out object? parsed))
                    return parsed;
            }

            if (effectiveType.IsInstanceOfType(value))
                return value;
        }
        catch
        {
            return null;
        }

        return null;
    }

    private Rectangle GetDeliveryModalBox()
    {
        int boxWidth = Math.Min(820, Math.Max(680, Game1.uiViewport.Width - 180));
        int boxHeight = 430;
        return new Rectangle(
            (Game1.uiViewport.Width - boxWidth) / 2,
            (Game1.uiViewport.Height - boxHeight) / 2,
            boxWidth,
            boxHeight
        );
    }

    private (Rectangle Confirm, Rectangle Cancel) GetDeliveryModalButtons(Rectangle box)
    {
        int buttonWidth = 160;
        int buttonHeight = 44;
        int gap = 28;
        int y = box.Bottom - 72;
        int x = box.X + (box.Width - buttonWidth * 2 - gap) / 2;
        return (
            new Rectangle(x, y, buttonWidth, buttonHeight),
            new Rectangle(x + buttonWidth + gap, y, buttonWidth, buttonHeight)
        );
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

        if (this.currentPage == ExchangePage.Create)
        {
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
            return;
        }

        if (this.currentPage != ExchangePage.Positions || string.IsNullOrWhiteSpace(this.positionsMessage))
            return;

        PositionsLayout positionsLayout = this.GetPositionsLayout();
        Utility.drawTextWithShadow(
            b,
            this.positionsMessage,
            Game1.smallFont,
            new Vector2(positionsLayout.DetailBounds.X, positionsLayout.DetailBounds.Bottom + 14),
            this.positionsMessageSucceeded ? Game1.textColor : Color.Red
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

    private void SetPositionsMessage(string message, bool succeeded)
    {
        this.positionsMessage = message;
        this.positionsMessageSucceeded = succeeded;
        this.positionsMessageExpiresAt = this.GetUiTimeMilliseconds() + (succeeded ? 2600 : 4200);
    }

    private void ExpireMessages()
    {
        double now = this.GetUiTimeMilliseconds();
        if (!string.IsNullOrWhiteSpace(this.accountMessage) && now >= this.accountMessageExpiresAt)
            this.accountMessage = string.Empty;

        if (!string.IsNullOrWhiteSpace(this.createMessage) && now >= this.createMessageExpiresAt)
            this.createMessage = string.Empty;

        if (!string.IsNullOrWhiteSpace(this.positionsMessage) && now >= this.positionsMessageExpiresAt)
            this.positionsMessage = string.Empty;
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

        if (ok)
        {
            this.accountMessage = string.Empty;
            this.transferAmountText = string.Empty;
            this.transferInputSelected = false;
        }
        else
        {
            this.SetAccountMessage(message, succeeded: false);
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


    private PositionsLayout GetPositionsLayout()
    {
        Rectangle viewport = new(
            this.xPositionOnScreen + 58,
            this.yPositionOnScreen + 166,
            this.width - 116,
            this.height - 218
        );

        int listX = viewport.X + 18;
        int listY = viewport.Y + 86;
        int listWidth = Math.Min(430, Math.Max(340, (viewport.Width - 86) / 3));
        int listHeight = Math.Max(PositionsListRowHeight * 5, viewport.Bottom - listY - 22);
        listHeight = Math.Min(listHeight, PositionsListRowHeight * 10);
        Rectangle listBounds = new(listX, listY, listWidth, listHeight);
        Rectangle upButton = new(listBounds.Right - 34, listBounds.Y + 2, 30, 26);
        Rectangle downButton = new(listBounds.Right - 34, listBounds.Bottom - 28, 30, 26);

        int detailX = listBounds.Right + 46;
        Rectangle detailBounds = new(
            detailX,
            listY,
            Math.Max(560, viewport.Right - detailX - 24),
            listBounds.Height
        );
        Rectangle topUpButton = new(detailBounds.X, detailBounds.Bottom - 48, 180, 42);
        Rectangle closePositionButton = new(topUpButton.Right + 18, detailBounds.Bottom - 48, 150, 42);
        Rectangle depositDeliveryButton = new(detailBounds.X, detailBounds.Bottom - 48, 180, 42);
        Rectangle executeDeliveryButton = new(depositDeliveryButton.Right + 18, detailBounds.Bottom - 48, 150, 42);
        Rectangle claimDeliveryButton = new(detailBounds.X, detailBounds.Bottom - 48, 180, 42);

        return new PositionsLayout
        {
            Viewport = viewport,
            ListBounds = listBounds,
            UpButton = upButton,
            DownButton = downButton,
            DetailBounds = detailBounds,
            TopUpButton = topUpButton,
            ClosePositionButton = closePositionButton,
            DepositDeliveryButton = depositDeliveryButton,
            ExecuteDeliveryButton = executeDeliveryButton,
            ClaimDeliveryButton = claimDeliveryButton
        };
    }

    private int GetPositionsVisibleRows(PositionsLayout layout)
    {
        return Math.Max(1, layout.ListBounds.Height / PositionsListRowHeight);
    }

    private void ClampSelectedPositionIndex()
    {
        this.ClampSelectedPositionIndex(this.exchangeService.GetPositions().Count, ensureVisible: true);
    }

    private void ClampSelectedPositionIndex(int count, bool ensureVisible = true)
    {
        if (count <= 0)
        {
            this.selectedPositionIndex = 0;
            this.positionsListScrollIndex = 0;
            return;
        }

        this.selectedPositionIndex = Math.Clamp(this.selectedPositionIndex, 0, count - 1);
        if (ensureVisible)
            this.EnsurePositionSelectionVisible(count);
        this.ClampPositionsListScrollIndex(count);
    }

    private void EnsurePositionSelectionVisible(int count)
    {
        if (count <= 0)
        {
            this.positionsListScrollIndex = 0;
            return;
        }

        PositionsLayout layout = this.GetPositionsLayout();
        int visibleRows = this.GetPositionsVisibleRows(layout);
        int maxScroll = Math.Max(0, count - visibleRows);
        if (this.selectedPositionIndex < this.positionsListScrollIndex)
            this.positionsListScrollIndex = this.selectedPositionIndex;
        else if (this.selectedPositionIndex >= this.positionsListScrollIndex + visibleRows)
            this.positionsListScrollIndex = this.selectedPositionIndex - visibleRows + 1;

        this.positionsListScrollIndex = Math.Clamp(this.positionsListScrollIndex, 0, maxScroll);
    }

    private void ClampPositionsListScrollIndex(int count)
    {
        PositionsLayout layout = this.GetPositionsLayout();
        int visibleRows = this.GetPositionsVisibleRows(layout);
        int maxScroll = Math.Max(0, count - visibleRows);
        this.positionsListScrollIndex = Math.Clamp(this.positionsListScrollIndex, 0, maxScroll);
    }

    private List<ExchangePosition> GetSortedPositions()
    {
        List<ExchangePosition> all = this.exchangeService.GetPositions().ToList();
        List<ExchangePosition> active = all
            .Where(position => !this.IsTerminalPosition(position))
            .ToList();
        List<ExchangePosition> recentTerminal = all
            .Where(this.IsTerminalPosition)
            .OrderByDescending(this.GetPositionActivityDateIndex)
            .ThenByDescending(position => position.ContractId)
            .Take(5)
            .ToList();

        return active
            .Concat(recentTerminal)
            .OrderBy(position => this.GetPositionSortPriority(position))
            .ThenBy(position => Math.Max(0, position.ExpiryDateIndex))
            .ThenBy(position => Math.Max(0, position.OpenDateIndex))
            .ThenBy(position => position.ContractId)
            .ToList();
    }

    private bool IsTerminalPosition(ExchangePosition position)
    {
        return string.Equals(position.Status, ExchangePosition.StatusForcedLiquidated, StringComparison.OrdinalIgnoreCase)
            || string.Equals(position.Status, ExchangePosition.StatusClosed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(position.Status, ExchangePosition.StatusDelivered, StringComparison.OrdinalIgnoreCase)
            || string.Equals(position.Status, ExchangePosition.StatusDeliveryDefault, StringComparison.OrdinalIgnoreCase);
    }

    private int GetPositionActivityDateIndex(ExchangePosition position)
    {
        return Math.Max(
            Math.Max(position.ForcedLiquidationDateIndex, position.ExpiryDateIndex),
            position.OpenDateIndex
        );
    }

    private int GetPositionSortPriority(ExchangePosition position)
    {
        int daysLeft = this.GetDaysLeft(position);

        if (string.Equals(position.Status, ExchangePosition.StatusMarginCall, StringComparison.OrdinalIgnoreCase))
            return 10;

        if (string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase)
            || (position.IsOpenLike() && daysLeft <= 0))
            return 20;

        if (string.Equals(position.Status, ExchangePosition.StatusOpen, StringComparison.OrdinalIgnoreCase))
            return 30;

        if (string.Equals(position.Status, ExchangePosition.StatusForcedLiquidated, StringComparison.OrdinalIgnoreCase))
            return 40;

        if (string.Equals(position.Status, ExchangePosition.StatusClosed, StringComparison.OrdinalIgnoreCase))
            return 50;

        if (string.Equals(position.Status, ExchangePosition.StatusDeliveryDefault, StringComparison.OrdinalIgnoreCase))
            return 80;

        if (string.Equals(position.Status, ExchangePosition.StatusDelivered, StringComparison.OrdinalIgnoreCase))
            return 90;

        return 70;
    }

    private void OpenDeliveryModal(ExchangePosition position)
    {
        if (position is null)
            return;

        this.deliveryModalContractId = position.ContractId;
        if (string.Equals(position.Direction, ExchangePosition.DirectionLong, StringComparison.OrdinalIgnoreCase))
            this.deliveryModalKind = DeliveryModalKind.LongDelivery;
        else if (this.NeedsDeliveryDefaultConfirmation(position))
            this.deliveryModalKind = DeliveryModalKind.DeliveryDefault;
        else
            this.deliveryModalKind = DeliveryModalKind.ShortDelivery;

        Game1.playSound("smallSelect");
    }

    private void CloseDeliveryModal()
    {
        this.deliveryModalKind = DeliveryModalKind.None;
        this.deliveryModalContractId = string.Empty;
    }

    private ExchangePosition? FindPositionByContractId(string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
            return null;

        return this.exchangeService.GetPositions().FirstOrDefault(position =>
            string.Equals(position.ContractId, contractId, StringComparison.OrdinalIgnoreCase)
        );
    }

    private void SelectPositionByContractId(string contractId)
    {
        List<ExchangePosition> positions = this.GetSortedPositions();
        int index = positions.FindIndex(position => string.Equals(position.ContractId, contractId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return;

        this.selectedPositionIndex = index;
        this.EnsurePositionSelectionVisible(positions.Count);
    }


    private bool CanClosePositionForUi(ExchangePosition position)
    {
        if (position is null || !position.IsOpenLike())
            return false;

        return this.GetDaysLeft(position) > 0;
    }

    private bool CanDepositDeliveryForUi(ExchangePosition position)
    {
        return position is not null
            && string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase)
            && string.Equals(position.Direction, ExchangePosition.DirectionShort, StringComparison.OrdinalIgnoreCase)
            && this.exchangeService.GetDeliveryStorageQuantity(position) < Math.Max(0, position.TotalQuantity);
    }

    private bool CanExecuteDeliveryForUi(ExchangePosition position)
    {
        return position is not null
            && string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase);
    }

    private bool NeedsDeliveryDefaultConfirmation(ExchangePosition position)
    {
        return position is not null
            && string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase)
            && string.Equals(position.Direction, ExchangePosition.DirectionShort, StringComparison.OrdinalIgnoreCase)
            && this.exchangeService.GetDeliveryStorageQuantity(position) < Math.Max(0, position.TotalQuantity);
    }

    private bool CanClaimDeliveryForUi(ExchangePosition position)
    {
        return position is not null
            && string.Equals(position.Status, ExchangePosition.StatusDelivered, StringComparison.OrdinalIgnoreCase)
            && string.Equals(position.Direction, ExchangePosition.DirectionLong, StringComparison.OrdinalIgnoreCase)
            && this.exchangeService.GetDeliveryStorageQuantity(position) > 0;
    }

    private bool ShouldShowDeliveryStorageRow(ExchangePosition position)
    {
        return position is not null
            && (string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase)
                || string.Equals(position.Status, ExchangePosition.StatusDelivered, StringComparison.OrdinalIgnoreCase)
                || string.Equals(position.Status, ExchangePosition.StatusDeliveryDefault, StringComparison.OrdinalIgnoreCase));
    }

    private int GetPositionDisplayPrice(ExchangePosition position)
    {
        ExchangeContractSpec? currentSpec = this.catalog.FirstOrDefault(spec => string.Equals(spec.MarketCommodityKey, position.MarketCommodityKey, StringComparison.OrdinalIgnoreCase));
        if (currentSpec is not null && currentSpec.MarketUnitPrice > 0)
            return currentSpec.MarketUnitPrice;

        if (position.CurrentPrice > 0)
            return position.CurrentPrice;

        return position.LastSettlementPrice > 0 ? position.LastSettlementPrice : position.OpenPrice;
    }

    private int CalculateUnrealizedProfit(ExchangePosition position, int currentPrice)
    {
        int delta = currentPrice - position.OpenPrice;
        if (string.Equals(position.Direction, ExchangePosition.DirectionShort, StringComparison.OrdinalIgnoreCase))
            delta = -delta;

        return delta * Math.Max(0, position.TotalQuantity);
    }

    private string FormatDirection(string direction)
    {
        return string.Equals(direction, ExchangePosition.DirectionShort, StringComparison.OrdinalIgnoreCase)
            ? I18n.Get("exchange.short")
            : I18n.Get("exchange.long");
    }

    private int GetDaysLeft(ExchangePosition position)
    {
        if (position is null || position.ExpiryDateIndex <= 0)
            return 0;

        return Math.Max(0, position.ExpiryDateIndex - GetCurrentDateIndexForUi());
    }

    private string FormatPositionLifecycleStatus(ExchangePosition position, int daysLeft)
    {
        if (string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase))
            return I18n.Get("exchange.status_pending_delivery");

        if (position.IsOpenLike() && daysLeft <= 0)
            return I18n.Get("exchange.status_pending_delivery");

        return this.FormatPositionStatus(position.Status);
    }

    private string FormatPositionStatus(string status)
    {
        if (string.Equals(status, ExchangePosition.StatusMarginCall, StringComparison.OrdinalIgnoreCase))
            return I18n.Get("exchange.status_margin_call");
        if (string.Equals(status, ExchangePosition.StatusClosed, StringComparison.OrdinalIgnoreCase))
            return I18n.Get("exchange.status_closed");
        if (string.Equals(status, ExchangePosition.StatusForcedLiquidated, StringComparison.OrdinalIgnoreCase))
            return I18n.Get("exchange.status_forced_liquidated");
        if (string.Equals(status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase))
            return I18n.Get("exchange.status_pending_delivery");
        if (string.Equals(status, ExchangePosition.StatusDelivered, StringComparison.OrdinalIgnoreCase))
            return I18n.Get("exchange.status_delivered");
        if (string.Equals(status, ExchangePosition.StatusDeliveryDefault, StringComparison.OrdinalIgnoreCase))
            return I18n.Get("exchange.status_delivery_default");

        return I18n.Get("exchange.status_open");
    }

    private Color GetPositionStatusColor(ExchangePosition position, int daysLeft)
    {
        if (string.Equals(position.Status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase)
            || (position.IsOpenLike() && daysLeft <= 0))
            return Color.Orange;

        return this.GetStatusColor(position.Status);
    }

    private Color GetStatusColor(string status)
    {
        if (string.Equals(status, ExchangePosition.StatusMarginCall, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ExchangePosition.StatusPendingDelivery, StringComparison.OrdinalIgnoreCase))
            return Color.Orange;
        if (string.Equals(status, ExchangePosition.StatusForcedLiquidated, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ExchangePosition.StatusDeliveryDefault, StringComparison.OrdinalIgnoreCase))
            return Color.Red;
        if (string.Equals(status, ExchangePosition.StatusClosed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ExchangePosition.StatusDelivered, StringComparison.OrdinalIgnoreCase))
            return Game1.textColor * 0.65f;

        return Game1.textColor;
    }

    private string FormatDateIndex(int dateIndex)
    {
        if (dateIndex <= 0)
            return "-";

        string[] seasons = { "spring", "summer", "fall", "winter" };
        int zeroBased = Math.Max(0, dateIndex - 1);
        int year = zeroBased / 112 + 1;
        int dayOfYear = zeroBased % 112;
        string season = seasons[Math.Clamp(dayOfYear / 28, 0, seasons.Length - 1)];
        int day = dayOfYear % 28 + 1;

        return I18n.Date(year, season, day);
    }

    private static int GetCurrentDateIndexForUi()
    {
        int seasonIndex = Game1.currentSeason switch
        {
            "spring" => 0,
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };

        return ((Game1.year - 1) * 112) + (seasonIndex * 28) + Math.Max(1, Game1.dayOfMonth);
    }

    private string TrimToWidth(string text, int maxWidth)
    {
        if (Game1.smallFont.MeasureString(text).X <= maxWidth)
            return text;

        string suffix = "…";
        string candidate = text;
        while (candidate.Length > 0 && Game1.smallFont.MeasureString(candidate + suffix).X > maxWidth)
            candidate = candidate[..^1];

        return string.IsNullOrEmpty(candidate) ? suffix : candidate + suffix;
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
        // In Reality Check 1.4, the displayed risk line is the 12% maintenance-margin line.
        // With 20% initial margin and 12% maintenance margin, the price gap from open is 8%.
        return CalculateWarningPrice(openPrice, direction);
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
