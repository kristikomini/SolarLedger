using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarLedger.Domain.Members;

namespace SolarLedger.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("MEMBERS");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.PaymentReference).HasMaxLength(64).IsRequired();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(m => m.Community)
            .WithMany(c => c.Members)
            .HasForeignKey(m => m.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CommunityId);
    }
}
