using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SolarLedger.Infrastructure.Persistence;

namespace SolarLedger.Infrastructure.Settlement;

/// <summary>
/// Atomically claims one settlement run for a worker, so that running many workers is
/// safe. Uses Oracle's <c>FOR UPDATE SKIP LOCKED</c>: a row another worker has locked is
/// skipped rather than waited on, so each worker walks away with a different run and no
/// run is ever processed twice.
///
/// Also reclaims runs stuck in <c>InProgress</c> past a timeout — the worker that held
/// them died — so a crash never strands a run forever.
/// </summary>
public sealed class SettlementRunClaimer
{
    private readonly SolarLedgerDbContext _db;

    public SettlementRunClaimer(SolarLedgerDbContext db) => _db = db;

    public async Task<long?> TryClaimAsync(
        string workerId, TimeSpan staleAfter, CancellationToken ct = default)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var staleBefore = DateTime.UtcNow - staleAfter;

            // Pick one claimable run and lock it, skipping rows locked by other workers.
            long? runId;
            await using (var select = conn.CreateCommand())
            {
                select.Transaction = tx;
                select.CommandText = """
                    SELECT ID FROM SETTLEMENT_RUNS
                    WHERE (STATUS = 'Pending'
                           OR (STATUS = 'InProgress' AND CLAIMED_AT_UTC < :staleBefore))
                      AND ROWNUM <= 1
                    FOR UPDATE SKIP LOCKED
                    """;
                AddParam(select, "staleBefore", staleBefore);

                var scalar = await select.ExecuteScalarAsync(ct);
                runId = scalar is null or DBNull ? null : Convert.ToInt64(scalar);
            }

            if (runId is null)
            {
                await tx.CommitAsync(ct);
                return null;
            }

            // Mark it ours while still holding the lock, then commit to release it.
            await using (var update = conn.CreateCommand())
            {
                update.Transaction = tx;
                update.CommandText = """
                    UPDATE SETTLEMENT_RUNS
                    SET STATUS = 'InProgress', CLAIMED_BY = :worker, CLAIMED_AT_UTC = :now
                    WHERE ID = :id
                    """;
                AddParam(update, "worker", workerId);
                AddParam(update, "now", DateTime.UtcNow);
                AddParam(update, "id", runId.Value);
                await update.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return runId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
