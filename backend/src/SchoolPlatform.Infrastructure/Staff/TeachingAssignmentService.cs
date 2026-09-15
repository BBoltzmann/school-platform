using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Staff;
using SchoolPlatform.Domain.Staff;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Staff;

public sealed class TeachingAssignmentService
    : ITeachingAssignmentService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public TeachingAssignmentService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<TeachingAssignmentSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var currentSession =
            await _database.AcademicSessions
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent &&
                    x.IsActive)
                .OrderByDescending(x =>
                    x.StartDate)
                .Select(x =>
                    new TeachingAssignmentSessionOption(
                        x.Id,
                        x.Name,
                        x.StartDate,
                        x.EndDate))
                .FirstOrDefaultAsync(
                    cancellationToken);

        var teachingStaff =
            await _database.StaffMembers
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsTeachingStaff &&
                    x.IsActive)
                .OrderBy(x =>
                    x.LastName)
                .ThenBy(x =>
                    x.FirstName)
                .Select(x =>
                    new TeachingAssignmentStaffOption(
                        x.Id,
                        x.StaffNumber,
                        x.MiddleName == null
                            ? x.FirstName + " " + x.LastName
                            : x.FirstName + " " + x.MiddleName + " " + x.LastName,
                        x.JobTitle,
                        x.Department,
                        x.EmploymentType))
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
                    x.AcademicLevel.SortOrder)
                .ThenBy(x =>
                    x.Name)
                .Select(x =>
                    new TeachingAssignmentClassOption(
                        x.Id,
                        x.Name,
                        x.AcademicLevelId,
                        x.AcademicLevel.Name,
                        x.UsesCustomSubjectOffering,
                        x.ClassSubjects.Select(cs => cs.SubjectId).ToList()))
                .ToListAsync(
                    cancellationToken);

        var subjects =
            await _database.Subjects
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.Name)
                .Select(x =>
                    new TeachingAssignmentSubjectOption(
                        x.Id,
                        x.Name))
                .ToListAsync(
                    cancellationToken);

        return new TeachingAssignmentSetupResult(
            currentSession,
            teachingStaff,
            classes,
            subjects);
    }

    public async Task<IReadOnlyCollection<TeachingAssignmentResult>> GetForStaffAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var staffExists =
            await _database.StaffMembers
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == staffId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (!staffExists)
        {
            throw new InvalidOperationException(
                "Staff member was not found.");
        }

        return await _database.TeachingAssignments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.StaffMemberId == staffId &&
                x.IsActive)
            .OrderByDescending(x =>
                x.AcademicSession.StartDate)
            .ThenBy(x =>
                x.ClassGroup.AcademicLevel.SortOrder)
            .ThenBy(x =>
                x.ClassGroup.Name)
            .ThenBy(x =>
                x.Subject.Name)
            .Select(x =>
                new TeachingAssignmentResult(
                    x.Id,
                    x.StaffMemberId,
                    x.StaffMember.StaffNumber,
                    x.StaffMember.MiddleName == null
                        ? x.StaffMember.FirstName + " " + x.StaffMember.LastName
                        : x.StaffMember.FirstName + " " + x.StaffMember.MiddleName + " " + x.StaffMember.LastName,
                    x.AcademicSessionId,
                    x.AcademicSession.Name,
                    x.ClassGroupId,
                    x.ClassGroup.Name,
                    x.ClassGroup.AcademicLevelId,
                    x.ClassGroup.AcademicLevel.Name,
                    x.SubjectId,
                    x.Subject.Name,
                    x.IsActive))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<TeachingAssignmentResult> CreateAsync(
        CreateTeachingAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var staff =
            await _database.StaffMembers
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.StaffMemberId &&
                        x.TenantId ==
                            tenantId,
                    cancellationToken);

        if (staff is null)
        {
            throw new InvalidOperationException(
                "Staff member was not found.");
        }

        if (!staff.IsActive)
        {
            throw new InvalidOperationException(
                "Only active staff members can receive teaching assignments.");
        }

        if (!staff.IsTeachingStaff)
        {
            throw new InvalidOperationException(
                "This staff member is not marked as Teaching Staff.");
        }

        var session =
            await _database.AcademicSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.AcademicSessionId &&
                        x.TenantId ==
                            tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException(
                "Academic session was not found.");
        }

        var classGroup =
            await _database.ClassGroups
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.ClassGroupId &&
                        x.TenantId ==
                            tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (classGroup is null)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        var subject =
            await _database.Subjects
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.SubjectId &&
                        x.TenantId ==
                            tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (subject is null)
        {
            throw new InvalidOperationException(
                "Subject was not found.");
        }

        if (classGroup.UsesCustomSubjectOffering &&
            !await _database.ClassSubjects.AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ClassGroupId == classGroup.Id &&
                     x.SubjectId == subject.Id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"{subject.Name} is not configured as a subject offered by {classGroup.Name}.");
        }

        var existing =
            await _database.TeachingAssignments
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId ==
                            tenantId &&
                        x.StaffMemberId ==
                            request.StaffMemberId &&
                        x.AcademicSessionId ==
                            request.AcademicSessionId &&
                        x.ClassGroupId ==
                            request.ClassGroupId &&
                        x.SubjectId ==
                            request.SubjectId,
                    cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new InvalidOperationException(
                    "This teaching assignment already exists.");
            }

            existing.Activate();

            await _database.SaveChangesAsync(
                cancellationToken);

            return await GetRequiredAsync(
                existing.Id,
                cancellationToken);
        }

        var assignment =
            new TeachingAssignment(
                tenantId,
                request.StaffMemberId,
                request.AcademicSessionId,
                request.ClassGroupId,
                request.SubjectId);

        _database.TeachingAssignments.Add(
            assignment);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredAsync(
            assignment.Id,
            cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var assignment =
            await _database.TeachingAssignments
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            assignmentId &&
                        x.TenantId ==
                            tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (assignment is null)
        {
            throw new InvalidOperationException(
                "Teaching assignment was not found.");
        }

        assignment.Deactivate();

        await _database.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<TeachingAssignmentResult> GetRequiredAsync(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var tenantId =
            _tenantContext.TenantId;

        var result =
            await _database.TeachingAssignments
                .AsNoTracking()
                .Where(x =>
                    x.Id == assignmentId &&
                    x.TenantId == tenantId)
                .Select(x =>
                    new TeachingAssignmentResult(
                        x.Id,
                        x.StaffMemberId,
                        x.StaffMember.StaffNumber,
                        x.StaffMember.MiddleName == null
                            ? x.StaffMember.FirstName + " " + x.StaffMember.LastName
                            : x.StaffMember.FirstName + " " + x.StaffMember.MiddleName + " " + x.StaffMember.LastName,
                        x.AcademicSessionId,
                        x.AcademicSession.Name,
                        x.ClassGroupId,
                        x.ClassGroup.Name,
                        x.ClassGroup.AcademicLevelId,
                        x.ClassGroup.AcademicLevel.Name,
                        x.SubjectId,
                        x.Subject.Name,
                        x.IsActive))
                .SingleOrDefaultAsync(
                    cancellationToken);

        return result
            ?? throw new InvalidOperationException(
                "Teaching assignment was not found.");
    }
}
