using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.Patches;

public static class TooltipMarketPricePatch
{
    [ThreadStatic]
    private static Stack<StardewValley.Object?>? tooltipObjectStack;

    [ThreadStatic]
    private static int marketPriceLookupDepth;

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

        int tooltipMethodCount = PatchDrawToolTipMethods(harmony);
        if (tooltipMethodCount > 0)
            patched += tooltipMethodCount;
        else
            failed++;


        if (TryPatchObjectSellToStorePrice(harmony))
            patched++;
        else
            failed++;

        monitor?.Log(
            $"[Tooltip Market] Installed tooltip market price patches: patched={patched}, failed={failed}.",
            failed > 0 ? LogLevel.Warn : LogLevel.Trace
        );
    }

    public static void DrawToolTipPrefix(
        object[] __args
    )
    {
        StardewValley.Object? hoveredObject = null;

        foreach (object? arg in __args)
        {
            if (arg is StardewValley.Object obj)
            {
                hoveredObject = obj;
                break;
            }
        }

        tooltipObjectStack ??= new Stack<StardewValley.Object?>();
        tooltipObjectStack.Push(hoveredObject);
    }

    public static void DrawToolTipFinalizer()
    {
        if (tooltipObjectStack is null || tooltipObjectStack.Count == 0)
            return;

        tooltipObjectStack.Pop();

        if (tooltipObjectStack.Count == 0)
            tooltipObjectStack = null;
    }


    public static void ObjectSellToStorePricePostfix(
        StardewValley.Object __instance,
        ref int __result
    )
    {
        ReplaceTooltipPriceIfNeeded(
            __instance,
            ref __result,
            "sellToStorePrice"
        );
    }

    private static void ReplaceTooltipPriceIfNeeded(
        StardewValley.Object item,
        ref int result,
        string source
    )
    {
        if (!IsCurrentTooltipObject(item))
            return;

        if (marketPriceService is null)
            return;

        int vanillaUnitPrice = Math.Max(0, result);
        int marketUnitPrice = GetMarketUnitPriceSafely(
            item,
            vanillaUnitPrice,
            source
        );

        if (marketUnitPrice == vanillaUnitPrice)
            return;

        result = marketUnitPrice;

        monitor?.Log(
            $"[Tooltip Market] {source} changed for {item.DisplayName}: {vanillaUnitPrice}g -> {marketUnitPrice}g.",
            LogLevel.Trace
        );
    }

    private static bool IsCurrentTooltipObject(
        StardewValley.Object item
    )
    {
        if (tooltipObjectStack is null || tooltipObjectStack.Count == 0)
            return false;

        StardewValley.Object? currentTooltipObject = tooltipObjectStack.Peek();

        if (currentTooltipObject is null)
            return false;

        return ReferenceEquals(currentTooltipObject, item);
    }

    private static int GetMarketUnitPriceSafely(
        StardewValley.Object item,
        int vanillaUnitPrice,
        string source
    )
    {
        if (marketPriceService is null)
            return Math.Max(0, vanillaUnitPrice);

        if (marketPriceLookupDepth > 0)
            return Math.Max(0, vanillaUnitPrice);

        try
        {
            marketPriceLookupDepth++;
            return marketPriceService.GetShopSaleMarketUnitPrice(
                item,
                Math.Max(0, vanillaUnitPrice)
            );
        }
        catch (Exception ex)
        {
            monitor?.Log(
                $"[Tooltip Market] Failed to calculate market tooltip price through {source} for {item.DisplayName}: {ex.GetType().Name}: {ex.Message}",
                LogLevel.Warn
            );

            return Math.Max(0, vanillaUnitPrice);
        }
        finally
        {
            if (marketPriceLookupDepth > 0)
                marketPriceLookupDepth--;
        }
    }

    private static int PatchDrawToolTipMethods(Harmony harmony)
    {
        List<MethodInfo> methods = AccessTools.GetDeclaredMethods(typeof(IClickableMenu))
            .Where(method => string.Equals(method.Name, "drawToolTip", StringComparison.Ordinal))
            .Where(HasItemArgument)
            .Distinct()
            .ToList();

        int patched = 0;

        foreach (MethodInfo method in methods)
        {
            try
            {
                harmony.Patch(
                    original: method,
                    prefix: new HarmonyMethod(typeof(TooltipMarketPricePatch), nameof(DrawToolTipPrefix)),
                    finalizer: new HarmonyMethod(typeof(TooltipMarketPricePatch), nameof(DrawToolTipFinalizer))
                );

                patched++;
            }
            catch (Exception ex)
            {
                monitor?.Log(
                    $"[Tooltip Market] Failed to patch IClickableMenu.drawToolTip overload: {ex.GetType().Name}: {ex.Message}",
                    LogLevel.Warn
                );
            }
        }

        if (patched <= 0)
        {
            monitor?.Log(
                "[Tooltip Market] Could not find a patchable IClickableMenu.drawToolTip overload with an Item argument.",
                LogLevel.Warn
            );
        }
        else
        {
            monitor?.Log(
                $"[Tooltip Market] Patched IClickableMenu.drawToolTip overloads: {patched}/{methods.Count}.",
                patched == methods.Count ? LogLevel.Trace : LogLevel.Warn
            );
        }

        return patched;
    }

    private static bool HasItemArgument(MethodInfo method)
    {
        return method.GetParameters()
            .Any(parameter => typeof(Item).IsAssignableFrom(parameter.ParameterType));
    }

    private static bool TryPatchObjectSellToStorePrice(Harmony harmony)
    {
        MethodInfo? method = AccessTools.Method(
            typeof(StardewValley.Object),
            nameof(StardewValley.Object.sellToStorePrice),
            new[] { typeof(long) }
        );

        if (method is null)
        {
            monitor?.Log(
                "[Tooltip Market] Could not find Object.sellToStorePrice(long).",
                LogLevel.Warn
            );
            return false;
        }

        try
        {
            harmony.Patch(
                original: method,
                postfix: new HarmonyMethod(typeof(TooltipMarketPricePatch), nameof(ObjectSellToStorePricePostfix))
            );

            return true;
        }
        catch (Exception ex)
        {
            monitor?.Log(
                $"[Tooltip Market] Failed to patch Object.sellToStorePrice(long): {ex.GetType().Name}: {ex.Message}",
                LogLevel.Warn
            );
            return false;
        }
    }
}
