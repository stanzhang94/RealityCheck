using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace RealityCheck.Patches;

public static class ShopSalePriceTracePatch
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

        foreach (MethodInfo method in FindSellToStorePriceMethods())
        {
            string key = GetMethodKey(method);

            if (!PatchedMethodKeys.Add(key))
                continue;

            try
            {
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(ShopSalePriceTracePatch), nameof(SellToStorePricePostfix))
                );

                patched++;
            }
            catch (Exception ex)
            {
                failed++;
                monitor?.Log(
                    $"[Shop Price Trace] Failed to patch {key}: {ex.GetType().Name}: {ex.Message}",
                    LogLevel.Warn
                );
            }
        }

        monitor?.Log(
            $"[Shop Price Trace] Installed sellToStorePrice trace patches: patched={patched}, failed={failed}.",
            failed > 0 ? LogLevel.Warn : LogLevel.Info
        );
    }

    public static void SellToStorePricePostfix(MethodBase __originalMethod, object __instance, ref int __result)
    {
        if (!Context.IsWorldReady || Game1.player is null)
            return;

        string stack = BuildCallerStack();

        if (!IsRelevantSaleStack(stack))
            return;

        string instanceDescription = DescribeInstance(__instance);

        monitor?.Log(
            $"[Shop Price Trace] {GetMethodKey(__originalMethod)} returned {__result}g for {instanceDescription}. callerStack={stack}",
            LogLevel.Warn
        );
    }

    private static bool IsRelevantSaleStack(string stack)
    {
        if (!stack.Contains("StardewValley.Menus.ShopMenu", StringComparison.Ordinal))
            return false;

        if (stack.Contains("performHoverAction", StringComparison.Ordinal))
            return false;

        return stack.Contains("receiveLeftClick", StringComparison.Ordinal)
            || stack.Contains("receiveRightClick", StringComparison.Ordinal)
            || stack.Contains("tryToPurchaseItem", StringComparison.Ordinal)
            || stack.Contains("AddBuybackItem", StringComparison.Ordinal);
    }

    private static IEnumerable<MethodInfo> FindSellToStorePriceMethods()
    {
        Type[] candidateTypes =
        {
            typeof(Item),
            typeof(SObject)
        };

        foreach (Type type in candidateTypes)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "sellToStorePrice")
                    continue;

                if (method.ReturnType != typeof(int))
                    continue;

                yield return method;
            }
        }
    }

    private static string DescribeInstance(object instance)
    {
        if (instance is Item item)
        {
            string itemName = SafeText(item.DisplayName);
            string itemId = SafeText(item.QualifiedItemId);
            string typeName = item.GetType().FullName ?? item.GetType().Name;

            return $"Item(name='{itemName}', id='{itemId}', type='{typeName}', stack={item.Stack})";
        }

        return instance.GetType().FullName ?? instance.GetType().Name;
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

    private static string SafeText(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? "<blank>"
            : text.Replace("\r", " ").Replace("\n", " ");
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
                    .Where(name => !name.Contains(nameof(ShopSalePriceTracePatch), StringComparison.Ordinal))
                    .Take(16)
                ?? Array.Empty<string>()
            );
        }
        catch (Exception ex)
        {
            return $"<stack unavailable: {ex.GetType().Name}>";
        }
    }
}
