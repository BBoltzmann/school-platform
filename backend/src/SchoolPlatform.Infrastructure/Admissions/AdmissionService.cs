using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Admissions;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Students;
using SchoolPlatform.Domain.Admissions;
using SchoolPlatform.Domain.Students;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Admissions;

public sealed class AdmissionService : IAdmissionService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public AdmissionService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyCollection<AdmissionApplicationResult>> GetApplicationsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _database.AdmissionApplications
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new AdmissionApplicationResult(
                x.Id,
                x.ApplicationNumber,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.DateOfBirth,
                x.Gender,
                x.Email,
                x.Phone,
                x.Religion,
                x.PreviousSchoolName,
                x.PresentClass,
                x.GuardianName,
                x.GuardianHomeAddress,
                x.GuardianOccupation,
                x.GuardianPhone,
                x.GuardianOfficeAddress,
                x.AcademicSessionId,
                x.AcademicSession.Name,
                x.AcademicLevelId,
                x.AcademicLevel.Name,
                x.Status,
                x.SubmittedAtUtc,
                x.ReviewedAtUtc,
                x.DecisionAtUtc,
                x.DecisionNote,
                x.ApprovedStudentId,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }


    public async Task<AdmissionApplicationResult?> GetApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _database.AdmissionApplications
            .AsNoTracking()
            .Where(x =>
                x.Id == applicationId &&
                x.TenantId == tenantId &&
                x.IsActive)
            .Select(x => new AdmissionApplicationResult(
                x.Id,
                x.ApplicationNumber,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.DateOfBirth,
                x.Gender,
                x.Email,
                x.Phone,
                x.Religion,
                x.PreviousSchoolName,
                x.PresentClass,
                x.GuardianName,
                x.GuardianHomeAddress,
                x.GuardianOccupation,
                x.GuardianPhone,
                x.GuardianOfficeAddress,
                x.AcademicSessionId,
                x.AcademicSession.Name,
                x.AcademicLevelId,
                x.AcademicLevel.Name,
                x.Status,
                x.SubmittedAtUtc,
                x.ReviewedAtUtc,
                x.DecisionAtUtc,
                x.DecisionNote,
                x.ApprovedStudentId,
                x.IsActive))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<AdmissionSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var currentSession = await _database.AcademicSessions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsCurrent &&
                x.IsActive)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new StudentSessionOption(
                x.Id,
                x.Name,
                x.StartDate,
                x.EndDate))
            .FirstOrDefaultAsync(cancellationToken);

        var levels = await _database.AcademicLevels
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new StudentLevelOption(
                x.Id,
                x.Name,
                x.Category,
                x.SortOrder))
            .ToListAsync(cancellationToken);

        var classes = await _database.ClassGroups
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive &&
                x.AcademicLevel.IsActive)
            .OrderBy(x => x.AcademicLevel.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new StudentClassOption(
                x.Id,
                x.Name,
                x.AcademicLevelId,
                x.AcademicLevel.Name,
                x.CampusId))
            .ToListAsync(cancellationToken);

        return new AdmissionSetupResult(
            currentSession,
            levels,
            classes);
    }

    public async Task<AdmissionApplicationResult> CreateAsync(
        CreateAdmissionApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        ValidateApplicant(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.Religion,
            request.GuardianName,
            request.GuardianHomeAddress,
            request.GuardianOccupation,
            request.GuardianPhone);

        var session = await _database.AcademicSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.Id == request.AcademicSessionId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException(
                "Academic session was not found.");
        }

        var level = await _database.AcademicLevels
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.Id == request.AcademicLevelId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (level is null)
        {
            throw new InvalidOperationException(
                "Academic level was not found.");
        }

        var applicationNumber =
            await GenerateApplicationNumberAsync(
                tenantId,
                cancellationToken);

        var application =
            new AdmissionApplication(
                tenantId,
                applicationNumber,
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.Email,
                request.Phone,
                request.Religion,
                request.PreviousSchoolName,
                request.PresentClass,
                request.GuardianName,
                request.GuardianHomeAddress,
                request.GuardianOccupation,
                request.GuardianPhone,
                request.GuardianOfficeAddress,
                session.Id,
                level.Id);

        _database.AdmissionApplications.Add(
            application);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredApplicationAsync(
            application.Id,
            cancellationToken);
    }

    public async Task<AdmissionApplicationResult> MarkUnderReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application =
            await GetTrackedApplicationAsync(
                applicationId,
                cancellationToken);

        application.MarkUnderReview();

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredApplicationAsync(
            applicationId,
            cancellationToken);
    }

    public async Task<AdmissionApplicationResult> WaitlistAsync(
        Guid applicationId,
        AdmissionDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var application =
            await GetTrackedApplicationAsync(
                applicationId,
                cancellationToken);

        application.Waitlist(request.Note);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredApplicationAsync(
            applicationId,
            cancellationToken);
    }

    public async Task<AdmissionApplicationResult> RejectAsync(
        Guid applicationId,
        AdmissionDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var application =
            await GetTrackedApplicationAsync(
                applicationId,
                cancellationToken);

        application.Reject(request.Note);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredApplicationAsync(
            applicationId,
            cancellationToken);
    }

    public async Task<AdmissionApplicationResult> ApproveAsync(
        Guid applicationId,
        ApproveAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var application =
            await _database.AdmissionApplications
                .Include(x => x.AcademicSession)
                .Include(x => x.AcademicLevel)
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == applicationId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (application is null)
        {
            throw new InvalidOperationException(
                "Admission application was not found.");
        }

        if (application.ApprovedStudentId is not null ||
            string.Equals(
                application.Status,
                "Approved",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "This application has already been approved.");
        }

        var admissionNumber =
            request.AdmissionNumber
                .Trim()
                .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(
                admissionNumber))
        {
            throw new InvalidOperationException(
                "Admission number is required.");
        }

        var duplicateAdmissionNumber =
            await _database.Students.AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.AdmissionNumber ==
                        admissionNumber,
                cancellationToken);

        if (duplicateAdmissionNumber)
        {
            throw new InvalidOperationException(
                $"Student admission number '{admissionNumber}' already exists.");
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

        if (classGroup.AcademicLevelId !=
            application.AcademicLevelId)
        {
            throw new InvalidOperationException(
                "The selected class does not belong to the applicant's requested academic level.");
        }

        ValidateEnrollmentDate(
            request.EnrollmentDate,
            application.AcademicSession.StartDate,
            application.AcademicSession.EndDate);

        await using var transaction =
            await _database.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var student = new Student(
                tenantId,
                admissionNumber,
                application.FirstName,
                application.MiddleName,
                application.LastName,
                application.DateOfBirth,
                application.Gender,
                request.EnrollmentDate,
                application.Email,
                application.Phone);

            _database.Students.Add(student);

            await _database.SaveChangesAsync(
                cancellationToken);

            var enrollment =
                new StudentEnrollment(
                    tenantId,
                    student.Id,
                    application.AcademicSessionId,
                    application.AcademicLevelId,
                    classGroup.Id,
                    request.EnrollmentDate,
                    true);

            _database.StudentEnrollments.Add(
                enrollment);

            await CreateOrLinkGuardianAsync(
                tenantId,
                student.Id,
                application,
                cancellationToken);

            application.Approve(
                student.Id,
                request.Note);

            await _database.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }

        return await GetRequiredApplicationAsync(
            applicationId,
            cancellationToken);
    }

    private async Task CreateOrLinkGuardianAsync(
        Guid tenantId,
        Guid studentId,
        AdmissionApplication application,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                application.GuardianName) ||
            string.IsNullOrWhiteSpace(
                application.GuardianPhone))
        {
            return;
        }

        var phone =
            application.GuardianPhone.Trim();

        var email =
            string.IsNullOrWhiteSpace(
                application.Email)
                ? null
                : application.Email
                    .Trim()
                    .ToLowerInvariant();

        Guardian? guardian = null;

        if (email is not null)
        {
            guardian =
                await _database.Guardians
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId ==
                                tenantId &&
                            x.IsActive &&
                            (
                                x.Phone == phone ||
                                x.Email == email
                            ),
                        cancellationToken);
        }
        else
        {
            guardian =
                await _database.Guardians
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId ==
                                tenantId &&
                            x.IsActive &&
                            x.Phone == phone,
                        cancellationToken);
        }

        if (guardian is null)
        {
            var nameParts =
                SplitGuardianName(
                    application.GuardianName);

            guardian = new Guardian(
                tenantId,
                nameParts.FirstName,
                nameParts.MiddleName,
                nameParts.LastName,
                email,
                phone,
                null,
                application.GuardianOccupation,
                application.GuardianHomeAddress);

            _database.Guardians.Add(
                guardian);

            await _database.SaveChangesAsync(
                cancellationToken);
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
                            guardian.Id,
                    cancellationToken);

        if (alreadyLinked)
        {
            return;
        }

        var link = new StudentGuardian(
            tenantId,
            studentId,
            guardian.Id,
            "Guardian",
            true,
            false,
            false,
            false);

        _database.StudentGuardians.Add(
            link);
    }

    private async Task<AdmissionApplication> GetTrackedApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var tenantId =
            _tenantContext.TenantId;

        return await _database
            .AdmissionApplications
            .SingleOrDefaultAsync(
                x =>
                    x.Id == applicationId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Admission application was not found.");
    }

    private async Task<AdmissionApplicationResult> GetRequiredApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var tenantId =
            _tenantContext.TenantId;

        var result =
            await _database
                .AdmissionApplications
                .AsNoTracking()
                .Where(x =>
                    x.Id == applicationId &&
                    x.TenantId == tenantId)
                .Select(x =>
                    new AdmissionApplicationResult(
                        x.Id,
                        x.ApplicationNumber,
                        x.FirstName,
                        x.MiddleName,
                        x.LastName,
                        x.DateOfBirth,
                        x.Gender,
                        x.Email,
                        x.Phone,
                        x.Religion,
                        x.PreviousSchoolName,
                        x.PresentClass,
                        x.GuardianName,
                        x.GuardianHomeAddress,
                        x.GuardianOccupation,
                        x.GuardianPhone,
                        x.GuardianOfficeAddress,
                        x.AcademicSessionId,
                        x.AcademicSession.Name,
                        x.AcademicLevelId,
                        x.AcademicLevel.Name,
                        x.Status,
                        x.SubmittedAtUtc,
                        x.ReviewedAtUtc,
                        x.DecisionAtUtc,
                        x.DecisionNote,
                        x.ApprovedStudentId,
                        x.IsActive))
                .SingleOrDefaultAsync(
                    cancellationToken);

        return result
            ?? throw new InvalidOperationException(
                "Admission application was not found.");
    }

    private async Task<string> GenerateApplicationNumberAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0;
             attempt < 10;
             attempt++)
        {
            var candidate =
                $"APP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

            var exists =
                await _database
                    .AdmissionApplications
                    .AnyAsync(
                        x =>
                            x.TenantId ==
                                tenantId &&
                            x.ApplicationNumber ==
                                candidate,
                        cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Unable to generate a unique application number.");
    }

    private static (
        string FirstName,
        string? MiddleName,
        string LastName)
        SplitGuardianName(
            string fullName)
    {
        var parts = fullName
            .Split(
                ' ',
                StringSplitOptions
                    .RemoveEmptyEntries |
                StringSplitOptions
                    .TrimEntries);

        if (parts.Length == 0)
        {
            return (
                "Guardian",
                null,
                "");
        }

        if (parts.Length == 1)
        {
            return (
                parts[0],
                null,
                "");
        }

        if (parts.Length == 2)
        {
            return (
                parts[0],
                null,
                parts[1]);
        }

        return (
            parts[0],
            string.Join(
                " ",
                parts.Skip(1)
                    .Take(
                        parts.Length - 2)),
            parts[^1]);
    }

    private static void ValidateApplicant(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string gender,
        string? religion,
        string? guardianName,
        string? guardianHomeAddress,
        string? guardianOccupation,
        string? guardianPhone)
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

        if (dateOfBirth >=
            DateOnly.FromDateTime(
                DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                "Date of birth must be in the past.");
        }

        if (string.IsNullOrWhiteSpace(
                gender))
        {
            throw new InvalidOperationException(
                "Gender is required.");
        }

        if (string.IsNullOrWhiteSpace(
                religion))
        {
            throw new InvalidOperationException(
                "Religion is required.");
        }

        if (string.IsNullOrWhiteSpace(
                guardianName))
        {
            throw new InvalidOperationException(
                "Guardian name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                guardianHomeAddress))
        {
            throw new InvalidOperationException(
                "Guardian home address is required.");
        }

        if (string.IsNullOrWhiteSpace(
                guardianOccupation))
        {
            throw new InvalidOperationException(
                "Guardian occupation is required.");
        }

        if (string.IsNullOrWhiteSpace(
                guardianPhone))
        {
            throw new InvalidOperationException(
                "Guardian telephone number is required.");
        }
    }

    private static void ValidateEnrollmentDate(
        DateOnly enrollmentDate,
        DateOnly sessionStartDate,
        DateOnly sessionEndDate)
    {
        if (enrollmentDate <
                sessionStartDate ||
            enrollmentDate >
                sessionEndDate)
        {
            throw new InvalidOperationException(
                $"Enrolment date must fall between {sessionStartDate:yyyy-MM-dd} and {sessionEndDate:yyyy-MM-dd}.");
        }
    }
}
