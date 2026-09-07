using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Inventory;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryCategoryConfiguration
    : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(
        EntityTypeBuilder<InventoryCategory> builder)
    {
        builder.ToTable("inventory_categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

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

public sealed class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(
        EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryItemVariantConfiguration
    : IEntityTypeConfiguration<InventoryItemVariant>
{
    public void Configure(
        EntityTypeBuilder<InventoryItemVariant> builder)
    {
        builder.ToTable("inventory_item_variants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Sku)
            .HasMaxLength(100);

        builder.Property(x => x.ReorderLevel)
            .HasPrecision(18, 3);

        builder.Property(x => x.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.SellingPrice)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Sku
        })
        .IsUnique();

        builder.HasOne(x => x.InventoryItem)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryLocationConfiguration
    : IEntityTypeConfiguration<InventoryLocation>
{
    public void Configure(
        EntityTypeBuilder<InventoryLocation> builder)
    {
        builder.ToTable("inventory_locations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Name
        })
        .IsUnique();
    }
}

public sealed class InventoryStockBalanceConfiguration
    : IEntityTypeConfiguration<InventoryStockBalance>
{
    public void Configure(
        EntityTypeBuilder<InventoryStockBalance> builder)
    {
        builder.ToTable("inventory_stock_balances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.InventoryItemVariantId,
            x.InventoryLocationId
        })
        .IsUnique();

        builder.HasOne(x => x.InventoryItemVariant)
            .WithMany(x => x.Balances)
            .HasForeignKey(x => x.InventoryItemVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InventoryLocation)
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryTransactionConfiguration
    : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(
        EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.RecipientType)
            .HasMaxLength(50);

        builder.Property(x => x.RecipientName)
            .HasMaxLength(250);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasOne(x => x.InventoryItemVariant)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.CreatedAtUtc
        });
    }
}

public sealed class InventoryListConfiguration
    : IEntityTypeConfiguration<InventoryList>
{
    public void Configure(
        EntityTypeBuilder<InventoryList> builder)
    {
        builder.ToTable("inventory_lists");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ListType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AudienceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasMany(x => x.Items)
            .WithOne(x => x.InventoryList)
            .HasForeignKey(x => x.InventoryListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InventoryListItemConfiguration
    : IEntityTypeConfiguration<InventoryListItem>
{
    public void Configure(
        EntityTypeBuilder<InventoryListItem> builder)
    {
        builder.ToTable("inventory_list_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityPerRecipient)
            .HasPrecision(18, 3);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.InventoryListId,
            x.InventoryItemVariantId
        })
        .IsUnique();

        builder.HasOne(x => x.InventoryItemVariant)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
