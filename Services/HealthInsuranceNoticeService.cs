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
            "Harvey Medical Clinic",
            "Medical Insurance Reimbursement Notice",
            "",
            $"Claim Number: {mailId}",
            "",
            "Your medical insurance claim has been reviewed and processed.",
            "",
            $"Incident Date: Year {claim.MedicalExpenseYear} {this.FormatSeason(claim.MedicalExpenseSeason)} {claim.MedicalExpenseDay}",
            $"Processed Date: Year {claim.ProcessedYear} {this.FormatSeason(claim.ProcessedSeason)} {claim.ProcessedDay}",
            "",
            $"Actual Medical Expense Charged: {this.FormatGold(claim.MedicalExpenseAmount)}",
            $"Insurance Coverage Credited: {this.FormatGold(claim.CoverageAmount)}",
            "",
            "Thank you for your continued participation in the Harvey Medical Insurance Fund."
        };

        string body = string.Join("^", lines);

        return $"{body}[#]Harvey Medical Insurance Claim";
    }

    private string FormatGold(int amount)
    {
        return $"{amount.ToString("N0", CultureInfo.InvariantCulture)}g";
    }

    private string FormatSeason(string season)
    {
        return season switch
        {
            "spring" => "Spring",
            "summer" => "Summer",
            "fall" => "Fall",
            "winter" => "Winter",
            _ => season
        };
    }
}
