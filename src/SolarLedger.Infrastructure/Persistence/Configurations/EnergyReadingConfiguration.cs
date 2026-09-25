using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarLedger.Domain.Metering;

namespace SolarLedger.Infrastructure.Persistence.Configurations;

public class EnergyReadingConfiguration : IEntityTypeConfiguration<EnergyReading>
{
    public void Configure(EntityTypeBuilder<EnergyReading> builder)
    {
        builder.ToTable("ENERGY_READINGS");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Hour).IsRequired();
        builder.Property(r => r.InjectedKwh).HasPrecision(18, 3);
        builder.Property(r => r.WithdrawnKwh).HasPrecision(18, 3);

        builder.HasOne(r => r.Pod)
            .WithMany(p => p.Readings)
            .HasForeignKey(r => r.PodId)
            .OnDelete(DeleteBehavior.Cascade);

        // One reading per POD per hour — the natural key of a meter series.
        builder.HasIndex(r => new { r.PodId, r.Hour }).IsUnique();
    }
}
