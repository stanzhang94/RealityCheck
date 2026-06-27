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
        string itemId = __args[0] is Item soldItem ? soldItem.QualifiedItemId : "";

        int quantity = 1;
        int unitPrice = 0;

        if (__args.Length >= 2 && __args[1] is int price)
            unitPrice = price;

        if (__args.Length >= 3 && __args[2] is int stack && stack > 0)
            quantity = stack;

        int amount = unitPrice * quantity;

        if (amount <= 0)
        {
            monitor?.Log(
                $"Shop sale detected but amount was invalid: {itemName}, unitPrice={unitPrice}, quantity={quantity}",
                LogLevel.Trace
            );
            return;
        }

        string transactionId = $"shop_{Game1.year}_{Game1.currentSeason}_{Game1.dayOfMonth}_{Guid.NewGuid():N}";

        ledgerService.SuppressNextIncomeAmount(
            amount,
            reason: "KnownShopSaleIncome",
            source: "Shop Sale",
            transactionId: transactionId
        );

        ledgerService.AddIncome(
            "Shop Sale",
            itemName,
            quantity,
            amount,
            itemId,
            dataOrigin: "KnownShopSale",
            transactionId: transactionId
        );

        monitor?.Log(
            $"Shop sale recorded: {itemName} x{quantity} = {amount}g",
            LogLevel.Info
        );
    }
}
