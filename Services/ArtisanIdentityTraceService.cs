using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;

namespace RealityCheck.Services;

public static class ArtisanIdentityTraceService
{
    private static readonly string[] InterestingMemberNameParts =
    {
        "preserve",
        "preserved",
        "parent",
        "ingredient",
        "flavor",
        "quality",
        "color",
        "moddata",
        "order",
        "object",
        "held"
    };

    public static void LogIfPotentialArtisan(
        Item? item,
        string source,
        int quantity,
        int unitPrice,
        IMonitor? monitor
    )
    {
        if (item is null || monitor is null)
            return;

        if (!IsPotentialArtisan(item))
            return;

        string safeName = Safe(() => item.Name, "");
        string safeDisplayName = Safe(() => item.DisplayName, "");
        string safeQualifiedItemId = Safe(() => item.QualifiedItemId, "");
        string safeTypeName = item.GetType().FullName ?? item.GetType().Name;

        monitor.Log(
            $"[Artisan Trace] {source}: type='{safeTypeName}', name='{safeName}', displayName='{safeDisplayName}', qualifiedItemId='{safeQualifiedItemId}', category={item.Category}, stack={item.Stack}, quantity={quantity}, unitPrice={unitPrice}g.",
            LogLevel.Info
        );

        LogContextTags(item, monitor);
        LogInterestingMembers(item, monitor);
    }

    private static bool IsPotentialArtisan(Item item)
    {
        if (item.Category == -26)
            return true;

        string name = Safe(() => item.Name, "");
        string displayName = Safe(() => item.DisplayName, "");
        string combined = $"{name} {displayName}";

        return combined.Contains("Wine", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Jelly", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Pickles", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Juice", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Smoked", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Dried", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Roe", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Aged Roe", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("酒", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("果酱", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("腌", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("熏", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("果干", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("鱼籽", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogContextTags(Item item, IMonitor monitor)
    {
        try
        {
            MethodInfo? method = item.GetType().GetMethod(
                "GetContextTags",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            );

            object? result = method?.Invoke(item, null);

            if (result is IEnumerable<string> tags)
            {
                string joinedTags = string.Join(
                    ", ",
                    tags.Take(40)
                );

                if (!string.IsNullOrWhiteSpace(joinedTags))
                {
                    monitor.Log(
                        $"[Artisan Trace] ContextTags: {joinedTags}",
                        LogLevel.Info
                    );
                }
            }
        }
        catch (Exception ex)
        {
            monitor.Log(
                $"[Artisan Trace] Could not read context tags: {ex.GetType().Name}: {ex.Message}",
                LogLevel.Trace
            );
        }
    }

    private static void LogInterestingMembers(Item item, IMonitor monitor)
    {
        Type type = item.GetType();
        List<string> lines = new();

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!IsInterestingMemberName(property.Name))
                continue;

            if (property.GetIndexParameters().Length > 0)
                continue;

            if (!property.CanRead)
                continue;

            try
            {
                object? value = property.GetValue(item);
                string formattedValue = FormatValue(value);

                if (!string.IsNullOrWhiteSpace(formattedValue))
                    lines.Add($"property {property.Name}={formattedValue}");
            }
            catch
            {
                // Reflection trace only; ignore unreadable runtime members.
            }
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!IsInterestingMemberName(field.Name))
                continue;

            try
            {
                object? value = field.GetValue(item);
                string formattedValue = FormatValue(value);

                if (!string.IsNullOrWhiteSpace(formattedValue))
                    lines.Add($"field {field.Name}={formattedValue}");
            }
            catch
            {
                // Reflection trace only; ignore unreadable runtime members.
            }
        }

        foreach (string line in lines.Distinct().Take(60))
        {
            monitor.Log(
                $"[Artisan Trace] {line}",
                LogLevel.Info
            );
        }

        if (lines.Count == 0)
        {
            monitor.Log(
                "[Artisan Trace] No obvious preserve/parent/ingredient members found on this runtime item object.",
                LogLevel.Info
            );
        }
    }

    private static bool IsInterestingMemberName(string name)
    {
        return InterestingMemberNameParts.Any(part =>
            name.Contains(part, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
            return "null";

        if (value is string s)
            return $"'{s}'";

        if (value is int or long or short or byte or bool or double or float or decimal)
            return value.ToString() ?? "";

        if (value is IDictionary dictionary)
        {
            List<string> pairs = new();
            int count = 0;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (count >= 20)
                    break;

                pairs.Add($"{entry.Key}={entry.Value}");
                count++;
            }

            return $"{{{string.Join(", ", pairs)}}}";
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            List<string> values = new();
            int count = 0;

            foreach (object? entry in enumerable)
            {
                if (count >= 20)
                    break;

                values.Add(entry?.ToString() ?? "null");
                count++;
            }

            if (values.Count > 0)
                return $"[{string.Join(", ", values)}]";
        }

        string text = value.ToString() ?? "";

        if (text.Length > 300)
            text = text[..300] + "...";

        return text;
    }

    private static T Safe<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter();
        }
        catch
        {
            return fallback;
        }
    }
}
