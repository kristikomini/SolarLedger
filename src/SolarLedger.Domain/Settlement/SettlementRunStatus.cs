namespace SolarLedger.Domain.Settlement;

public enum SettlementRunStatus
{
    /// <summary>Enqueued, waiting for a worker to claim it.</summary>
    Pending,

    /// <summary>Claimed by a worker and being processed.</summary>
    InProgress,

    /// <summary>Finished; results persisted.</summary>
    Completed,

    /// <summary>Processing threw; see the error message.</summary>
    Failed
}
