using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Inventory;
using SchoolPlatform.Domain.Inventory;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Inventory;

public sealed class InventoryService : IInventoryService
{
    private static readonly HashSet<string> MovementTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "StockIn",
            "Issue",
            "Sale",
            "Return",
            "Transfer",
            "Consumed",
            "Damaged",
            "Lost",
            "AdjustmentIn",
            "AdjustmentOut"
        };

    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public InventoryService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<InventorySetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var categories =
            await _database.InventoryCategories
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.Name)
                .Select(x =>
                    new InventoryCategoryResult(
                        x.Id,
                        x.Name,
                        x.Description))
                .ToListAsync(
                    cancellationToken);

        var locations =
            await _database.InventoryLocations
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.Name)
                .Select(x =>
                    new InventoryLocationResult(
                        x.Id,
                        x.Name,
                        x.Code))
                .ToListAsync(
                    cancellationToken);

        var items =
            await GetItemsAsync(
                tenantId,
                cancellationToken);

        var lists =
            await GetListsAsync(
                tenantId,
                cancellationToken);

        var recentTransactions =
            await GetRecentTransactionsAsync(
                tenantId,
                cancellationToken);

        var levels =
            await _database.AcademicLevels
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.Name)
                .Select(x =>
                    new InventoryAudienceOptionResult(
                        x.Id,
                        x.Name))
                .ToListAsync(
                    cancellationToken);

        var classes =
            await _database.ClassGroups
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive &&
                    x.AcademicLevel.IsActive)
                .OrderBy(x =>
                    x.AcademicLevel.Name)
                .ThenBy(x =>
                    x.Name)
                .Select(x =>
                    new InventoryAudienceOptionResult(
                        x.Id,
                        x.AcademicLevel.Name +
                        " — " +
                        x.Name))
                .ToListAsync(
                    cancellationToken);

        var students =
            await _database.Students
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.LastName)
                .ThenBy(x =>
                    x.FirstName)
                .Select(x =>
                    new InventoryRecipientResult(
                        x.Id,
                        x.FirstName +
                        " " +
                        x.LastName,
                        x.AdmissionNumber))
                .ToListAsync(
                    cancellationToken);

        var staff =
            await _database.StaffMembers
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.LastName)
                .ThenBy(x =>
                    x.FirstName)
                .Select(x =>
                    new InventoryRecipientResult(
                        x.Id,
                        x.FirstName +
                        " " +
                        x.LastName,
                        x.StaffNumber))
                .ToListAsync(
                    cancellationToken);

        return new InventorySetupResult(
            categories,
            locations,
            items,
            lists,
            recentTransactions,
            levels,
            classes,
            students,
            staff);
    }

    public async Task<InventoryCategoryResult> CreateCategoryAsync(
        CreateInventoryCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var exists =
            await _database.InventoryCategories
                .AnyAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.IsActive &&
                        x.Name.ToLower() ==
                            request.Name.Trim().ToLower(),
                    cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "An inventory category with this name already exists.");
        }

        var category =
            new InventoryCategory(
                tenantId,
                request.Name,
                request.Description);

        _database.InventoryCategories.Add(
            category);

        await _database.SaveChangesAsync(
            cancellationToken);

        return new InventoryCategoryResult(
            category.Id,
            category.Name,
            category.Description);
    }

    public async Task<InventoryLocationResult> CreateLocationAsync(
        CreateInventoryLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var location =
            new InventoryLocation(
                tenantId,
                request.Name,
                request.Code);

        _database.InventoryLocations.Add(
            location);

        await _database.SaveChangesAsync(
            cancellationToken);

        return new InventoryLocationResult(
            location.Id,
            location.Name,
            location.Code);
    }

    public async Task<InventoryItemResult> CreateItemAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var categoryExists =
            await _database.InventoryCategories
                .AnyAsync(
                    x =>
                        x.Id == request.CategoryId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException(
                "Inventory category was not found.");
        }

        var item =
            new InventoryItem(
                tenantId,
                request.CategoryId,
                request.Name,
                request.Unit,
                request.Description,
                request.TrackVariants);

        _database.InventoryItems.Add(
            item);

        var variant =
            new InventoryItemVariant(
                tenantId,
                item.Id,
                request.TrackVariants
                    ? request.DefaultVariantName ?? "Standard"
                    : "Standard",
                request.Sku,
                request.ReorderLevel,
                request.CostPrice,
                request.SellingPrice);

        _database.InventoryItemVariants.Add(
            variant);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetItemAsync(
            item.Id,
            tenantId,
            cancellationToken);
    }

    public async Task<InventoryVariantResult> CreateVariantAsync(
        Guid itemId,
        CreateInventoryVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var item =
            await _database.InventoryItems
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == itemId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Inventory item was not found.");

        var variant =
            new InventoryItemVariant(
                tenantId,
                item.Id,
                request.Name,
                request.Sku,
                request.ReorderLevel,
                request.CostPrice,
                request.SellingPrice);

        _database.InventoryItemVariants.Add(
            variant);

        await _database.SaveChangesAsync(
            cancellationToken);

        return new InventoryVariantResult(
            variant.Id,
            item.Id,
            variant.Name,
            variant.Sku,
            variant.ReorderLevel,
            variant.CostPrice,
            variant.SellingPrice,
            0m,
            variant.ReorderLevel > 0);
    }

    public async Task<InventoryTransactionResult> CreateMovementAsync(
        CreateStockMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        if (!MovementTypes.Contains(request.Type))
        {
            throw new InvalidOperationException(
                "Invalid inventory transaction type.");
        }

        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException(
                "Quantity must be greater than zero.");
        }

        var type =
            MovementTypes.First(x =>
                string.Equals(
                    x,
                    request.Type,
                    StringComparison.OrdinalIgnoreCase));

        var variant =
            await _database.InventoryItemVariants
                .Include(x => x.InventoryItem)
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.InventoryItemVariantId &&
                        x.TenantId == tenantId &&
                        x.IsActive &&
                        x.InventoryItem.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Inventory item variant was not found.");

        var requiresFrom =
            type is
                "Issue" or
                "Sale" or
                "Transfer" or
                "Consumed" or
                "Damaged" or
                "Lost" or
                "AdjustmentOut";

        var requiresTo =
            type is
                "StockIn" or
                "Return" or
                "Transfer" or
                "AdjustmentIn";

        if (requiresFrom &&
            !request.FromLocationId.HasValue)
        {
            throw new InvalidOperationException(
                "A source location is required.");
        }

        if (requiresTo &&
            !request.ToLocationId.HasValue)
        {
            throw new InvalidOperationException(
                "A destination location is required.");
        }

        InventoryStockBalance? fromBalance = null;
        InventoryStockBalance? toBalance = null;

        if (request.FromLocationId.HasValue)
        {
            await ValidateLocationAsync(
                tenantId,
                request.FromLocationId.Value,
                cancellationToken);

            fromBalance =
                await GetOrCreateBalanceAsync(
                    tenantId,
                    variant.Id,
                    request.FromLocationId.Value,
                    cancellationToken);
        }

        if (request.ToLocationId.HasValue)
        {
            await ValidateLocationAsync(
                tenantId,
                request.ToLocationId.Value,
                cancellationToken);

            toBalance =
                await GetOrCreateBalanceAsync(
                    tenantId,
                    variant.Id,
                    request.ToLocationId.Value,
                    cancellationToken);
        }

        if (fromBalance is not null)
        {
            fromBalance.Remove(
                request.Quantity);
        }

        if (toBalance is not null)
        {
            toBalance.Add(
                request.Quantity);
        }

        var recipientName =
            await ResolveRecipientNameAsync(
                tenantId,
                request.RecipientType,
                request.RecipientId,
                request.RecipientName,
                cancellationToken);

        var unitPrice =
            request.UnitPrice ??
            (type == "Sale"
                ? variant.SellingPrice
                : null);

        var transaction =
            new InventoryTransaction(
                tenantId,
                variant.Id,
                type,
                request.Quantity,
                request.FromLocationId,
                request.ToLocationId,
                request.RecipientType,
                request.RecipientId,
                recipientName,
                unitPrice,
                request.InventoryListId,
                request.InventoryListItemId,
                request.Notes);

        _database.InventoryTransactions.Add(
            transaction);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetTransactionAsync(
            transaction.Id,
            tenantId,
            cancellationToken);
    }

    public async Task<InventoryListResult> CreateListAsync(
        CreateInventoryListRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        ValidateAudience(
            request.AudienceType,
            request.AudienceId);

        foreach (var item in request.Items)
        {
            var exists =
                await _database.InventoryItemVariants
                    .AnyAsync(
                        x =>
                            x.Id ==
                                item.InventoryItemVariantId &&
                            x.TenantId == tenantId &&
                            x.IsActive,
                        cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(
                    "One or more inventory list items were not found.");
            }
        }

        var list =
            new InventoryList(
                tenantId,
                request.Name,
                request.ListType,
                request.AudienceType,
                request.AudienceId,
                request.AcademicSessionId,
                request.AcademicTermId);

        _database.InventoryLists.Add(
            list);

        foreach (var item in request.Items)
        {
            _database.InventoryListItems.Add(
                new InventoryListItem(
                    tenantId,
                    list.Id,
                    item.InventoryItemVariantId,
                    item.QuantityPerRecipient,
                    item.IsRequired));
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetListAsync(
            list.Id,
            tenantId,
            cancellationToken);
    }

    public async Task<InventoryDemandResult> GetDemandAsync(
        Guid inventoryListId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var list =
            await GetListAsync(
                inventoryListId,
                tenantId,
                cancellationToken);

        var rawList =
            await _database.InventoryLists
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id == inventoryListId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        var recipientCount =
            await GetRecipientCountAsync(
                tenantId,
                rawList,
                cancellationToken);

        var rows =
            new List<InventoryDemandItemResult>();

        foreach (var item in list.Items)
        {
            var required =
                recipientCount *
                item.QuantityPerRecipient;

            var issued =
                await _database.InventoryTransactions
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.InventoryListItemId ==
                            item.Id &&
                        (x.Type == "Issue" ||
                         x.Type == "Sale"))
                    .SumAsync(
                        x => (decimal?)x.Quantity,
                        cancellationToken)
                ?? 0m;

            var available =
                await _database.InventoryStockBalances
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.InventoryItemVariantId ==
                            item.InventoryItemVariantId)
                    .SumAsync(
                        x => (decimal?)x.Quantity,
                        cancellationToken)
                ?? 0m;

            var remaining =
                Math.Max(
                    required - issued,
                    0m);

            var shortfall =
                Math.Max(
                    remaining - available,
                    0m);

            rows.Add(
                new InventoryDemandItemResult(
                    item.Id,
                    item.ItemName,
                    item.VariantName,
                    item.Unit,
                    item.QuantityPerRecipient,
                    required,
                    issued,
                    remaining,
                    available,
                    shortfall));
        }

        return new InventoryDemandResult(
            list.Id,
            list.Name,
            recipientCount,
            rows);
    }

    private async Task<IReadOnlyCollection<InventoryItemResult>>
        GetItemsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        var items =
            await _database.InventoryItems
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.Variants)
                    .ThenInclude(x => x.Balances)
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x => x.Category.Name)
                .ThenBy(x => x.Name)
                .ToListAsync(
                    cancellationToken);

        return items
            .Select(item =>
                new InventoryItemResult(
                    item.Id,
                    item.CategoryId,
                    item.Category.Name,
                    item.Name,
                    item.Unit,
                    item.Description,
                    item.TrackVariants,
                    item.Variants
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.Name)
                        .Select(variant =>
                        {
                            var quantity =
                                variant.Balances.Sum(
                                    x => x.Quantity);

                            return new InventoryVariantResult(
                                variant.Id,
                                item.Id,
                                variant.Name,
                                variant.Sku,
                                variant.ReorderLevel,
                                variant.CostPrice,
                                variant.SellingPrice,
                                quantity,
                                quantity <=
                                    variant.ReorderLevel);
                        })
                        .ToList()))
            .ToList();
    }

    private async Task<InventoryItemResult> GetItemAsync(
        Guid itemId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var items =
            await GetItemsAsync(
                tenantId,
                cancellationToken);

        return items.FirstOrDefault(
                   x => x.Id == itemId)
               ?? throw new InvalidOperationException(
                   "Inventory item was not found.");
    }

    private async Task<IReadOnlyCollection<InventoryListResult>>
        GetListsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        var ids =
            await _database.InventoryLists
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => x.Id)
                .ToListAsync(
                    cancellationToken);

        var results =
            new List<InventoryListResult>();

        foreach (var id in ids)
        {
            results.Add(
                await GetListAsync(
                    id,
                    tenantId,
                    cancellationToken));
        }

        return results;
    }

    private async Task<InventoryListResult> GetListAsync(
        Guid listId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var list =
            await _database.InventoryLists
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == listId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Inventory list was not found.");

        var items =
            await _database.InventoryListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.InventoryListId == list.Id)
                .OrderBy(x =>
                    x.InventoryItemVariant.InventoryItem.Name)
                .Select(x =>
                    new InventoryListItemResult(
                        x.Id,
                        x.InventoryItemVariantId,
                        x.InventoryItemVariant.InventoryItem.Name,
                        x.InventoryItemVariant.Name,
                        x.InventoryItemVariant.InventoryItem.Unit,
                        x.QuantityPerRecipient,
                        x.IsRequired))
                .ToListAsync(
                    cancellationToken);

        var audienceName =
            await GetAudienceNameAsync(
                tenantId,
                list.AudienceType,
                list.AudienceId,
                cancellationToken);

        return new InventoryListResult(
            list.Id,
            list.Name,
            list.ListType,
            list.AudienceType,
            list.AudienceId,
            audienceName,
            list.AcademicSessionId,
            list.AcademicTermId,
            items);
    }

    private async Task<IReadOnlyCollection<InventoryTransactionResult>>
        GetRecentTransactionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        var ids =
            await _database.InventoryTransactions
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId)
                .OrderByDescending(x =>
                    x.CreatedAtUtc)
                .Take(50)
                .Select(x => x.Id)
                .ToListAsync(
                    cancellationToken);

        var results =
            new List<InventoryTransactionResult>();

        foreach (var id in ids)
        {
            results.Add(
                await GetTransactionAsync(
                    id,
                    tenantId,
                    cancellationToken));
        }

        return results;
    }

    private async Task<InventoryTransactionResult>
        GetTransactionAsync(
            Guid transactionId,
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        var transaction =
            await _database.InventoryTransactions
                .AsNoTracking()
                .Include(x =>
                    x.InventoryItemVariant)
                    .ThenInclude(x =>
                        x.InventoryItem)
                .SingleAsync(
                    x =>
                        x.Id == transactionId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        string? fromName = null;
        string? toName = null;

        if (transaction.FromLocationId.HasValue)
        {
            fromName =
                await _database.InventoryLocations
                    .Where(x =>
                        x.Id ==
                            transaction.FromLocationId.Value)
                    .Select(x => x.Name)
                    .SingleOrDefaultAsync(
                        cancellationToken);
        }

        if (transaction.ToLocationId.HasValue)
        {
            toName =
                await _database.InventoryLocations
                    .Where(x =>
                        x.Id ==
                            transaction.ToLocationId.Value)
                    .Select(x => x.Name)
                    .SingleOrDefaultAsync(
                        cancellationToken);
        }

        return new InventoryTransactionResult(
            transaction.Id,
            transaction.InventoryItemVariantId,
            transaction.InventoryItemVariant.InventoryItem.Name,
            transaction.InventoryItemVariant.Name,
            transaction.Type,
            transaction.Quantity,
            fromName,
            toName,
            transaction.RecipientType,
            transaction.RecipientId,
            transaction.RecipientName,
            transaction.UnitPrice,
            transaction.Notes,
            transaction.CreatedAtUtc);
    }

    private async Task<InventoryStockBalance>
        GetOrCreateBalanceAsync(
            Guid tenantId,
            Guid variantId,
            Guid locationId,
            CancellationToken cancellationToken)
    {
        var balance =
            await _database.InventoryStockBalances
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.InventoryItemVariantId ==
                            variantId &&
                        x.InventoryLocationId ==
                            locationId,
                    cancellationToken);

        if (balance is not null)
        {
            return balance;
        }

        balance =
            new InventoryStockBalance(
                tenantId,
                variantId,
                locationId);

        _database.InventoryStockBalances.Add(
            balance);

        return balance;
    }

    private async Task ValidateLocationAsync(
        Guid tenantId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var exists =
            await _database.InventoryLocations
                .AnyAsync(
                    x =>
                        x.Id == locationId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Inventory location was not found.");
        }
    }

    private async Task<string?> ResolveRecipientNameAsync(
        Guid tenantId,
        string? recipientType,
        Guid? recipientId,
        string? suppliedName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipientType))
        {
            return string.IsNullOrWhiteSpace(suppliedName)
                ? null
                : suppliedName.Trim();
        }

        if (string.Equals(
                recipientType,
                "Student",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!recipientId.HasValue)
            {
                throw new InvalidOperationException(
                    "Student recipient is required.");
            }

            return await _database.Students
                .Where(x =>
                    x.Id == recipientId.Value &&
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Select(x =>
                    x.FirstName + " " + x.LastName)
                .SingleOrDefaultAsync(
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Student recipient was not found.");
        }

        if (string.Equals(
                recipientType,
                "Staff",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!recipientId.HasValue)
            {
                throw new InvalidOperationException(
                    "Staff recipient is required.");
            }

            return await _database.StaffMembers
                .Where(x =>
                    x.Id == recipientId.Value &&
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Select(x =>
                    x.FirstName + " " + x.LastName)
                .SingleOrDefaultAsync(
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Staff recipient was not found.");
        }

        if (string.Equals(
                recipientType,
                "Class",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!recipientId.HasValue)
            {
                throw new InvalidOperationException(
                    "Class recipient is required.");
            }

            return await _database.ClassGroups
                .Where(x =>
                    x.Id == recipientId.Value &&
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Select(x =>
                    x.AcademicLevel.Name +
                    " — " +
                    x.Name)
                .SingleOrDefaultAsync(
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Class recipient was not found.");
        }

        if (string.IsNullOrWhiteSpace(suppliedName))
        {
            throw new InvalidOperationException(
                "Recipient name is required.");
        }

        return suppliedName.Trim();
    }

    private static void ValidateAudience(
        string audienceType,
        Guid? audienceId)
    {
        var allowed =
            new[]
            {
                "School",
                "Level",
                "Class",
                "Staff"
            };

        if (!allowed.Contains(
                audienceType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Audience must be School, Level, Class or Staff.");
        }

        if (!string.Equals(
                audienceType,
                "School",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                audienceType,
                "Staff",
                StringComparison.OrdinalIgnoreCase) &&
            !audienceId.HasValue)
        {
            throw new InvalidOperationException(
                "An academic level or class must be selected.");
        }
    }

    private async Task<string?> GetAudienceNameAsync(
        Guid tenantId,
        string audienceType,
        Guid? audienceId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(
                audienceType,
                "School",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Entire School";
        }

        if (string.Equals(
                audienceType,
                "Staff",
                StringComparison.OrdinalIgnoreCase))
        {
            return "All Staff";
        }

        if (!audienceId.HasValue)
        {
            return null;
        }

        if (string.Equals(
                audienceType,
                "Level",
                StringComparison.OrdinalIgnoreCase))
        {
            return await _database.AcademicLevels
                .Where(x =>
                    x.Id == audienceId.Value &&
                    x.TenantId == tenantId)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(
                    cancellationToken);
        }

        return await _database.ClassGroups
            .Where(x =>
                x.Id == audienceId.Value &&
                x.TenantId == tenantId)
            .Select(x =>
                x.AcademicLevel.Name +
                " — " +
                x.Name)
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private async Task<int> GetRecipientCountAsync(
        Guid tenantId,
        InventoryList list,
        CancellationToken cancellationToken)
    {
        if (string.Equals(
                list.AudienceType,
                "Staff",
                StringComparison.OrdinalIgnoreCase))
        {
            return await _database.StaffMembers
                .CountAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);
        }

        var currentSessionId =
            list.AcademicSessionId ??
            await _database.AcademicSessions
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent &&
                    x.IsActive)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (!currentSessionId.HasValue)
        {
            return 0;
        }

        var enrollments =
            _database.StudentEnrollments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId ==
                        currentSessionId.Value &&
                    x.IsCurrent &&
                    x.IsActive &&
                    x.Student.IsActive);

        if (string.Equals(
                list.AudienceType,
                "Class",
                StringComparison.OrdinalIgnoreCase))
        {
            enrollments =
                enrollments.Where(x =>
                    x.ClassGroupId ==
                        list.AudienceId);
        }

        if (string.Equals(
                list.AudienceType,
                "Level",
                StringComparison.OrdinalIgnoreCase))
        {
            enrollments =
                enrollments.Where(x =>
                    x.ClassGroup.AcademicLevelId ==
                        list.AudienceId);
        }

        return await enrollments.CountAsync(
            cancellationToken);
    }
}
