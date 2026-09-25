using Microsoft.EntityFrameworkCore;
using SolarLedger.Domain.Communities;
using SolarLedger.Domain.Members;
using SolarLedger.Domain.Metering;
using SolarLedger.Domain.Pods;

namespace SolarLedger.Application.Abstractions;

/// <summary>
/// Application-facing view of the persistence context. Lets the settlement logic
/// (Phase 2+) depend on an abstraction rather than the concrete EF context.
/// </summary>
public interface ISolarLedgerDbContext
{
    DbSet<Community> Communities { get; }
    DbSet<Member> Members { get; }
    DbSet<Pod> Pods { get; }
    DbSet<EnergyReading> EnergyReadings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
