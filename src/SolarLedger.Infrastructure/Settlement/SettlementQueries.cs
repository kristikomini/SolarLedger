using Microsoft.EntityFrameworkCore;
using SolarLedger.Application.Settlement;
using SolarLedger.Infrastructure.Persistence;

namespace SolarLedger.Infrastructure.Settlement;

/// <summary>Builds the engine's <see cref="SettlementInput"/> from stored readings.</summary>
public static class SettlementQueries
{
    public static async Task<SettlementInput> LoadInputAsync(
        SolarLedgerDbContext db, long communityId, decimal tariffEurPerMwh,
        DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await db.EnergyReadings
            .Where(r => r.Pod!.Member!.CommunityId == communityId
                        && r.Hour >= fromUtc && r.Hour < toUtc)
            .Select(r => new
            {
                r.Pod!.MemberId,
                Role = r.Pod.Member!.Role,
                r.Hour,
                r.InjectedKwh,
                r.WithdrawnKwh
            })
            .ToListAsync(ct);

        var members = rows
            .GroupBy(x => new { x.MemberId, x.Role })
            .Select(g => new MemberEnergy(
                g.Key.MemberId,
                g.Key.Role,
                g.Select(x => new HourlyEnergy(x.Hour, x.InjectedKwh, x.WithdrawnKwh)).ToList()))
            .ToList();

        return new SettlementInput(tariffEurPerMwh, members);
    }
}
