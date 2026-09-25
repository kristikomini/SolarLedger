using System.Text;
using Microsoft.EntityFrameworkCore;
using SolarLedger.Domain.Settlement;
using SolarLedger.Infrastructure.Persistence;

namespace SolarLedger.Api.Endpoints;

/// <summary>
/// Phase 4: enqueue settlement runs and read their status/results. The API only
/// enqueues and reports — the worker instances do the processing.
/// </summary>
public static class SettlementRunEndpoints
{
    public static IEndpointRouteBuilder MapSettlementRunEndpoints(this IEndpointRouteBuilder app)
    {
        // Enqueue one run for a period (idempotent per community + period).
        app.MapPost("/communities/{id:long}/settlement/runs",
            async (SolarLedgerDbContext db, long id, DateTime from, DateTime to, string? policy) =>
        {
            if (await db.Communities.CountAsync(c => c.Id == id) == 0)
                return Results.NotFound();

            var existing = await db.SettlementRuns
                .Where(r => r.CommunityId == id && r.PeriodStartUtc == from && r.PeriodEndUtc == to)
                .Select(r => new { r.Id, r.Status })
                .FirstOrDefaultAsync();

            if (existing is not null)
                return Results.Ok(new { runId = existing.Id, status = existing.Status.ToString(), created = false });

            var run = new SettlementRun
            {
                CommunityId = id,
                PeriodStartUtc = from,
                PeriodEndUtc = to,
                PolicyName = policy,
                Status = SettlementRunStatus.Pending
            };
            db.SettlementRuns.Add(run);
            await db.SaveChangesAsync();

            return Results.Ok(new { runId = run.Id, status = run.Status.ToString(), created = true });
        });

        // Convenience: enqueue one run per day, to give the workers a queue to share.
        app.MapPost("/communities/{id:long}/settlement/runs/enqueue-daily",
            async (SolarLedgerDbContext db, long id, DateTime start, int days, string? policy) =>
        {
            if (await db.Communities.CountAsync(c => c.Id == id) == 0)
                return Results.NotFound();

            var created = 0;
            for (var d = 0; d < days; d++)
            {
                var from = DateTime.SpecifyKind(start.Date.AddDays(d), DateTimeKind.Utc);
                var to = from.AddDays(1);

                var exists = await db.SettlementRuns.CountAsync(r =>
                    r.CommunityId == id && r.PeriodStartUtc == from && r.PeriodEndUtc == to) > 0;
                if (exists) continue;

                db.SettlementRuns.Add(new SettlementRun
                {
                    CommunityId = id,
                    PeriodStartUtc = from,
                    PeriodEndUtc = to,
                    PolicyName = policy,
                    Status = SettlementRunStatus.Pending
                });
                created++;
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { enqueued = created });
        });

        app.MapGet("/communities/{id:long}/settlement/runs", async (SolarLedgerDbContext db, long id) =>
        {
            var runs = await db.SettlementRuns
                .Where(r => r.CommunityId == id)
                .OrderBy(r => r.PeriodStartUtc)
                .Select(r => new
                {
                    r.Id,
                    from = r.PeriodStartUtc,
                    to = r.PeriodEndUtc,
                    status = r.Status.ToString(),
                    r.ClaimedBy,
                    r.TotalSharedKwh,
                    r.TotalIncentiveEur
                })
                .ToListAsync();
            return Results.Ok(runs);
        });

        app.MapGet("/settlement/runs/{runId:long}", async (SolarLedgerDbContext db, long runId) =>
        {
            var run = await db.SettlementRuns
                .Where(r => r.Id == runId)
                .Select(r => new
                {
                    r.Id,
                    r.CommunityId,
                    from = r.PeriodStartUtc,
                    to = r.PeriodEndUtc,
                    status = r.Status.ToString(),
                    r.PolicyName,
                    r.ClaimedBy,
                    r.TotalSharedKwh,
                    r.TotalIncentiveEur,
                    r.ErrorMessage,
                    members = r.MemberSettlements
                        .Select(m => new { m.MemberId, m.AttributedKwh, m.IncentiveEur })
                })
                .FirstOrDefaultAsync();

            return run is null ? Results.NotFound() : Results.Ok(run);
        });

        // Hourly shared-energy detail of a run (for the dashboard chart).
        app.MapGet("/settlement/runs/{runId:long}/hourly", async (SolarLedgerDbContext db, long runId) =>
        {
            var hours = await db.HourlyShares
                .Where(h => h.SettlementRunId == runId)
                .OrderBy(h => h.Hour)
                .Select(h => new { h.Hour, h.InjectedKwh, h.WithdrawnKwh, h.SharedKwh })
                .ToListAsync();
            return Results.Ok(hours);
        });

        // GSE-style settlement report as CSV.
        app.MapGet("/settlement/runs/{runId:long}/report.csv", async (SolarLedgerDbContext db, long runId) =>
        {
            var run = await db.SettlementRuns.Include(r => r.Community)
                .FirstOrDefaultAsync(r => r.Id == runId);
            if (run is null) return Results.NotFound();

            var lines = await db.MemberSettlements.Where(m => m.SettlementRunId == runId).ToListAsync();
            var names = await db.Members.Where(m => m.CommunityId == run.CommunityId)
                .ToDictionaryAsync(m => m.Id, m => m.Name);

            var sb = new StringBuilder();
            sb.AppendLine("RunId,Community,PeriodStartUtc,PeriodEndUtc,Policy,TotalSharedKwh,TotalIncentiveEur");
            sb.AppendLine(string.Join(',',
                run.Id, Csv(run.Community!.Name), $"{run.PeriodStartUtc:o}", $"{run.PeriodEndUtc:o}",
                Csv(run.PolicyName ?? ""), run.TotalSharedKwh, run.TotalIncentiveEur));
            sb.AppendLine();
            sb.AppendLine("MemberId,MemberName,AttributedKwh,IncentiveEur");
            foreach (var m in lines)
                sb.AppendLine(string.Join(',',
                    m.MemberId, Csv(names.GetValueOrDefault(m.MemberId, "?")),
                    m.AttributedKwh, m.IncentiveEur));

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Results.File(bytes, "text/csv", $"settlement-run-{runId}.csv");
        });

        return app;
    }

    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
