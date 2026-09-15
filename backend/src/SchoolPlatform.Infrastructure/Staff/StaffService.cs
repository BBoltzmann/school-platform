using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Staff;
using SchoolPlatform.Domain.Staff;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Staff;

public sealed class StaffService : IStaffService
{
    private static readonly string[] AllowedStatuses =
    {
        "Active",
        "Inactive",
        "Suspended",
        "Resigned",
        "Terminated",
        "Retired"
    };

    private static readonly string[] AllowedEmploymentTypes =
    {
        "Full-Time",
        "Part-Time",
        "Contract",
        "Temporary"
    };

    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public StaffService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyCollection<StaffMemberResult>> GetStaffAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _database.StaffMembers
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new StaffMemberResult(
                x.Id,
                x.StaffNumber,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.Gender,
                x.DateOfBirth,
                x.Email,
                x.Phone,
                x.Address,
                x.EmploymentDate,
                x.JobTitle,
                x.Department,
                x.EmploymentType,
                x.IsTeachingStaff,
                x.Status,
                x.IsActive,
                x.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<StaffMemberResult?> GetStaffMemberAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _database.StaffMembers
            .AsNoTracking()
            .Where(x =>
                x.Id == staffId &&
                x.TenantId == tenantId)
            .Select(x => new StaffMemberResult(
                x.Id,
                x.StaffNumber,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.Gender,
                x.DateOfBirth,
                x.Email,
                x.Phone,
                x.Address,
                x.EmploymentDate,
                x.JobTitle,
                x.Department,
                x.EmploymentType,
                x.IsTeachingStaff,
                x.Status,
                x.IsActive,
                x.UserId))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<StaffMemberResult> CreateAsync(
        CreateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        ValidateStaff(
            request.FirstName,
            request.LastName,
            request.Gender,
            request.DateOfBirth,
            request.Phone,
            request.EmploymentDate,
            request.JobTitle,
            request.EmploymentType);

        var staffNumber =
            request.StaffNumber
                .Trim()
                .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(
                staffNumber))
        {
            throw new InvalidOperationException(
                "Staff number is required.");
        }

        var duplicate =
            await _database.StaffMembers
                .AnyAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.StaffNumber == staffNumber,
                    cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Staff number '{staffNumber}' already exists.");
        }

        var staff =
            new StaffMember(
                tenantId,
                staffNumber,
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.Gender,
                request.DateOfBirth,
                request.Email,
                request.Phone,
                request.Address,
                request.EmploymentDate,
                request.JobTitle,
                request.Department,
                request.EmploymentType,
                request.IsTeachingStaff);

        _database.StaffMembers.Add(
            staff);

        await _database.SaveChangesAsync(
            cancellationToken);

        return ToResult(staff);
    }

    public async Task<StaffMemberResult> UpdateAsync(
        Guid staffId,
        UpdateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        ValidateStaff(
            request.FirstName,
            request.LastName,
            request.Gender,
            request.DateOfBirth,
            request.Phone,
            request.EmploymentDate,
            request.JobTitle,
            request.EmploymentType);

        if (!AllowedStatuses.Contains(
                request.Status,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Staff status is invalid.");
        }

        var staff =
            await _database.StaffMembers
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == staffId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (staff is null)
        {
            throw new InvalidOperationException(
                "Staff member was not found.");
        }

        staff.UpdatePersonalInformation(
            request.FirstName,
            request.MiddleName,
            request.LastName,
            request.Gender,
            request.DateOfBirth,
            request.Email,
            request.Phone,
            request.Address);

        staff.UpdateEmployment(
            request.EmploymentDate,
            request.JobTitle,
            request.Department,
            request.EmploymentType,
            request.IsTeachingStaff);

        staff.SetStatus(
            request.Status);

        await _database.SaveChangesAsync(
            cancellationToken);

        return ToResult(staff);
    }

    private static StaffMemberResult ToResult(
        StaffMember staff)
    {
        return new StaffMemberResult(
            staff.Id,
            staff.StaffNumber,
            staff.FirstName,
            staff.MiddleName,
            staff.LastName,
            staff.Gender,
            staff.DateOfBirth,
            staff.Email,
            staff.Phone,
            staff.Address,
            staff.EmploymentDate,
            staff.JobTitle,
            staff.Department,
            staff.EmploymentType,
            staff.IsTeachingStaff,
            staff.Status,
            staff.IsActive,
            staff.UserId);
    }

    private static void ValidateStaff(
        string firstName,
        string lastName,
        string gender,
        DateOnly? dateOfBirth,
        string phone,
        DateOnly employmentDate,
        string jobTitle,
        string employmentType)
    {
        if (string.IsNullOrWhiteSpace(
                firstName))
        {
            throw new InvalidOperationException(
                "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                lastName))
        {
            throw new InvalidOperationException(
                "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                gender))
        {
            throw new InvalidOperationException(
                "Gender is required.");
        }

        if (dateOfBirth is not null &&
            dateOfBirth.Value >=
                DateOnly.FromDateTime(
                    DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                "Date of birth must be in the past.");
        }

        if (string.IsNullOrWhiteSpace(
                phone))
        {
            throw new InvalidOperationException(
                "Telephone number is required.");
        }

        if (employmentDate >
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)))
        {
            throw new InvalidOperationException(
                "Employment date is invalid.");
        }

        if (string.IsNullOrWhiteSpace(
                jobTitle))
        {
            throw new InvalidOperationException(
                "Job title is required.");
        }

        if (!AllowedEmploymentTypes.Contains(
                employmentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Employment type is invalid.");
        }
    }
}
