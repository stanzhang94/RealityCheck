using System;
using System.Collections.Generic;
using System.Globalization;
using RealityCheck.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RealityCheck.Services;

public class HealthInsuranceNoticeService
{
    private readonly LedgerService ledgerService;
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private readonly Dictionary<string, string> runtimeMailBodies = new();

    public HealthInsuranceNoticeService(
        LedgerService ledgerService,
        IModHelper helper,
        IMonitor monitor
    )
    {
        this.ledgerService = ledgerService;
        this.helper = helper;
        this.monitor = monitor;
    }

    public void DeliverHealthInsuranceClaimNotice(
        HealthInsuranceClaim claim
    )
    {
        string mailId = claim.GetMailId();
        string mailBody = this.BuildMailBody(
            claim,
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
                $"Health insurance claim notice delivered: {mailId}",
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

            foreach (HealthInsuranceClaim claim in this.ledgerService.GetHealthInsuranceClaims())
            {
                if (!claim.Processed)
                    continue;

                string mailId = claim.GetMailId();

                mailData[mailId] = this.BuildMailBody(
                    claim,
                    mailId
                );
            }

            foreach (KeyValuePair<string, string> pair in this.runtimeMailBodies)
            {
                mailData[pair.Key] = pair.Value;
            }
        });
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
                $"Health insurance claim mail body directly registered: {mailId}",
                LogLevel.Trace
            );
        }
        catch (Exception ex)
        {
            this.monitor.Log(
                $"Failed to directly register health insurance claim mail body: {mailId}. Error: {ex}",
                LogLevel.Warn
            );
        }
    }

    private string BuildMailBody(
        HealthInsuranceClaim claim,
        string mailId
    )
    {
        var lines = new List<string>
        {
            I18n.Get("health_mail.clinic"),
            I18n.Get("health_mail.notice_title"),
            "",
            I18n.Get("health_mail.claim_number", new { claimNumber = this.BuildDisplayClaimNumber(claim) }),
            "",
            I18n.Get("health_mail.reviewed"),
            "",
            I18n.Get("health_mail.incident_date", new { date = I18n.Date(claim.MedicalExpenseYear, claim.MedicalExpenseSeason, claim.MedicalExpenseDay) }),
            I18n.Get("health_mail.processed_date", new { date = I18n.Date(claim.ProcessedYear, claim.ProcessedSeason, claim.ProcessedDay) }),
            "",
            I18n.Get("health_mail.actual_expense_charged", new { amount = this.FormatGold(claim.MedicalExpenseAmount) }),
            I18n.Get("health_mail.coverage_credited", new { amount = this.FormatGold(claim.CoverageAmount) }),
            "",
            I18n.Get("health_mail.thank_you")
        };

        string body = string.Join("^", lines);

        return $"{body}[#]{I18n.Get("health_mail.mail_title")}";
    }

    private string BuildDisplayClaimNumber(HealthInsuranceClaim claim)
    {
        string source = string.IsNullOrWhiteSpace(claim.Id)
            ? claim.GetMailId()
            : claim.Id;

        uint hash = 2166136261;

        unchecked
        {
            foreach (char c in source)
            {
                hash ^= c;
                hash *= 16777619;
            }
        }

        return $"HMC-{hash:X8}";
    }

    private string FormatGold(int amount)
    {
        return $"{amount.ToString("N0", CultureInfo.InvariantCulture)}g";
    }

    private string FormatSeason(string season)
    {
        return I18n.Season(season);
    }
}
