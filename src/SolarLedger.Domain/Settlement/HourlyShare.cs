using SolarLedger.Domain.Common;

namespace SolarLedger.Domain.Settlement;

/// <summary>Per-hour shared-energy detail of a run, kept for audit.</summary>
public class HourlyShare : Entity
{
    public long SettlementRunId { get; set; }
    public SettlementRun? SettlementRun { get; set; }

    public DateTime Hour { get; set; }
    public decimal InjectedKwh { get; set; }
    public decimal WithdrawnKwh { get; set; }
    public decimal SharedKwh { get; set; }
}
