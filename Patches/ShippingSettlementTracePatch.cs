using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RealityCheck.Services;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace RealityCheck.Patches;

public static class ShippingSettlementTracePatch
{
    private static readonly HashSet<string> PatchedMethodKeys = new();

    private static IMonitor? monitor;
    private static ConfigService? configService;
    private static bool traceWindowOpen;
    private static int dayEndingMoney;
    private static int vanillaShippingEstimate;
    private static int marketShippingEstimate;
    private static int focusedCallCount;
    private static int focusedInsideMoneyChangeCount;
    private static int moneySetterTraceCount;
    private static int shippingPayoutInterceptCount;

    public static void Initialize(
        IMonitor monitor,
        ConfigService configService
    )
    {
        ShippingSettlementTracePatch.monitor = monitor;
        ShippingSettlementTracePatch.configService = configService;
    }

    public static void Apply(Harmony harmony)
    {
        int patched = 0;
        int failed = 0;

        if (TryPatchMoneySetter(harmony))
            patched++;
        else
            failed++;

        if (IsVerboseTraceEnabled())
        {
            foreach (MethodBase method in FindFocusedMethods())
            {
                string key = GetMethodKey(method);

                if (!PatchedMethodKeys.Add(key))
                    continue;

                try
                {
                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(ShippingSettlementTracePatch), nameof(FocusedPrefix)),
                        postfix: new HarmonyMethod(typeof(ShippingSettlementTracePatch), nameof(FocusedPostfix))
                    );

                    patched++;
                }
                catch (Exception ex)
                {
                    failed++;
                    monitor?.Log(
                        $"[Shipping Trace] Failed to patch focused candidate method {key}: {ex.GetType().Name}: {ex.Message}",
                        LogLevel.Trace
                    );
                }
            }
        }

        monitor?.Log(
            $"[Shipping Intercept] Shipping payout interceptor installed: patched={patched}, failed={failed}.",
            failed > 0 ? LogLevel.Warn : LogLevel.Trace
        );
    }

    public static void BeginTraceWindow(
        int vanillaEstimate,
        int marketEstimate
    )
    {
        if (!ShouldOpenWindow())
            return;

        if (!Context.IsWorldReady || Game1.player is null)
            return;

        traceWindowOpen = true;
        dayEndingMoney = Game1.player.Money;
        vanillaShippingEstimate = vanillaEstimate;
        marketShippingEstimate = marketEstimate;
        focusedCallCount = 0;
        focusedInsideMoneyChangeCount = 0;
        moneySetterTraceCount = 0;
        shippingPayoutInterceptCount = 0;

        if (IsVerboseTraceEnabled())
        {
            monitor?.Log(
                $"[Shipping Trace] Trace window opened at DayEnding. Player money={dayEndingMoney}g, vanilla shipping estimate={vanillaShippingEstimate}g, market shipping estimate={marketShippingEstimate}g, diff={marketShippingEstimate - vanillaShippingEstimate:+#;-#;0}g.",
                LogLevel.Info
            );
        }
        else if (IsMarketSettlementEnabled() && vanillaShippingEstimate > 0)
        {
            monitor?.Log(
                $"[Shipping Intercept] Pending shipping market settlement: vanilla {vanillaShippingEstimate}g -> market {marketShippingEstimate}g (diff {marketShippingEstimate - vanillaShippingEstimate:+#;-#;0}g).",
                LogLevel.Info
            );
        }
    }

    public static void EndTraceWindow(string reason)
    {
        if (!traceWindowOpen)
            return;

        int currentMoney = Context.IsWorldReady && Game1.player is not null
            ? Game1.player.Money
            : dayEndingMoney;

        if (IsVerboseTraceEnabled())
        {
            monitor?.Log(
                $"[Shipping Trace] Trace window closed at {reason}. Player money {dayEndingMoney}g -> {currentMoney}g (delta {currentMoney - dayEndingMoney:+#;-#;0}g). Vanilla estimate={vanillaShippingEstimate}g, market estimate={marketShippingEstimate}g. Focused calls={focusedCallCount}, inside-call money changes={focusedInsideMoneyChangeCount}, money setter traces={moneySetterTraceCount}, shipping payout intercepts={shippingPayoutInterceptCount}.",
                LogLevel.Info
            );
        }
        else if (IsMarketSettlementEnabled() && vanillaShippingEstimate > 0)
        {
            if (shippingPayoutInterceptCount > 0)
            {
                monitor?.Log(
                    $"[Shipping Intercept] Shipping market settlement completed: vanilla {vanillaShippingEstimate}g -> market {marketShippingEstimate}g. Intercepts={shippingPayoutInterceptCount}.",
                    LogLevel.Info
                );
            }
            else
            {
                monitor?.Log(
                    $"[Shipping Intercept] WARNING: Market settlement was enabled, but the vanilla shipping payout was not intercepted. Ledger/tax may not match actual player money. Disable EnableShippingBinMarketSettlement or enable EnableShippingSettlementVerboseTrace for diagnosis.",
                    LogLevel.Error
                );
            }
        }

        traceWindowOpen = false;
    }

    public static void FocusedPrefix(MethodBase __originalMethod, out int __state)
    {
        __state = SafeMoney();

        if (!ShouldVerboseTrace())
            return;

        focusedCallCount++;

        monitor?.Log(
            $"[Shipping Trace] FOCUS ENTER {GetMethodKey(__originalMethod)} moneyBefore={__state}g",
            LogLevel.Info
        );
    }

    public static void FocusedPostfix(MethodBase __originalMethod, int __state, object? __result)
    {
        if (!ShouldVerboseTrace())
            return;

        int afterMoney = SafeMoney();
        string resultDescription = DescribeResult(__result);

        if (afterMoney != __state)
        {
            focusedInsideMoneyChangeCount++;

            monitor?.Log(
                $"[Shipping Trace] FOCUS MONEY CHANGED during {GetMethodKey(__originalMethod)}: {__state}g -> {afterMoney}g (delta {afterMoney - __state:+#;-#;0}g). Result={resultDescription}",
                LogLevel.Warn
            );
        }
        else
        {
            monitor?.Log(
                $"[Shipping Trace] FOCUS EXIT {GetMethodKey(__originalMethod)} moneyUnchanged={afterMoney}g. Result={resultDescription}",
                LogLevel.Info
            );
        }
    }

    public static void MoneySetterPrefix(Farmer __instance, ref int value)
    {
        if (!ShouldMonitorForFarmer(__instance))
            return;

        int oldValue = __instance.Money;

        if (oldValue == value)
            return;

        int originalDelta = value - oldValue;

        if (IsMarketSettlementEnabled()
            && shippingPayoutInterceptCount == 0
            && vanillaShippingEstimate > 0
            && marketShippingEstimate >= 0
            && originalDelta == vanillaShippingEstimate
            && IsLikelyOriginalShippingPayoutCaller())
        {
            int marketValue = oldValue + marketShippingEstimate;
            int marketDelta = marketValue - oldValue;

            value = marketValue;
            shippingPayoutInterceptCount++;
            moneySetterTraceCount++;

            monitor?.Log(
                $"[Shipping Intercept] Shipping payout intercepted: vanilla {originalDelta:+#;-#;0}g -> market {marketDelta:+#;-#;0}g (diff {marketDelta - originalDelta:+#;-#;0}g). Money {oldValue}g -> {value}g.",
                LogLevel.Info
            );

            if (IsVerboseTraceEnabled())
            {
                monitor?.Log(
                    $"[Shipping Trace] Shipping payout caller stack: {BuildCallerStack()}",
                    LogLevel.Info
                );
            }

            return;
        }

        if (IsVerboseTraceEnabled())
        {
            moneySetterTraceCount++;

            monitor?.Log(
                $"[Shipping Trace] PLAYER MONEY SETTER: {oldValue}g -> {value}g (delta {originalDelta:+#;-#;0}g). Caller stack: {BuildCallerStack()}",
                LogLevel.Warn
            );
        }
    }

    private static bool IsLikelyOriginalShippingPayoutCaller()
    {
        string stack = BuildCallerStack();

        return stack.Contains("StardewValley.Game1+<_newDayAfterFade", StringComparison.Ordinal)
            || stack.Contains("StardewValley.Game1+<>c.<newDayAfterFade", StringComparison.Ordinal);
    }

    private static bool TryPatchMoneySetter(Harmony harmony)
    {
        MethodInfo? setter = AccessTools.PropertySetter(typeof(Farmer), nameof(Farmer.Money));

        if (setter is null)
        {
            monitor?.Log(
                "[Shipping Intercept] Farmer.Money setter not found. Shipping market settlement unavailable.",
                LogLevel.Warn
            );

            return false;
        }

        string key = GetMethodKey(setter);

        if (!PatchedMethodKeys.Add(key))
            return true;

        try
        {
            harmony.Patch(
                setter,
                prefix: new HarmonyMethod(typeof(ShippingSettlementTracePatch), nameof(MoneySetterPrefix))
            );

            return true;
        }
        catch (Exception ex)
        {
            monitor?.Log(
                $"[Shipping Intercept] Failed to patch Farmer.Money setter {key}: {ex.GetType().Name}: {ex.Message}",
                LogLevel.Warn
            );

            return false;
        }
    }

    private static IEnumerable<MethodBase> FindFocusedMethods()
    {
        foreach (MethodInfo method in typeof(Farm).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            if (method.Name.Equals("getShippingBin", StringComparison.Ordinal))
                yield return method;
        }
    }

    private static string DescribeResult(object? result)
    {
        if (result is null)
            return "null";

        if (result is System.Collections.ICollection collection)
            return $"{result.GetType().Name} Count={collection.Count}";

        return result.GetType().Name;
    }

    private static bool ShouldOpenWindow()
    {
        return IsMarketSettlementEnabled() || IsVerboseTraceEnabled();
    }

    private static bool ShouldMonitorForFarmer(Farmer farmer)
    {
        return traceWindowOpen
            && Context.IsWorldReady
            && Game1.player is not null
            && ReferenceEquals(farmer, Game1.player);
    }

    private static bool ShouldVerboseTrace()
    {
        return ShouldMonitorWindow() && IsVerboseTraceEnabled();
    }

    private static bool ShouldMonitorWindow()
    {
        return traceWindowOpen
            && Context.IsWorldReady
            && Game1.player is not null;
    }

    private static bool IsMarketSettlementEnabled()
    {
        return configService?.Config.Market.EnableShippingBinMarketSettlement == true;
    }

    private static bool IsVerboseTraceEnabled()
    {
        return configService?.Config.Market.EnableShippingSettlementVerboseTrace == true;
    }

    private static int SafeMoney()
    {
        return Context.IsWorldReady && Game1.player is not null
            ? Game1.player.Money
            : 0;
    }

    private static string BuildCallerStack()
    {
        try
        {
            StackTrace stackTrace = new(skipFrames: 2, fNeedFileInfo: false);
            List<string> frames = new();

            foreach (StackFrame frame in stackTrace.GetFrames().Take(10))
            {
                MethodBase? method = frame.GetMethod();

                if (method is null)
                    continue;

                string key = GetMethodKey(method);

                if (key.Contains(nameof(ShippingSettlementTracePatch), StringComparison.Ordinal))
                    continue;

                frames.Add(key);
            }

            return frames.Count > 0
                ? string.Join(" <- ", frames)
                : "<empty>";
        }
        catch (Exception ex)
        {
            return $"<stack unavailable: {ex.GetType().Name}>";
        }
    }

    private static string GetMethodKey(MethodBase method)
    {
        return $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
    }
}
