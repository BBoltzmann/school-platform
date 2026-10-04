using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Tenancy;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class TenantProfileConfiguration : IEntityTypeConfiguration<TenantProfile>
{
    public void Configure(EntityTypeBuilder<TenantProfile> builder)
    {
        builder.ToTable("tenant_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WebsiteUrl).HasMaxLength(500);
        builder.Property(x => x.ContactEmail).HasMaxLength(320);
        builder.Property(x => x.ContactPhone).HasMaxLength(80);
        builder.Property(x => x.Address).HasMaxLength(1000);
        builder.Property(x => x.Motto).HasMaxLength(500);
        builder.Property(x => x.Mission).HasMaxLength(4000);
        builder.Property(x => x.Vision).HasMaxLength(4000);
        builder.Property(x => x.ShortAbout).HasMaxLength(4000);
        builder.Property(x => x.LogoDataUrl).HasColumnType("text");
        builder.Property(x => x.IconDataUrl).HasColumnType("text");
        builder.Property(x => x.PrimaryColor).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SecondaryColor).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccentColor).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasOne(x => x.Tenant).WithOne().HasForeignKey<TenantProfile>(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
