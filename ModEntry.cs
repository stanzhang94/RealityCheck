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
    private HealthInsuranceNoticeService? healthInsuranceNoticeService;
    private ConfigService? configService;
    private MarketPriceService? marketPriceService;
    private WeatherFactorService? weatherFactorService;
    private FestivalFactorService? festivalFactorService;
    private ArtisanIdentityService? artisanIdentityService;
    private MarketCategoryResolver? marketCategoryResolver;
    private TaxEvents taxEvents = null!;
    private TaxNoticeMailRouter? taxNoticeMailRouter;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper);

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

        this.artisanIdentityService = new ArtisanIdentityService(
            this.Monitor
        );

        this.marketCategoryResolver = new MarketCategoryResolver(
            this.artisanIdentityService
        );

        this.weatherFactorService = new WeatherFactorService();
        this.festivalFactorService = new FestivalFactorService();

        this.marketPriceService = new MarketPriceService(
            this.configService,
            this.marketCategoryResolver,
            this.weatherFactorService,
            this.festivalFactorService,
            this.Monitor
        );

        this.incomeEvents = new IncomeEvents(
            this.ledgerService,
            this.marketPriceService,
            this.artisanIdentityService,
            this.Monitor
        );

        this.healthInsuranceNoticeService = new HealthInsuranceNoticeService(
            this.ledgerService,
            helper,
            this.Monitor
        );

        this.expenseEvents = new ExpenseEvents(
            this.ledgerService,
            this.healthInsuranceNoticeService,
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
            this.artisanIdentityService,
            this.Monitor
        );

        ShopSaleMarketPricePatch.Initialize(
            this.marketPriceService,
            this.Monitor
        );

        ShippingSettlementTracePatch.Initialize(
            this.Monitor,
            this.configService
        );

        var harmony = new Harmony(
            this.ModManifest.UniqueID
        );

        ShopSalePatch.Apply(harmony);
        ShopSaleMarketPricePatch.Apply(harmony);
        ShippingSettlementTracePatch.Apply(harmony);

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.Saving += this.OnSaving;

        helper.Events.GameLoop.DayEnding += this.incomeEvents.OnDayEnding;

        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;

        helper.Events.GameLoop.SaveLoaded += this.expenseEvents.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.expenseEvents.OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += this.expenseEvents.OnUpdateTicked;
        helper.Events.GameLoop.DayEnding += this.expenseEvents.OnDayEnding;

        helper.Events.Content.AssetRequested += this.healthInsuranceNoticeService.OnAssetRequested;

        helper.Events.GameLoop.DayStarted += this.OnShippingTraceDayStarted;
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

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is not null)
            return;

        if (this.configService?.Config.OpenReportKey.JustPressed() != true)
            return;

        if (this.ledgerService is null || this.analyticsService is null || this.marketPriceService is null)
            return;

        Game1.activeClickableMenu = new FinanceMenu(
            this.ledgerService,
            this.analyticsService,
            this.marketPriceService
        );

        Game1.playSound("bigSelect");
    }

    private void OnShippingTraceDayStarted(object? sender, DayStartedEventArgs e)
    {
        ShippingSettlementTracePatch.EndTraceWindow("SMAPI DayStarted");
    }
}
