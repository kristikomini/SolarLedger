using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarLedger.Domain.Communities;

namespace SolarLedger.Infrastructure.Persistence.Configurations;

public class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("COMMUNITIES");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PrimarySubstationCode).HasMaxLength(50).IsRequired();

        // Oracle: money-like value. 18,3 comfortably holds €/MWh tariffs.
        builder.Property(c => c.IncentiveTariffEurPerMwh).HasColumnType("NUMBER(18,3)");

        builder.Property(c => c.DistributionPolicy).HasMaxLength(50).IsRequired();
        builder.Property(c => c.CreatedOnUtc).IsRequired();

        builder.HasIndex(c => c.PrimarySubstationCode);
    }
}
