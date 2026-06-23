using HarmonyLib;
using RealityCheck.Events;
using RealityCheck.Patches;
using RealityCheck.Services;
using RealityCheck.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck;

public class ModEntry : Mod
{
    private LedgerService? ledgerService;
    private AnalyticsService? analyticsService;
    private IncomeEvents? incomeEvents;
    private ExpenseEvents? expenseEvents;
    private ConfigService? configService;
    private TaxEvents taxEvents = null!;
    private TaxNoticeMailRouter? taxNoticeMailRouter;

    public override void Entry(IModHelper helper)
    {
        this.configService = new ConfigService(
            helper,
            this.Monitor
        );

        this.configService.Load();

        this.ledgerService = new LedgerService(
            helper,
            this.Monitor
        );

        this.analyticsService = new AnalyticsService(
            this.ledgerService
        );

        this.incomeEvents = new IncomeEvents(
            this.ledgerService,
            this.Monitor
        );

        this.expenseEvents = new ExpenseEvents(
            this.ledgerService,
            this.Monitor
        );

        this.taxEvents = new TaxEvents(
            this.ledgerService,
            this.Helper,
            this.Monitor,
            this.configService
        );

        this.taxNoticeMailRouter = new TaxNoticeMailRouter(
            this.ledgerService,
            this.Monitor
        );

        ShopSalePatch.Initialize(
            this.ledgerService,
            this.Monitor
        );

        var harmony = new Harmony(
            this.ModManifest.UniqueID
        );

        ShopSalePatch.Apply(harmony);

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.Saving += this.OnSaving;

        helper.Events.GameLoop.DayEnding += this.incomeEvents.OnDayEnding;

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;

        helper.Events.GameLoop.SaveLoaded += this.expenseEvents.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.expenseEvents.OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += this.expenseEvents.OnUpdateTicked;
        helper.Events.GameLoop.DayEnding += this.expenseEvents.OnDayEnding;

        helper.Events.GameLoop.DayStarted += this.taxEvents.OnDayStarted;

        helper.Events.Display.MenuChanged += this.taxNoticeMailRouter.OnMenuChanged;
        helper.Events.GameLoop.UpdateTicked += this.taxNoticeMailRouter.OnUpdateTicked;

        this.Monitor.Log(
            "Reality Check loaded.",
            LogLevel.Info
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.ledgerService?.Load();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.ledgerService?.Save();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button == SButton.O)
        {
            if (this.ledgerService is null || this.analyticsService is null)
                return;

            Game1.activeClickableMenu = new FinanceMenu(
                this.ledgerService,
                this.analyticsService
            );

            Game1.playSound("bigSelect");

            return;
        }
    }
}