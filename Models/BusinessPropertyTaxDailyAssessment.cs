namespace RealityCheck.Models;

public class BusinessPropertyTaxDailyAssessment
{
    public int Year { get; set; }

    public string Season { get; set; } = "";

    public int Day { get; set; }

    public int KegCount { get; set; }

    public int PreservesJarCount { get; set; }

    public int CaskCount { get; set; }

    public int BeeHouseCount { get; set; }

    public int MayonnaiseMachineCount { get; set; }

    public int CheesePressCount { get; set; }

    public int LoomCount { get; set; }

    public int OilMakerCount { get; set; }

    public int DehydratorCount { get; set; }

    public int FishSmokerCount { get; set; }

    public int TotalBusinessPropertyTaxAmount { get; set; }
}