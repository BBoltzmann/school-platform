using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Fees;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Fees;

public sealed class FeeItemConfiguration
    : IEntityTypeConfiguration<FeeItem>
{
    public void Configure(
        EntityTypeBuilder<FeeItem> builder)
    {
        builder.ToTable("fee_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Name
        })
        .IsUnique();
    }
}

public sealed class FeeStructureConfiguration
    : IEntityTypeConfiguration<FeeStructure>
{
    public void Configure(
        EntityTypeBuilder<FeeStructure> builder)
    {
        builder.ToTable("fee_structures");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AudienceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicTermId,
            x.Name
        });

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.FeeStructure)
            .HasForeignKey(x => x.FeeStructureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FeeStructureLineConfiguration
    : IEntityTypeConfiguration<FeeStructureLine>
{
    public void Configure(
        EntityTypeBuilder<FeeStructureLine> builder)
    {
        builder.ToTable("fee_structure_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.FeeStructureId,
            x.FeeItemId
        })
        .IsUnique();

        builder.HasOne(x => x.FeeItem)
            .WithMany(x => x.StructureLines)
            .HasForeignKey(x => x.FeeItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StudentFeeChargeConfiguration
    : IEntityTypeConfiguration<StudentFeeCharge>
{
    public void Configure(
        EntityTypeBuilder<StudentFeeCharge> builder)
    {
        builder.ToTable("student_fee_charges");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.AmountPaid)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StudentId,
            x.AcademicTermId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StudentId,
            x.AcademicTermId,
            x.FeeStructureLineId
        })
        .IsUnique();

        builder.HasOne(x => x.FeeItem)
            .WithMany(x => x.Charges)
            .HasForeignKey(x => x.FeeItemId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FeePaymentConfiguration
    : IEntityTypeConfiguration<FeePayment>
{
    public void Configure(
        EntityTypeBuilder<FeePayment> builder)
    {
        builder.ToTable("fee_payments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PaymentMethod)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ReceiptNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Reference)
            .HasMaxLength(150);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.ReversalReason)
            .HasMaxLength(500);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.ReceiptNumber
        })
        .IsUnique();

        builder.HasMany(x => x.Allocations)
            .WithOne(x => x.FeePayment)
            .HasForeignKey(x => x.FeePaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FeePaymentAllocationConfiguration
    : IEntityTypeConfiguration<FeePaymentAllocation>
{
    public void Configure(
        EntityTypeBuilder<FeePaymentAllocation> builder)
    {
        builder.ToTable("fee_payment_allocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.FeePaymentId,
            x.StudentFeeChargeId
        })
        .IsUnique();

        builder.HasOne(x => x.StudentFeeCharge)
            .WithMany()
            .HasForeignKey(x => x.StudentFeeChargeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
