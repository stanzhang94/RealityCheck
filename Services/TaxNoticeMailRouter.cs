using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RealityCheck.Models;
using RealityCheck.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace RealityCheck.Services;

public class TaxNoticeMailRouter
{
    private const string TaxNoticeMarker = "RC_TAX_NOTICE::";

    private readonly LedgerService ledgerService;
    private readonly IMonitor monitor;

    private int lastScannedMenuHash = 0;
    private string? lastRoutedMailId;

    public TaxNoticeMailRouter(
        LedgerService ledgerService,
        IMonitor monitor
    )
    {
        this.ledgerService = ledgerService;
        this.monitor = monitor;
    }

    public void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        this.TryRouteActiveLetterMenu(
            e.NewMenu,
            source: "MenuChanged"
        );
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        IClickableMenu? menu = Game1.activeClickableMenu;

        if (menu == null)
            return;

        int menuHash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(menu);

        if (menuHash == this.lastScannedMenuHash)
            return;

        this.lastScannedMenuHash = menuHash;

        this.TryRouteActiveLetterMenu(
            menu,
            source: "UpdateTicked"
        );
    }

    private void TryRouteActiveLetterMenu(
        IClickableMenu? menu,
        string source
    )
    {
        try
        {
            if (!Context.IsWorldReady)
                return;

            if (menu == null)
                return;

            if (menu is TaxNoticeMenu)
                return;

            string typeName = menu.GetType().Name;

            if (!typeName.Contains(
                    "LetterViewerMenu",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return;
            }

            string? mailId = this.TryExtractTaxNoticeMailId(menu);

            if (string.IsNullOrWhiteSpace(mailId))
            {
                this.monitor.Log(
                    $"LetterViewerMenu scanned from {source}, no Reality Check tax marker found.",
                    LogLevel.Trace
                );

                return;
            }

            TaxRecord? record = this.FindTaxRecordByMailId(mailId);

            if (record == null)
            {
                this.monitor.Log(
                    $"Reality Check tax mail marker found, but matching TaxRecord was not found: {mailId}",
                    LogLevel.Warn
                );

                return;
            }

            Game1.activeClickableMenu = new TaxNoticeMenu(
                this.ledgerService,
                record
            );

            this.lastRoutedMailId = mailId;

            Game1.playSound("bigSelect");

            this.monitor.Log(
                $"Tax notice mail routed to custom TaxNoticeMenu from {source}: {mailId}",
                LogLevel.Info
            );
        }
        catch (Exception ex)
        {
            this.monitor.Log(
                $"Tax notice mail router failed from {source}: {ex}",
                LogLevel.Warn
            );
        }
    }

    private TaxRecord? FindTaxRecordByMailId(string mailId)
    {
        foreach (TaxRecord record in this.ledgerService.GetTaxRecords())
        {
            if (this.GetMailId(record).Equals(
                    mailId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return record;
            }
        }

        return this.TryFindTaxRecordByParsingMailId(mailId);
    }

    private TaxRecord? TryFindTaxRecordByParsingMailId(string mailId)
    {
        string prefix = "RC_WeeklyTaxNotice_Y";

        if (!mailId.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return null;
        }

        string raw = mailId.Substring(prefix.Length);
        string[] parts = raw.Split('_');

        if (parts.Length != 4)
            return null;

        if (!int.TryParse(parts[0], out int year))
            return null;

        string season = parts[1];

        if (!int.TryParse(parts[2], out int startDay))
            return null;

        if (!int.TryParse(parts[3], out int endDay))
            return null;

        return this.ledgerService.GetTaxRecords()
            .LastOrDefault(r =>
                r.Year == year
                && string.Equals(
                    r.Season,
                    season,
                    StringComparison.OrdinalIgnoreCase
                )
                && r.CoveredStartDay == startDay
                && r.CoveredEndDay == endDay
            );
    }

    private string GetMailId(TaxRecord record)
    {
        return
            $"RC_WeeklyTaxNotice_" +
            $"Y{record.Year}_" +
            $"{record.Season}_" +
            $"{record.CoveredStartDay}_{record.CoveredEndDay}";
    }

    private string? TryExtractTaxNoticeMailId(object menu)
    {
        string? idFromString = this.TryExtractTaxNoticeMailIdFromString(
            menu.ToString() ?? ""
        );

        if (!string.IsNullOrWhiteSpace(idFromString))
            return idFromString;

        var visited = new HashSet<object>(
            ReferenceEqualityComparer.Instance
        );

        return this.TryExtractTaxNoticeMailIdFromObject(
            menu,
            visited,
            depth: 0
        );
    }

    private string? TryExtractTaxNoticeMailIdFromObject(
        object? value,
        HashSet<object> visited,
        int depth
    )
    {
        if (value == null)
            return null;

        if (depth > 6)
            return null;

        if (value is string text)
            return this.TryExtractTaxNoticeMailIdFromString(text);

        Type type = value.GetType();

        if (type.IsPrimitive || type.IsEnum)
            return null;

        if (type.FullName != null
            && type.FullName.StartsWith(
                "Microsoft.Xna.Framework",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return null;
        }

        if (!visited.Add(value))
            return null;

        if (value is IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                string? id = this.TryExtractTaxNoticeMailIdFromObject(
                    item,
                    visited,
                    depth + 1
                );

                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }
        }

        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            object? fieldValue;

            try
            {
                fieldValue = field.GetValue(value);
            }
            catch
            {
                continue;
            }

            string? id = this.TryExtractTaxNoticeMailIdFromObject(
                fieldValue,
                visited,
                depth + 1
            );

            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length > 0)
                continue;

            if (!property.CanRead)
                continue;

            object? propertyValue;

            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            string? id = this.TryExtractTaxNoticeMailIdFromObject(
                propertyValue,
                visited,
                depth + 1
            );

            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return null;
    }

    private string? TryExtractTaxNoticeMailIdFromString(string text)
    {
        int markerIndex = text.IndexOf(
            TaxNoticeMarker,
            StringComparison.OrdinalIgnoreCase
        );

        if (markerIndex < 0)
            return null;

        int start = markerIndex + TaxNoticeMarker.Length;

        while (start < text.Length && char.IsWhiteSpace(text[start]))
            start++;

        int end = start;

        while (end < text.Length && this.IsMailIdCharacter(text[end]))
            end++;

        if (end <= start)
            return null;

        return text.Substring(
            start,
            end - start
        );
    }

    private bool IsMailIdCharacter(char value)
    {
        return char.IsLetterOrDigit(value)
            || value == '_'
            || value == '-';
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
