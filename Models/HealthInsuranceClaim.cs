using System;

namespace RealityCheck.Models;

public class HealthInsuranceClaim
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int MedicalExpenseYear { get; set; }

    public string MedicalExpenseSeason { get; set; } = "";

    public int MedicalExpenseDay { get; set; }

    public int MedicalExpenseAmount { get; set; }

    public int CoverageAmount { get; set; }

    public bool Processed { get; set; } = false;

    public int ProcessedYear { get; set; }

    public string ProcessedSeason { get; set; } = "";

    public int ProcessedDay { get; set; }

    public string GetMailId()
    {
        return $"RC_HealthInsuranceClaim_{this.Id}";
    }
}
