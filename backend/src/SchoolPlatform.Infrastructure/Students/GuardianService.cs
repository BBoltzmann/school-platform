using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Students;
using SchoolPlatform.Domain.Students;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Students;

public sealed class GuardianService : IGuardianService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public GuardianService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyCollection<StudentGuardianResult>> GetStudentGuardiansAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var studentExists = await _database.Students
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == studentId &&
                    x.TenantId == tenantId,
                cancellationToken);

        if (!studentExists)
        {
            throw new InvalidOperationException(
                "Student was not found.");
        }

        return await _database.StudentGuardians
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.StudentId == studentId &&
                x.IsActive)
            .OrderByDescending(x => x.IsPrimaryContact)
            .ThenBy(x => x.Guardian.LastName)
            .ThenBy(x => x.Guardian.FirstName)
            .Select(x => new StudentGuardianResult(
                x.Id,
                x.StudentId,
                new GuardianResult(
                    x.Guardian.Id,
                    x.Guardian.FirstName,
                    x.Guardian.MiddleName,
                    x.Guardian.LastName,
                    x.Guardian.Email,
                    x.Guardian.Phone,
                    x.Guardian.AlternatePhone,
                    x.Guardian.Occupation,
                    x.Guardian.Address,
                    x.Guardian.IsActive),
                x.Relationship,
                x.IsPrimaryContact,
                x.IsEmergencyContact,
                x.CanPickUpStudent,
                x.LivesWithStudent,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GuardianResult>> SearchGuardiansAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _database.Guardians
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var rawSearch = search.Trim();
            var normalizedSearch =
                rawSearch.ToLowerInvariant();

            query = query.Where(x =>
                x.FirstName.ToLower().Contains(normalizedSearch) ||
                x.LastName.ToLower().Contains(normalizedSearch) ||
                x.Phone.Contains(rawSearch) ||
                (
                    x.Email != null &&
                    x.Email.ToLower().Contains(normalizedSearch)
                ));
        }

        return await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Take(20)
            .Select(x => new GuardianResult(
                x.Id,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.Email,
                x.Phone,
                x.AlternatePhone,
                x.Occupation,
                x.Address,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentGuardianResult> CreateAndLinkGuardianAsync(
        Guid studentId,
        CreateGuardianForStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureStudentExistsAsync(
            studentId,
            tenantId,
            cancellationToken);

        ValidateGuardianRequest(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Relationship);

        var normalizedPhone =
            request.Phone.Trim();

        var normalizedEmail =
            string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email
                    .Trim()
                    .ToLowerInvariant();

        var existingGuardian =
            await _database.Guardians
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Where(x =>
                    x.Phone == normalizedPhone ||
                    (
                        normalizedEmail != null &&
                        x.Email == normalizedEmail
                    ))
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName
                })
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (existingGuardian is not null)
        {
            throw new InvalidOperationException(
                $"Guardian '{existingGuardian.FirstName} {existingGuardian.LastName}' already exists. Link the existing guardian instead.");
        }

        await using var transaction =
            await _database.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            if (request.IsPrimaryContact)
            {
                await ClearPrimaryContactAsync(
                    studentId,
                    tenantId,
                    cancellationToken);
            }

            var guardian = new Guardian(
                tenantId,
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.Email,
                request.Phone,
                request.AlternatePhone,
                request.Occupation,
                request.Address);

            _database.Guardians.Add(
                guardian);

            await _database.SaveChangesAsync(
                cancellationToken);

            var link = new StudentGuardian(
                tenantId,
                studentId,
                guardian.Id,
                request.Relationship,
                request.IsPrimaryContact,
                request.IsEmergencyContact,
                request.CanPickUpStudent,
                request.LivesWithStudent);

            _database.StudentGuardians.Add(
                link);

            await _database.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return await GetLinkAsync(
                link.Id,
                tenantId,
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<StudentGuardianResult> LinkExistingGuardianAsync(
        Guid studentId,
        LinkExistingGuardianRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureStudentExistsAsync(
            studentId,
            tenantId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(
                request.Relationship))
        {
            throw new InvalidOperationException(
                "Relationship is required.");
        }

        var guardianExists =
            await _database.Guardians
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id ==
                            request.GuardianId &&
                        x.TenantId ==
                            tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (!guardianExists)
        {
            throw new InvalidOperationException(
                "Guardian was not found.");
        }

        var alreadyLinked =
            await _database.StudentGuardians
                .AnyAsync(
                    x =>
                        x.TenantId ==
                            tenantId &&
                        x.StudentId ==
                            studentId &&
                        x.GuardianId ==
                            request.GuardianId,
                    cancellationToken);

        if (alreadyLinked)
        {
            throw new InvalidOperationException(
                "This guardian is already linked to the student.");
        }

        await using var transaction =
            await _database.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            if (request.IsPrimaryContact)
            {
                await ClearPrimaryContactAsync(
                    studentId,
                    tenantId,
                    cancellationToken);
            }

            var link = new StudentGuardian(
                tenantId,
                studentId,
                request.GuardianId,
                request.Relationship,
                request.IsPrimaryContact,
                request.IsEmergencyContact,
                request.CanPickUpStudent,
                request.LivesWithStudent);

            _database.StudentGuardians.Add(
                link);

            await _database.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return await GetLinkAsync(
                link.Id,
                tenantId,
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task EnsureStudentExistsAsync(
        Guid studentId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var exists = await _database.Students
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == studentId &&
                    x.TenantId == tenantId,
                cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Student was not found.");
        }
    }

    private static void ValidateGuardianRequest(
        string firstName,
        string lastName,
        string phone,
        string relationship)
    {
        if (string.IsNullOrWhiteSpace(
                firstName))
        {
            throw new InvalidOperationException(
                "Guardian first name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                lastName))
        {
            throw new InvalidOperationException(
                "Guardian last name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                phone))
        {
            throw new InvalidOperationException(
                "Guardian phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(
                relationship))
        {
            throw new InvalidOperationException(
                "Relationship is required.");
        }
    }

    private async Task ClearPrimaryContactAsync(
        Guid studentId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var existingPrimaryContacts =
            await _database.StudentGuardians
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.StudentId == studentId &&
                    x.IsPrimaryContact &&
                    x.IsActive)
                .ToListAsync(
                    cancellationToken);

        foreach (var link in existingPrimaryContacts)
        {
            link.UpdateRelationship(
                link.Relationship,
                false,
                link.IsEmergencyContact,
                link.CanPickUpStudent,
                link.LivesWithStudent);
        }

        if (existingPrimaryContacts.Count > 0)
        {
            await _database.SaveChangesAsync(
                cancellationToken);
        }
    }

    private async Task<StudentGuardianResult> GetLinkAsync(
        Guid linkId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _database.StudentGuardians
            .AsNoTracking()
            .Where(x =>
                x.Id == linkId &&
                x.TenantId == tenantId)
            .Select(x =>
                new StudentGuardianResult(
                    x.Id,
                    x.StudentId,
                    new GuardianResult(
                        x.Guardian.Id,
                        x.Guardian.FirstName,
                        x.Guardian.MiddleName,
                        x.Guardian.LastName,
                        x.Guardian.Email,
                        x.Guardian.Phone,
                        x.Guardian.AlternatePhone,
                        x.Guardian.Occupation,
                        x.Guardian.Address,
                        x.Guardian.IsActive),
                    x.Relationship,
                    x.IsPrimaryContact,
                    x.IsEmergencyContact,
                    x.CanPickUpStudent,
                    x.LivesWithStudent,
                    x.IsActive))
            .SingleAsync(
                cancellationToken);
    }
}
