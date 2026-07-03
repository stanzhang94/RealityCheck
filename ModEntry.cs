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
    private OffSeasonFactorService? offSeasonFactorService;
    private MarketTrendService? marketTrendService;
    private ArtisanIdentityService? artisanIdentityService;
    private MarketCategoryResolver? marketCategoryResolver;
    private TaxEvents taxEvents = null!;
    private TaxNoticeMailRouter? taxNoticeMailRouter;
    private ExchangeService? exchangeService;
    private ExchangeContractCatalogService? exchangeContractCatalogService;

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
        this.offSeasonFactorService = new OffSeasonFactorService();

        this.marketTrendService = new MarketTrendService(
            helper,
            this.Monitor
        );

        this.marketTrendService.Load();

        this.marketPriceService = new MarketPriceService(
            this.configService,
            this.marketCategoryResolver,
            this.weatherFactorService,
            this.festivalFactorService,
            this.offSeasonFactorService,
            this.marketTrendService,
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

        this.exchangeService = new ExchangeService(
            helper,
            this.Monitor,
            this.ledgerService,
            this.marketPriceService
        );

        this.exchangeContractCatalogService = new ExchangeContractCatalogService(
            this.marketPriceService,
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

        TooltipMarketPricePatch.Initialize(
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
        TooltipMarketPricePatch.Apply(harmony);
        ShippingSettlementTracePatch.Apply(harmony);

        helper.ConsoleCommands.Add(
            "rc_exchange_status",
            "Shows the Reality Check exchange account debug status.",
            this.OnExchangeStatusCommand
        );

        helper.ConsoleCommands.Add(
            "rc_exchange_deposit",
            "Deposits gold from the farm wallet into the Reality Check exchange account. Usage: rc_exchange_deposit <amount>",
            this.OnExchangeDepositCommand
        );

        helper.ConsoleCommands.Add(
            "rc_exchange_withdraw",
            "Withdraws available gold from the Reality Check exchange account back to the farm wallet. Usage: rc_exchange_withdraw <amount>",
            this.OnExchangeWithdrawCommand
        );

        helper.ConsoleCommands.Add(
            "rc_exchange_catalog",
            "Shows a preview of tradable Reality Check exchange contracts.",
            this.OnExchangeCatalogCommand
        );

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
        helper.Events.GameLoop.DayStarted += this.OnMarketPriceDayStarted;
        helper.Events.GameLoop.DayStarted += this.OnExchangeDayStarted;
        helper.Events.GameLoop.DayStarted += this.taxEvents.OnDayStarted;

        helper.Events.Display.MenuChanged += this.taxNoticeMailRouter.OnMenuChanged;
        helper.Events.GameLoop.UpdateTicked += this.taxNoticeMailRouter.OnUpdateTicked;

        this.Monitor.Log(
            "Reality Check loaded.",
            LogLevel.Info
        );
    }


    private void OnExchangeStatusCommand(
        string command,
        string[] args
    )
    {
        if (!Context.IsWorldReady || this.exchangeService is null)
        {
            this.Monitor.Log(
                "Load a save before using exchange commands.",
                LogLevel.Warn
            );
            return;
        }

        this.Monitor.Log(
            this.exchangeService.GetDebugStatus(),
            LogLevel.Info
        );
    }

    private void OnExchangeDepositCommand(
        string command,
        string[] args
    )
    {
        if (!this.TryParseExchangeAmount(
            args,
            out int amount
        ))
        {
            this.Monitor.Log(
                "Usage: rc_exchange_deposit <amount>",
                LogLevel.Warn
            );
            return;
        }

        this.RunExchangeTransferCommand(
            amount,
            deposit: true
        );
    }

    private void OnExchangeWithdrawCommand(
        string command,
        string[] args
    )
    {
        if (!this.TryParseExchangeAmount(
            args,
            out int amount
        ))
        {
            this.Monitor.Log(
                "Usage: rc_exchange_withdraw <amount>",
                LogLevel.Warn
            );
            return;
        }

        this.RunExchangeTransferCommand(
            amount,
            deposit: false
        );
    }

    private void OnExchangeCatalogCommand(
        string command,
        string[] args
    )
    {
        if (!Context.IsWorldReady || this.exchangeContractCatalogService is null)
        {
            this.Monitor.Log(
                "Load a save before using exchange commands.",
                LogLevel.Warn
            );
            return;
        }

        this.Monitor.Log(
            this.exchangeContractCatalogService.GetDebugSummary(),
            LogLevel.Info
        );
    }

    private bool TryParseExchangeAmount(
        string[] args,
        out int amount
    )
    {
        amount = 0;

        return args.Length == 1
            && int.TryParse(
                args[0],
                out amount
            )
            && amount > 0;
    }

    private void RunExchangeTransferCommand(
        int amount,
        bool deposit
    )
    {
        if (!Context.IsWorldReady || this.exchangeService is null)
        {
            this.Monitor.Log(
                "Load a save before using exchange commands.",
                LogLevel.Warn
            );
            return;
        }

        string message;
        bool ok;

        if (deposit)
        {
            ok = this.exchangeService.TryDeposit(
                amount,
                out message
            );
        }
        else
        {
            ok = this.exchangeService.TryWithdraw(
                amount,
                out message
            );
        }

        this.Monitor.Log(
            message,
            ok ? LogLevel.Info : LogLevel.Warn
        );

        if (ok)
            Game1.showGlobalMessage(message);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.ledgerService?.Load();
        this.exchangeService?.Load();
        this.marketTrendService?.Load();
        this.marketPriceService?.UpdateAllMarketPricesForToday(this.ledgerService?.GetEntries());
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.ledgerService?.Save();
        this.exchangeService?.Save();
        this.marketTrendService?.Save();
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
            this.marketPriceService,
            this.exchangeService,
            this.exchangeContractCatalogService
        );

        Game1.playSound("bigSelect");
    }


    private void OnMarketPriceDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.marketPriceService?.UpdateAllMarketPricesForToday(this.ledgerService?.GetEntries());
    }


    private void OnExchangeDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.exchangeService?.ProcessDailySettlement();
    }

    private void OnShippingTraceDayStarted(object? sender, DayStartedEventArgs e)
    {
        ShippingSettlementTracePatch.EndTraceWindow("SMAPI DayStarted");
    }
}
