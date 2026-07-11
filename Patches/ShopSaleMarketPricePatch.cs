using System;
using System.Collections.Generic;
using HarmonyLib;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.Patches;

public static class ShopSaleMarketPricePatch
{
    [ThreadStatic]
    private static int shopReceiveClickDepth;

    [ThreadStatic]
    private static List<ShopSalePriceContext>? pendingPriceContexts;

    private static MarketPriceService? marketPriceService;
    private static IMonitor? monitor;

    public static void Initialize(
        MarketPriceService service,
        IMonitor modMonitor
    )
    {
        marketPriceService = service;
        monitor = modMonitor;
    }

    public static void Apply(Harmony harmony)
    {
        int patched = 0;
        int failed = 0;

        if (TryPatchShopClick(harmony, nameof(ShopMenu.receiveLeftClick)))
            patched++;
        else
            failed++;

        if (TryPatchShopClick(harmony, nameof(ShopMenu.receiveRightClick)))
            patched++;
        else
            failed++;

        if (TryPatchObjectSellToStorePrice(harmony))
            patched++;
        else
            failed++;

        monitor?.Log(
            $"[Shop Market] Installed shop sale market price patches: patched={patched}, failed={failed}.",
            failed > 0 ? LogLevel.Warn : LogLevel.Trace
        );
    }

    public static void ShopClickPrefix()
    {
        shopReceiveClickDepth++;

        if (shopReceiveClickDepth == 1)
            pendingPriceContexts = new List<ShopSalePriceContext>();
    }

    public static void ShopClickPostfix()
    {
        if (shopReceiveClickDepth > 0)
            shopReceiveClickDepth--;

        if (shopReceiveClickDepth == 0)
            pendingPriceContexts = null;
    }

    public static void ObjectSellToStorePricePostfix(
        StardewValley.Object __instance,
        long specificPlayerID,
        ref int __result
    )
    {
        if (shopReceiveClickDepth <= 0)
            return;

        if (marketPriceService is null)
            return;

        if (marketPriceService.IsVanillaPriceProbeActive())
            return;

        int vanillaUnitPrice = __result;
        int marketUnitPrice = marketPriceService.GetShopSaleMarketUnitPrice(
            __instance,
            vanillaUnitPrice,
            specificPlayerID
        );

        pendingPriceContexts ??= new List<ShopSalePriceContext>();
        pendingPriceContexts.Add(
            new ShopSalePriceContext(
                __instance.QualifiedItemId ?? string.Empty,
                __instance.DisplayName ?? string.Empty,
                vanillaUnitPrice,
                marketUnitPrice
            )
        );

        if (marketUnitPrice == vanillaUnitPrice)
            return;

        __result = marketUnitPrice;

        monitor?.Log(
            $"[Shop Market] Shop sale unit price changed: {__instance.DisplayName} {vanillaUnitPrice}g -> {marketUnitPrice}g.",
            LogLevel.Info
        );
    }

    public static int ConsumeBaseUnitPriceForShopSale(
        Item item,
        int fallbackUnitPrice
    )
    {
        if (pendingPriceContexts is null || pendingPriceContexts.Count == 0)
            return Math.Max(0, fallbackUnitPrice);

        string itemId = item.QualifiedItemId ?? string.Empty;
        string displayName = item.DisplayName ?? string.Empty;

        for (int i = pendingPriceContexts.Count - 1; i >= 0; i--)
        {
            ShopSalePriceContext context = pendingPriceContexts[i];

            bool idMatches = string.Equals(
                context.ItemId,
                itemId,
                StringComparison.OrdinalIgnoreCase
            );

            bool nameMatches = string.Equals(
                context.DisplayName,
                displayName,
                StringComparison.CurrentCulture
            );

            bool priceMatches = context.MarketUnitPrice == fallbackUnitPrice
                || context.BaseUnitPrice == fallbackUnitPrice;

            if (!idMatches || !nameMatches || !priceMatches)
                continue;

            pendingPriceContexts.RemoveAt(i);
            return Math.Max(0, context.BaseUnitPrice);
        }

        return Math.Max(0, fallbackUnitPrice);
    }

    private readonly record struct ShopSalePriceContext(
        string ItemId,
        string DisplayName,
        int BaseUnitPrice,
        int MarketUnitPrice
    );

    private static bool TryPatchShopClick(
        Harmony harmony,
        string methodName
    )
    {
        var method = AccessTools.Method(
            typeof(ShopMenu),
            methodName,
            new[]
            {
                typeof(int),
                typeof(int),
                typeof(bool)
            }
        );

        if (method is null)
        {
            monitor?.Log(
                $"[Shop Market] Could not find ShopMenu.{methodName}(int,int,bool).",
                LogLevel.Warn
            );
            return false;
        }

        try
        {
            harmony.Patch(
                original: method,
                prefix: new HarmonyMethod(typeof(ShopSaleMarketPricePatch), nameof(ShopClickPrefix)),
                postfix: new HarmonyMethod(typeof(ShopSaleMarketPricePatch), nameof(ShopClickPostfix))
            );

            return true;
        }
        catch (Exception ex)
        {
            monitor?.Log(
                $"[Shop Market] Failed to patch ShopMenu.{methodName}: {ex.GetType().Name}: {ex.Message}",
                LogLevel.Warn
            );
            return false;
        }
    }

    private static bool TryPatchObjectSellToStorePrice(Harmony harmony)
    {
        var method = AccessTools.Method(
            typeof(StardewValley.Object),
            nameof(StardewValley.Object.sellToStorePrice),
            new[] { typeof(long) }
        );

        if (method is null)
        {
            monitor?.Log(
                "[Shop Market] Could not find Object.sellToStorePrice(long).",
                LogLevel.Warn
            );
            return false;
        }

        try
        {
            harmony.Patch(
                original: method,
                postfix: new HarmonyMethod(typeof(ShopSaleMarketPricePatch), nameof(ObjectSellToStorePricePostfix))
            );

            return true;
        }
        catch (Exception ex)
        {
            monitor?.Log(
                $"[Shop Market] Failed to patch Object.sellToStorePrice(long): {ex.GetType().Name}: {ex.Message}",
                LogLevel.Warn
            );
            return false;
        }
    }
}
