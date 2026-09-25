using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarLedger.Domain.Settlement;

namespace SolarLedger.Infrastructure.Persistence.Configurations;

public class SettlementRunConfiguration : IEntityTypeConfiguration<SettlementRun>
{
    public void Configure(EntityTypeBuilder<SettlementRun> builder)
    {
        builder.ToTable("SETTLEMENT_RUNS");
        builder.HasKey(r => r.Id);

        // Explicit column names so the raw SKIP LOCKED claim SQL is clean and stable.
        builder.Property(r => r.Id).HasColumnName("ID");
        builder.Property(r => r.CommunityId).HasColumnName("COMMUNITY_ID");
        builder.Property(r => r.PeriodStartUtc).HasColumnName("PERIOD_START_UTC");
        builder.Property(r => r.PeriodEndUtc).HasColumnName("PERIOD_END_UTC");
        builder.Property(r => r.Status).HasColumnName("STATUS")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.PolicyName).HasColumnName("POLICY_NAME").HasMaxLength(50);
        builder.Property(r => r.TotalSharedKwh).HasColumnName("TOTAL_SHARED_KWH").HasPrecision(18, 3);
        builder.Property(r => r.TotalIncentiveEur).HasColumnName("TOTAL_INCENTIVE_EUR").HasPrecision(18, 2);
        builder.Property(r => r.ClaimedBy).HasColumnName("CLAIMED_BY").HasMaxLength(100);
        builder.Property(r => r.ClaimedAtUtc).HasColumnName("CLAIMED_AT_UTC");
        builder.Property(r => r.CreatedOnUtc).HasColumnName("CREATED_ON_UTC");
        builder.Property(r => r.CompletedOnUtc).HasColumnName("COMPLETED_ON_UTC");
        builder.Property(r => r.ErrorMessage).HasColumnName("ERROR_MESSAGE").HasMaxLength(2000);

        builder.HasOne(r => r.Community)
            .WithMany()
            .HasForeignKey(r => r.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Idempotency: one run per community per period.
        builder.HasIndex(r => new { r.CommunityId, r.PeriodStartUtc, r.PeriodEndUtc }).IsUnique();
        // Workers scan by status.
        builder.HasIndex(r => r.Status);
    }
}
