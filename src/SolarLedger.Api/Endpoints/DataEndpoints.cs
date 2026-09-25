using Microsoft.EntityFrameworkCore;
using SolarLedger.Infrastructure.Persistence;

namespace SolarLedger.Api.Endpoints;

/// <summary>
/// Phase 1 read/seed endpoints: enough to prove the model and the generated data are
/// real. The settlement endpoints arrive in later phases.
/// </summary>
public static class DataEndpoints
{
    public static IEndpointRouteBuilder MapDataEndpoints(this IEndpointRouteBuilder app)
    {
        // Seed one sample community + a week of hourly readings (idempotent).
        app.MapPost("/dev/seed", async (SolarLedgerDbContext db, int? days) =>
        {
            var result = await SampleDataSeeder.SeedAsync(db, days ?? 7);
            return Results.Ok(result);
        });

        app.MapGet("/communities", async (SolarLedgerDbContext db) =>
        {
            var list = await db.Communities
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.PrimarySubstationCode,
                    c.IncentiveTariffEurPerMwh,
                    c.DistributionPolicy,
                    MemberCount = c.Members.Count
                })
                .ToListAsync();
            return Results.Ok(list);
        });

        app.MapGet("/communities/{id:long}", async (SolarLedgerDbContext db, long id) =>
        {
            var community = await db.Communities
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.PrimarySubstationCode,
                    c.IncentiveTariffEurPerMwh,
                    Members = c.Members.Select(m => new
                    {
                        m.Id,
                        m.Name,
                        Role = m.Role.ToString(),
                        Pods = m.Pods.Select(p => new { p.Id, p.PodCode, Type = p.Type.ToString() })
                    })
                })
                .FirstOrDefaultAsync();

            return community is null ? Results.NotFound() : Results.Ok(community);
        });

        // Raw aggregation of the metered data (NOT settlement — that is Phase 2).
        app.MapGet("/communities/{id:long}/energy-summary", async (SolarLedgerDbContext db, long id) =>
        {
            // Count-based existence checks: Oracle < 23c rejects the boolean literals
            // that a scalar AnyAsync() emits (see SampleDataSeeder).
            if (await db.Communities.CountAsync(c => c.Id == id) == 0)
                return Results.NotFound();

            var readings = db.EnergyReadings.Where(r => r.Pod!.Member!.CommunityId == id);

            if (await readings.CountAsync() == 0)
                return Results.Ok(new { communityId = id, readingCount = 0 });

            var summary = new
            {
                communityId = id,
                readingCount = await readings.CountAsync(),
                totalInjectedKwh = await readings.SumAsync(r => r.InjectedKwh),
                totalWithdrawnKwh = await readings.SumAsync(r => r.WithdrawnKwh),
                firstHourUtc = await readings.MinAsync(r => r.Hour),
                lastHourUtc = await readings.MaxAsync(r => r.Hour)
            };
            return Results.Ok(summary);
        });

        return app;
    }
}
