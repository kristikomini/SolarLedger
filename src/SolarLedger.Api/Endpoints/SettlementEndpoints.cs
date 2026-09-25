using Microsoft.EntityFrameworkCore;
using SolarLedger.Application.Settlement;
using SolarLedger.Infrastructure.Persistence;

namespace SolarLedger.Api.Endpoints;

/// <summary>
/// Phase 2: run the settlement engine over stored readings and return the result.
/// Read-only preview — nothing is persisted yet (that is Phase 3). Proves the core
/// calculation end-to-end against real Oracle data.
/// </summary>
public static class SettlementEndpoints
{
    public static IEndpointRouteBuilder MapSettlementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/communities/{id:long}/settlement/preview",
            async (SolarLedgerDbContext db, long id, DateTime? from, DateTime? to, string? policy) =>
        {
            var community = await db.Communities
                .Select(c => new { c.Id, c.Name, c.IncentiveTariffEurPerMwh, c.DistributionPolicy })
                .FirstOrDefaultAsync(c => c.Id == id);

            if (community is null)
                return Results.NotFound();

            // Default to the full range of the community's readings.
            var readings = db.EnergyReadings.Where(r => r.Pod!.Member!.CommunityId == id);
            if (from is { } f) readings = readings.Where(r => r.Hour >= f);
            if (to is { } t) readings = readings.Where(r => r.Hour < t);

            var rows = await readings
                .Select(r => new
                {
                    r.Pod!.MemberId,
                    Role = r.Pod.Member!.Role,
                    r.Hour,
                    r.InjectedKwh,
                    r.WithdrawnKwh
                })
                .ToListAsync();

            if (rows.Count == 0)
                return Results.Ok(new { communityId = id, message = "No readings in range." });

            var members = rows
                .GroupBy(x => new { x.MemberId, x.Role })
                .Select(g => new MemberEnergy(
                    g.Key.MemberId,
                    g.Key.Role,
                    g.Select(x => new HourlyEnergy(x.Hour, x.InjectedKwh, x.WithdrawnKwh)).ToList()))
                .ToList();

            var chosen = DistributionPolicies.Resolve(policy ?? community.DistributionPolicy);
            var calculator = new SettlementCalculator(chosen);
            var result = calculator.Calculate(
                new SettlementInput(community.IncentiveTariffEurPerMwh, members));

            // Enrich member lines with names for a readable preview.
            var names = await db.Members
                .Where(m => m.CommunityId == id)
                .ToDictionaryAsync(m => m.Id, m => m.Name);

            return Results.Ok(new
            {
                communityId = id,
                community = community.Name,
                policy = result.PolicyName,
                tariffEurPerMwh = community.IncentiveTariffEurPerMwh,
                fromUtc = rows.Min(r => r.Hour),
                toUtc = rows.Max(r => r.Hour),
                totalSharedKwh = result.TotalSharedKwh,
                totalIncentiveEur = result.TotalIncentiveEur,
                members = result.Members.Select(a => new
                {
                    a.MemberId,
                    name = names.GetValueOrDefault(a.MemberId, "?"),
                    attributedKwh = a.AttributedKwh,
                    incentiveEur = a.IncentiveEur
                })
            });
        });

        return app;
    }
}
