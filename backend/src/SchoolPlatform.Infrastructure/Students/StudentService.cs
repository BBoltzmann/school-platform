using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Students;
using SchoolPlatform.Domain.Students;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Students;

public sealed class StudentService : IStudentService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public StudentService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyCollection<StudentListItemResult>> GetStudentsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _database.Students
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new StudentListItemResult(
                x.Id,
                x.AdmissionNumber,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.DateOfBirth,
                x.Gender,
                x.AdmissionDate,
                x.Email,
                x.Phone,
                x.Status,
                x.IsActive,
                x.Enrollments
                    .Where(enrollment =>
                        enrollment.TenantId == tenantId &&
                        enrollment.IsCurrent &&
                        enrollment.IsActive)
                    .OrderByDescending(enrollment =>
                        enrollment.EnrollmentDate)
                    .Select(enrollment =>
                        new StudentEnrollmentResult(
                            enrollment.Id,
                            enrollment.AcademicSessionId,
                            enrollment.AcademicSession.Name,
                            enrollment.AcademicLevelId,
                            enrollment.AcademicLevel.Name,
                            enrollment.ClassGroupId,
                            enrollment.ClassGroup.Name,
                            enrollment.EnrollmentDate,
                            enrollment.IsCurrent))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentDetailResult?> GetStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _database.Students
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId &&
                x.TenantId == tenantId)
            .Select(x => new StudentDetailResult(
                x.Id,
                x.AdmissionNumber,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.DateOfBirth,
                x.Gender,
                x.AdmissionDate,
                x.Email,
                x.Phone,
                x.Status,
                x.IsActive,
                x.Enrollments
                    .Where(enrollment =>
                        enrollment.TenantId == tenantId)
                    .OrderByDescending(enrollment =>
                        enrollment.IsCurrent)
                    .ThenByDescending(enrollment =>
                        enrollment.AcademicSession.StartDate)
                    .Select(enrollment =>
                        new StudentEnrollmentResult(
                            enrollment.Id,
                            enrollment.AcademicSessionId,
                            enrollment.AcademicSession.Name,
                            enrollment.AcademicLevelId,
                            enrollment.AcademicLevel.Name,
                            enrollment.ClassGroupId,
                            enrollment.ClassGroup.Name,
                            enrollment.EnrollmentDate,
                            enrollment.IsCurrent))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<StudentSetupResult> GetSetupAsync(
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

        return new StudentSetupResult(
            currentSession,
            levels,
            classes);
    }

    public async Task<StudentListItemResult> CreateStudentAsync(
        CreateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var admissionNumber = request.AdmissionNumber
            .Trim()
            .ToUpperInvariant();

        ValidateStudentDetails(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender);

        if (string.IsNullOrWhiteSpace(admissionNumber))
        {
            throw new InvalidOperationException(
                "Admission number is required.");
        }

        var duplicateStudent = await _database.Students
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.AdmissionNumber == admissionNumber,
                cancellationToken);

        if (duplicateStudent)
        {
            throw new InvalidOperationException(
                $"Student admission number '{admissionNumber}' already exists.");
        }

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

        ValidateEnrollmentDate(
            request.AdmissionDate,
            session.StartDate,
            session.EndDate);

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

        var classGroup = await _database.ClassGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.Id == request.ClassGroupId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (classGroup is null)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        if (classGroup.AcademicLevelId != level.Id)
        {
            throw new InvalidOperationException(
                $"Class '{classGroup.Name}' does not belong to academic level '{level.Name}'.");
        }

        await using var transaction =
            await _database.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var student = new Student(
                tenantId,
                admissionNumber,
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.AdmissionDate,
                request.Email,
                request.Phone);

            _database.Students.Add(student);

            await _database.SaveChangesAsync(
                cancellationToken);

            var enrollment = new StudentEnrollment(
                tenantId,
                student.Id,
                session.Id,
                level.Id,
                classGroup.Id,
                request.AdmissionDate,
                true);

            _database.StudentEnrollments.Add(enrollment);

            await _database.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new StudentListItemResult(
                student.Id,
                student.AdmissionNumber,
                student.FirstName,
                student.MiddleName,
                student.LastName,
                student.DateOfBirth,
                student.Gender,
                student.AdmissionDate,
                student.Email,
                student.Phone,
                student.Status,
                student.IsActive,
                new StudentEnrollmentResult(
                    enrollment.Id,
                    session.Id,
                    session.Name,
                    level.Id,
                    level.Name,
                    classGroup.Id,
                    classGroup.Name,
                    enrollment.EnrollmentDate,
                    enrollment.IsCurrent));
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<StudentDetailResult> UpdateStudentAsync(
        Guid studentId,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        ValidateStudentDetails(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender);

        var allowedStatuses = new[]
        {
            "Active",
            "Inactive",
            "Graduated",
            "Withdrawn",
            "Suspended"
        };

        var status = request.Status.Trim();

        if (!allowedStatuses.Contains(
                status,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Student status is invalid.");
        }

        var student = await _database.Students
            .SingleOrDefaultAsync(
                x =>
                    x.Id == studentId &&
                    x.TenantId == tenantId,
                cancellationToken);

        if (student is null)
        {
            throw new InvalidOperationException(
                "Student was not found.");
        }

        student.UpdateProfile(
            request.FirstName,
            request.MiddleName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.Email,
            request.Phone);

        student.SetStatus(status);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredStudentAsync(
            studentId,
            cancellationToken);
    }

    public async Task<StudentDetailResult> UpdateCurrentPlacementAsync(
        Guid studentId,
        UpdateStudentPlacementRequest request,
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

        var currentSession = await _database.AcademicSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent &&
                    x.IsActive,
                cancellationToken);

        if (currentSession is null)
        {
            throw new InvalidOperationException(
                "There is no current academic session.");
        }

        ValidateEnrollmentDate(
            request.EnrollmentDate,
            currentSession.StartDate,
            currentSession.EndDate);

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

        var classGroup = await _database.ClassGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.Id == request.ClassGroupId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (classGroup is null)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        if (classGroup.AcademicLevelId != level.Id)
        {
            throw new InvalidOperationException(
                $"Class '{classGroup.Name}' does not belong to academic level '{level.Name}'.");
        }

        var enrollment = await _database.StudentEnrollments
            .SingleOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.StudentId == studentId &&
                    x.AcademicSessionId == currentSession.Id,
                cancellationToken);

        if (enrollment is null)
        {
            throw new InvalidOperationException(
                "The student does not have an enrolment for the current academic session.");
        }

        enrollment.UpdatePlacement(
            level.Id,
            classGroup.Id,
            request.EnrollmentDate);

        enrollment.MakeCurrent();

        var otherCurrentEnrollments =
            await _database.StudentEnrollments
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.StudentId == studentId &&
                    x.Id != enrollment.Id &&
                    x.IsCurrent)
                .ToListAsync(cancellationToken);

        foreach (var otherEnrollment in otherCurrentEnrollments)
        {
            otherEnrollment.RemoveCurrentStatus();
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredStudentAsync(
            studentId,
            cancellationToken);
    }

    private async Task<StudentDetailResult> GetRequiredStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var result = await GetStudentAsync(
            studentId,
            cancellationToken);

        return result
            ?? throw new InvalidOperationException(
                "Student was not found.");
    }

    private static void ValidateStudentDetails(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string gender)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new InvalidOperationException(
                "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new InvalidOperationException(
                "Last name is required.");
        }

        if (dateOfBirth >=
            DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                "Date of birth must be in the past.");
        }

        if (string.IsNullOrWhiteSpace(gender))
        {
            throw new InvalidOperationException(
                "Gender is required.");
        }
    }

    private static void ValidateEnrollmentDate(
        DateOnly enrollmentDate,
        DateOnly sessionStartDate,
        DateOnly sessionEndDate)
    {
        if (enrollmentDate < sessionStartDate ||
            enrollmentDate > sessionEndDate)
        {
            throw new InvalidOperationException(
                $"Enrolment date must fall between {sessionStartDate:yyyy-MM-dd} and {sessionEndDate:yyyy-MM-dd}.");
        }
    }
}
