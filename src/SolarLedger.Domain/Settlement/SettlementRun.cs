using SolarLedger.Domain.Common;
using SolarLedger.Domain.Communities;

namespace SolarLedger.Domain.Settlement;

/// <summary>
/// One settlement of a community over a period. Enqueued as <see cref="SettlementRunStatus.Pending"/>,
/// claimed by exactly one worker, then completed with its results. Unique per
/// (community, period) so re-enqueueing the same period is idempotent, and claimed via
/// a database lock so multiple workers never process the same run.
/// </summary>
public class SettlementRun : Entity
{
    public long CommunityId { get; set; }
    public Community? Community { get; set; }

    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }

    public SettlementRunStatus Status { get; set; } = SettlementRunStatus.Pending;

    public string? PolicyName { get; set; }

    public decimal TotalSharedKwh { get; set; }
    public decimal TotalIncentiveEur { get; set; }

    /// <summary>Worker that claimed the run (its host name). Null until claimed.</summary>
    public string? ClaimedBy { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedOnUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public List<MemberSettlement> MemberSettlements { get; } = new();
    public List<HourlyShare> HourlyShares { get; } = new();
}
