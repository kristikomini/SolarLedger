using Microsoft.EntityFrameworkCore;
using SolarLedger.Application.Abstractions;
using SolarLedger.Domain.Communities;
using SolarLedger.Domain.Members;
using SolarLedger.Domain.Metering;
using SolarLedger.Domain.Pods;
using SolarLedger.Domain.Settlement;

namespace SolarLedger.Infrastructure.Persistence;

public class SolarLedgerDbContext : DbContext, ISolarLedgerDbContext
{
    public SolarLedgerDbContext(DbContextOptions<SolarLedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Community> Communities => Set<Community>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Pod> Pods => Set<Pod>();
    public DbSet<EnergyReading> EnergyReadings => Set<EnergyReading>();
    public DbSet<SettlementRun> SettlementRuns => Set<SettlementRun>();
    public DbSet<MemberSettlement> MemberSettlements => Set<MemberSettlement>();
    public DbSet<HourlyShare> HourlyShares => Set<HourlyShare>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Keep everything under one schema for the app user.
        modelBuilder.HasDefaultSchema("SOLAR");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SolarLedgerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
