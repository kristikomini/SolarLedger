using SolarLedger.Domain.Common;

namespace SolarLedger.Domain.Settlement;

/// <summary>One member's payout line within a settlement run.</summary>
public class MemberSettlement : Entity
{
    public long SettlementRunId { get; set; }
    public SettlementRun? SettlementRun { get; set; }

    public long MemberId { get; set; }

    public decimal AttributedKwh { get; set; }
    public decimal IncentiveEur { get; set; }
}
