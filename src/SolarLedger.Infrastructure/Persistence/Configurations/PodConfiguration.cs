using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarLedger.Domain.Pods;

namespace SolarLedger.Infrastructure.Persistence.Configurations;

public class PodConfiguration : IEntityTypeConfiguration<Pod>
{
    public void Configure(EntityTypeBuilder<Pod> builder)
    {
        builder.ToTable("PODS");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PodCode).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(p => p.Member)
            .WithMany(m => m.Pods)
            .HasForeignKey(p => p.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // A POD code is unique across the system.
        builder.HasIndex(p => p.PodCode).IsUnique();
    }
}
