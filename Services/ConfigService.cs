using RealityCheck.Data;
using StardewModdingAPI;

namespace RealityCheck.Services;

public class ConfigService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    public ModConfig Config { get; private set; } = new();

    public ConfigService(
        IModHelper helper,
        IMonitor monitor
    )
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public void Load()
    {
        this.Config = this.helper.ReadConfig<ModConfig>();

        this.EnsureDefaults();

        this.helper.WriteConfig(this.Config);

        this.monitor.Log(
            "Reality Check config loaded.",
            LogLevel.Trace
        );
    }

    private void EnsureDefaults()
    {
        this.Config ??= new ModConfig();
        this.Config.Tax ??= new TaxConfig();
        this.Config.Tax.BusinessPropertyDailyTaxRates ??= new BusinessPropertyDailyTaxRates();
        this.Config.Tax.IncomeTaxBrackets ??= new();
        this.Config.Tax.PropertyTax ??= new PropertyTaxConfig();

        if (this.Config.Tax.IncomeTaxBrackets.Count == 0)
        {
            this.Config.Tax.IncomeTaxBrackets.Add(
                new IncomeTaxBracketConfig
                {
                    MinimumTaxableIncome = 0,
                    Rate = 0.00
                }
            );
        }
    }
}