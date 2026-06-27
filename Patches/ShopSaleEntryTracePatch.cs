using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.Patches;

public static class ShopSaleEntryTracePatch
{
    private static readonly HashSet<string> PatchedMethodKeys = new();

    private static IMonitor? monitor;

    public static void Initialize(IMonitor modMonitor)
    {
        monitor = modMonitor;
    }

    public static void Apply(Harmony harmony)
    {
        int patched = 0;
        int failed = 0;

        if (TryPatchMoneySetter(harmony))
            patched++;
        else
            failed++;

        foreach (MethodBase method in FindCandidateMethods())
        {
            string key = GetMethodKey(method);

            if (!PatchedMethodKeys.Add(key))
                continue;

            try
            {
                harmony.Patch(
                    original: method,
                    prefix: new HarmonyMethod(typeof(ShopSaleEntryTracePatch), nameof(MethodPrefix)),
                    postfix: new HarmonyMethod(typeof(ShopSaleEntryTracePatch), nameof(MethodPostfix))
                );

                patched++;
            }
            catch (Exception ex)
            {
                failed++;
                monitor?.Log(
                    $"[Shop Entry Trace] Failed to patch {key}: {ex.GetType().Name}: {ex.Message}",
                    LogLevel.Warn
                );
            }
        }

        monitor?.Log(
            $"[Shop Entry Trace] Installed shop sale entry trace patches: patched={patched}, failed={failed}.",
            failed > 0 ? LogLevel.Warn : LogLevel.Info
        );
    }

    public static void MethodPrefix(MethodBase __originalMethod, object[] __args, out TraceState __state)
    {
        __state = new TraceState(
            SafeMoney(),
            DescribeArgs(__args)
        );

        monitor?.Log(
            $"[Shop Entry Trace] ENTER {GetMethodKey(__originalMethod)} moneyBefore={__state.MoneyBefore}g args={__state.ArgsDescription} callerStack={BuildCallerStack()}",
            LogLevel.Info
        );
    }

    // Intentionally no __result parameter here: receiveLeftClick/receiveRightClick are void methods.
    public static void MethodPostfix(MethodBase __originalMethod, TraceState __state)
    {
        int moneyAfter = SafeMoney();
        int delta = moneyAfter - __state.MoneyBefore;

        if (delta != 0)
        {
            monitor?.Log(
                $"[Shop Entry Trace] MONEY CHANGED during {GetMethodKey(__originalMethod)}: {__state.MoneyBefore}g -> {moneyAfter}g (delta {delta:+#;-#;0}g). args={__state.ArgsDescription}",
                LogLevel.Warn
            );
        }
        else
        {
            monitor?.Log(
                $"[Shop Entry Trace] EXIT {GetMethodKey(__originalMethod)} moneyUnchanged={moneyAfter}g. args={__state.ArgsDescription}",
                LogLevel.Info
            );
        }
    }

    public static void MoneySetterPrefix(Farmer __instance, int value)
    {
        if (!Context.IsWorldReady || Game1.player is null || !ReferenceEquals(__instance, Game1.player))
            return;

        int oldValue = __instance.Money;

        if (oldValue == value)
            return;

        string stack = BuildCallerStack();

        if (!stack.Contains("StardewValley.Menus.ShopMenu", StringComparison.Ordinal))
            return;

        int delta = value - oldValue;

        monitor?.Log(
            $"[Shop Entry Trace] MONEY SETTER {oldValue}g -> {value}g (delta {delta:+#;-#;0}g). callerStack={stack}",
            LogLevel.Warn
        );
    }

    private static IEnumerable<MethodBase> FindCandidateMethods()
    {
        string[] names =
        {
            "receiveLeftClick",
            "receiveRightClick",
            "tryToPurchaseItem",
            "AddBuybackItem"
        };

        return typeof(ShopMenu)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => names.Contains(method.Name, StringComparer.Ordinal))
            .Cast<MethodBase>();
    }

    private static bool TryPatchMoneySetter(Harmony harmony)
    {
        MethodInfo? setter = AccessTools.PropertySetter(typeof(Farmer), nameof(Farmer.Money));

        if (setter == null)
        {
            monitor?.Log("[Shop Entry Trace] Could not find Farmer.Money setter.", LogLevel.Warn);
            return false;
        }

        try
        {
            harmony.Patch(
                original: setter,
                prefix: new HarmonyMethod(typeof(ShopSaleEntryTracePatch), nameof(MoneySetterPrefix))
            );

            return true;
        }
        catch (Exception ex)
        {
            monitor?.Log(
                $"[Shop Entry Trace] Failed to patch Farmer.Money setter: {ex.GetType().Name}: {ex.Message}",
                LogLevel.Warn
            );
            return false;
        }
    }

    private static int SafeMoney()
    {
        return Context.IsWorldReady && Game1.player is not null
            ? Game1.player.Money
            : 0;
    }

    private static string DescribeArgs(object[] args)
    {
        if (args.Length == 0)
            return "<none>";

        List<string> parts = new();

        for (int i = 0; i < args.Length; i++)
            parts.Add($"arg{i}={DescribeValue(args[i])}");

        return string.Join(", ", parts);
    }

    private static string DescribeValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is Item item)
        {
            string itemName = SafeText(item.DisplayName);
            string itemId = SafeText(item.QualifiedItemId);
            string typeName = item.GetType().FullName ?? item.GetType().Name;

            return $"Item(name='{itemName}', id='{itemId}', type='{typeName}', stack={item.Stack})";
        }

        if (value is ISalable salable)
        {
            string salableName = SafeText(salable.DisplayName);
            string typeName = value.GetType().FullName ?? value.GetType().Name;

            return $"ISalable(name='{salableName}', type='{typeName}')";
        }

        if (value is string text)
            return $"\"{SafeText(text)}\"";

        if (value is bool or int or long or float or double or decimal)
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "<value>";

        return value.GetType().FullName ?? value.GetType().Name;
    }

    private static string SafeText(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? "<blank>"
            : text.Replace("\r", " ").Replace("\n", " ");
    }

    private static string GetMethodKey(MethodBase method)
    {
        string declaringType = method.DeclaringType?.FullName ?? "<unknown>";
        string parameters = string.Join(
            ",",
            method.GetParameters().Select(parameter => parameter.ParameterType.Name)
        );

        return $"{declaringType}.{method.Name}({parameters})";
    }

    private static string BuildCallerStack()
    {
        try
        {
            StackTrace trace = new StackTrace();

            return string.Join(
                " <- ",
                trace.GetFrames()?
                    .Select(frame => frame.GetMethod())
                    .Where(method => method != null)
                    .Select(method => $"{method!.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}")
                    .Where(name => !name.Contains(nameof(ShopSaleEntryTracePatch), StringComparison.Ordinal))
                    .Take(14)
                ?? Array.Empty<string>()
            );
        }
        catch (Exception ex)
        {
            return $"<stack unavailable: {ex.GetType().Name}>";
        }
    }

    public sealed class TraceState
    {
        public TraceState(int moneyBefore, string argsDescription)
        {
            this.MoneyBefore = moneyBefore;
            this.ArgsDescription = argsDescription;
        }

        public int MoneyBefore { get; }

        public string ArgsDescription { get; }
    }
}
