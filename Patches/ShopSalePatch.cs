using HarmonyLib;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.Patches;

public static class ShopSalePatch
{
    private static LedgerService? ledgerService;
    private static IMonitor? monitor;

    public static void Initialize(LedgerService service, IMonitor modMonitor)
    {
        ledgerService = service;
        monitor = modMonitor;
    }

    public static void Apply(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(ShopMenu), "AddBuybackItem");

        if (method == null)
        {
            monitor?.Log("Could not find ShopMenu.AddBuybackItem.", LogLevel.Warn);
            return;
        }

        harmony.Patch(
            original: method,
            postfix: new HarmonyMethod(typeof(ShopSalePatch), nameof(AfterAddBuybackItem))
        );

        monitor?.Log("Patched ShopMenu.AddBuybackItem.", LogLevel.Info);
    }

    private static void AfterAddBuybackItem(object[] __args)
    {
        if (ledgerService == null)
            return;

        if (__args.Length < 1)
            return;

        if (__args[0] is not ISalable item)
            return;

        string itemName = item.DisplayName;
        int quantity = 1;
        int amount = 0;

        if (__args.Length >= 2 && __args[1] is int price)
            amount = price;

        if (__args.Length >= 3 && __args[2] is int stack)
            quantity = stack;

        if (amount <= 0)
        {
            monitor?.Log($"Shop sale detected but amount was invalid: {itemName}", LogLevel.Trace);
            return;
        }

        ledgerService.AddIncome(
            "Shop Sale",
            itemName,
            quantity,
            amount
        );

        monitor?.Log($"Shop sale recorded: {itemName} x{quantity} = {amount}g", LogLevel.Info);
    }
}