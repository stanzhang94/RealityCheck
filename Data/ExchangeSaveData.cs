using RealityCheck.Models;

namespace RealityCheck.Data;

public class ExchangeSaveData
{
    public ExchangeAccount Account { get; set; } = new();

    public int NextContractSerial { get; set; } = 1;
}
