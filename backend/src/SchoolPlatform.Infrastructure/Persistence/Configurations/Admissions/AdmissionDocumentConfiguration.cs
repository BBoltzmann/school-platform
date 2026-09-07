using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Admissions;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Admissions;

public sealed class AdmissionDocumentConfiguration
    : IEntityTypeConfiguration<AdmissionDocument>
{
    public void Configure(
        EntityTypeBuilder<AdmissionDocument> builder)
    {
        builder.ToTable("admission_documents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StoredFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AdmissionApplicationId,
            x.DocumentType
        });

        builder.HasOne(x => x.AdmissionApplication)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
