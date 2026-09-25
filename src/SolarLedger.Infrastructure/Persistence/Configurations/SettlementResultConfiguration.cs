using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarLedger.Domain.Settlement;

namespace SolarLedger.Infrastructure.Persistence.Configurations;

public class MemberSettlementConfiguration : IEntityTypeConfiguration<MemberSettlement>
{
    public void Configure(EntityTypeBuilder<MemberSettlement> builder)
    {
        builder.ToTable("MEMBER_SETTLEMENTS");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AttributedKwh).HasPrecision(18, 3);
        builder.Property(x => x.IncentiveEur).HasPrecision(18, 2);

        builder.HasOne(x => x.SettlementRun)
            .WithMany(r => r.MemberSettlements)
            .HasForeignKey(x => x.SettlementRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SettlementRunId, x.MemberId }).IsUnique();
    }
}

public class HourlyShareConfiguration : IEntityTypeConfiguration<HourlyShare>
{
    public void Configure(EntityTypeBuilder<HourlyShare> builder)
    {
        builder.ToTable("HOURLY_SHARES");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InjectedKwh).HasPrecision(18, 3);
        builder.Property(x => x.WithdrawnKwh).HasPrecision(18, 3);
        builder.Property(x => x.SharedKwh).HasPrecision(18, 3);

        builder.HasOne(x => x.SettlementRun)
            .WithMany(r => r.HourlyShares)
            .HasForeignKey(x => x.SettlementRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SettlementRunId, x.Hour }).IsUnique();
    }
}
