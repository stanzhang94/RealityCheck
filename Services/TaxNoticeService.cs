using System;
using System.Collections.Generic;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck.Services;

public class TaxNoticeService
{
    private const string TaxNoticeMarker = "RC_TAX_NOTICE::";

    private readonly LedgerService ledgerService;
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private readonly Dictionary<string, string> runtimeMailBodies = new();

    public TaxNoticeService(
        LedgerService ledgerService,
        IModHelper helper,
        IMonitor monitor
    )
    {
        this.ledgerService = ledgerService;
        this.helper = helper;
        this.monitor = monitor;
    }

    public void DeliverWeeklyTaxNotice(TaxRecord record)
    {
        string mailId = this.GetMailId(record);
        string mailBody = this.BuildFallbackMailBody(
            record,
            mailId
        );

        this.runtimeMailBodies[mailId] = mailBody;

        this.RegisterMailBodyNow(
            mailId,
            mailBody
        );

        if (!Game1.player.mailReceived.Contains(mailId)
            && !Game1.mailbox.Contains(mailId))
        {
            Game1.mailbox.Add(mailId);

            this.monitor.Log(
                $"Weekly tax notice delivered: {mailId}",
                LogLevel.Info
            );
        }
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Mail")
            && !e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
        {
            return;
        }

        e.Edit(asset =>
        {
            IDictionary<string, string> mailData =
                asset.AsDictionary<string, string>().Data;

            foreach (TaxRecord record in this.ledgerService.GetTaxRecords())
            {
                string mailId = this.GetMailId(record);

                mailData[mailId] = this.BuildFallbackMailBody(
                    record,
                    mailId
                );
            }

            foreach (KeyValuePair<string, string> pair in this.runtimeMailBodies)
            {
                mailData[pair.Key] = pair.Value;
            }
        });
    }

    public string GetMailId(TaxRecord record)
    {
        return
            $"RC_WeeklyTaxNotice_" +
            $"Y{record.Year}_" +
            $"{record.Season}_" +
            $"{record.CoveredStartDay}_{record.CoveredEndDay}";
    }

    private void RegisterMailBodyNow(
        string mailId,
        string mailBody
    )
    {
        try
        {
            this.helper.GameContent.InvalidateCache("Data/Mail");
            this.helper.GameContent.InvalidateCache("Data/mail");

            Dictionary<string, string> mailData =
                Game1.content.Load<Dictionary<string, string>>("Data\\Mail");

            mailData[mailId] = mailBody;

            this.monitor.Log(
                $"Weekly tax notice mail body directly registered: {mailId}",
                LogLevel.Trace
            );
        }
        catch (Exception ex)
        {
            this.monitor.Log(
                $"Failed to directly register weekly tax notice mail body: {mailId}. Error: {ex}",
                LogLevel.Warn
            );
        }
    }

    private string BuildFallbackMailBody(
        TaxRecord record,
        string mailId
    )
    {
        var lines = new List<string>
        {
            $"{TaxNoticeMarker}{mailId}",
            "",
            I18n.Get("tax_mail.formal_notice_issued"),
            "",
            I18n.Get("tax_notice.tax_period", new { period = I18n.PeriodSameSeason(record.Year, record.Season, record.CoveredStartDay, record.CoveredEndDay) }),
            I18n.Get("tax_notice.settlement_date", new { date = I18n.Date(record.SettlementYear, record.SettlementSeason, record.SettlementDay) }),
            "",
            I18n.Get("tax_mail.open_notice")
        };

        string body = string.Join("^", lines);

        return $"{body}[#]{I18n.Get("tax_notice.title")}";
    }

    private string FormatSeason(string season)
    {
        return I18n.Season(season);
    }
}
