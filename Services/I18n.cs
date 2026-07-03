using StardewModdingAPI;

namespace RealityCheck.Services;

public static class I18n
{
    private static ITranslationHelper? translations;

    public static void Init(IModHelper helper)
    {
        translations = helper.Translation;
    }

    public static string Get(string key)
    {
        return translations?.Get(key).ToString() ?? key;
    }

    public static string Get(string key, object tokens)
    {
        return translations?.Get(key, tokens).ToString() ?? key;
    }

    public static string Season(string season)
    {
        if (string.IsNullOrWhiteSpace(season))
            return season;

        return Get($"season.{season}");
    }

    public static string Date(int year, string season, int day)
    {
        return Get(
            "date.full",
            new
            {
                year,
                season = Season(season),
                day
            }
        );
    }

    public static string PeriodSameSeason(int year, string season, int startDay, int endDay)
    {
        return Get(
            "date.period_same_season",
            new
            {
                year,
                season = Season(season),
                startDay,
                endDay
            }
        );
    }

    public static string Category(string category)
    {
        return category switch
        {
            "Base Game Expenses" => Get("category.base_game_expenses"),
            "Health Insurance Premium" => Get("category.health_insurance_premium"),
            "Health Insurance Coverage" => Get("category.health_insurance_coverage"),
            "Medical Expenses" => Get("category.medical_expenses"),
            "Income Tax" => Get("category.income_tax"),
            "Property Tax" => Get("category.property_tax"),
            "Business Property Tax" => Get("category.business_property_tax"),
            "Expense Offset" => Get("category.expense_offset"),
            "Unpaid Obligation" => Get("category.unpaid_obligation"),
            "Outstanding Balance" => Get("category.outstanding_balance"),
            "Exchange Transfer" => Get("category.exchange_transfer"),
            "Transfer to Exchange Account" => Get("category.exchange_transfer_to"),
            "Transfer from Exchange Account" => Get("category.exchange_transfer_from"),
            "Exchange Debt" => Get("category.exchange_debt"),
            "Exchange Debt Collection" => Get("category.exchange_debt_collection"),
            _ => category
        };
    }

    public static string Machine(string machineName)
    {
        return machineName switch
        {
            "Keg" => Get("machine.keg"),
            "Preserves Jar" => Get("machine.preserves_jar"),
            "Cask" => Get("machine.cask"),
            "Bee House" => Get("machine.bee_house"),
            "Mayonnaise Machine" => Get("machine.mayonnaise_machine"),
            "Cheese Press" => Get("machine.cheese_press"),
            "Loom" => Get("machine.loom"),
            "Oil Maker" => Get("machine.oil_maker"),
            "Dehydrator" => Get("machine.dehydrator"),
            "Fish Smoker" => Get("machine.fish_smoker"),
            _ => machineName
        };
    }
}
