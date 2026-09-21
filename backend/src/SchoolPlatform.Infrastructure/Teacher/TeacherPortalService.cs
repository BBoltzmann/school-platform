using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Teacher;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Teacher;

public sealed class TeacherPortalService(SchoolPlatformDbContext database, ITenantContext tenantContext, ICurrentUserContext currentUser) : ITeacherPortalService
{
    public async Task<TeacherPortalResult?> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var userId = currentUser.UserId;
        var staff = await database.StaffMembers.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive && x.IsTeachingStaff, cancellationToken);
        if (staff is null) return null;
        var assignments = await database.TeachingAssignments.AsNoTracking().Where(x => x.TenantId == tenantId && x.StaffMemberId == staff.Id && x.IsActive && x.ClassGroup.IsActive && x.Subject.IsActive).Select(x => new { x.ClassGroupId, ClassName = x.ClassGroup.Name, LevelName = x.ClassGroup.AcademicLevel.Name, x.SubjectId, SubjectName = x.Subject.Name, SubjectCode = x.Subject.Code }).ToListAsync(cancellationToken);
        var classIds = assignments.Select(x => x.ClassGroupId).Distinct().ToArray();
        var studentRows = await database.StudentEnrollments.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive && x.IsCurrent && classIds.Contains(x.ClassGroupId) && x.Student.IsActive).Select(x => new { x.StudentId, x.ClassGroupId, AdmissionNumber = x.Student.AdmissionNumber, Name = x.Student.MiddleName == null ? x.Student.FirstName + " " + x.Student.LastName : x.Student.FirstName + " " + x.Student.MiddleName + " " + x.Student.LastName, ClassName = x.ClassGroup.Name }).ToListAsync(cancellationToken);
        var session = await database.AcademicSessions.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive && x.IsCurrent).OrderByDescending(x => x.StartDate).Select(x => new { x.Id, x.Name, x.StartDate, x.EndDate }).FirstOrDefaultAsync(cancellationToken);
        var term = session is null ? null : await database.AcademicTerms.AsNoTracking().Where(x => x.TenantId == tenantId && x.AcademicSessionId == session.Id && x.IsActive).Where(x => DateOnly.FromDateTime(DateTime.UtcNow) >= x.StartDate && DateOnly.FromDateTime(DateTime.UtcNow) <= x.EndDate).SingleOrDefaultAsync(cancellationToken);
        var timetable = session is null || term is null ? new List<TeacherTimetableEntryResult>() : await GetTimetableAsync(tenantId, staff.Id, null, session.Id, term.Id, cancellationToken);
        var classes = assignments.GroupBy(x => new { x.ClassGroupId, x.ClassName, x.LevelName }).Select(g => new TeacherClassResult(g.Key.ClassGroupId, g.Key.ClassName, g.Key.LevelName, studentRows.Count(s => s.ClassGroupId == g.Key.ClassGroupId), g.Select(x => x.SubjectName).Distinct().OrderBy(x => x).ToList())).OrderBy(x => x.Name).ToList();
        var subjects = assignments.Select(x => new TeacherSubjectResult(x.SubjectId, x.SubjectName, x.SubjectCode, x.ClassGroupId, x.ClassName, x.LevelName, studentRows.Count(s => s.ClassGroupId == x.ClassGroupId))).DistinctBy(x => new { x.SubjectId, x.ClassGroupId }).OrderBy(x => x.SubjectName).ThenBy(x => x.ClassName).ToList();
        var students = studentRows.Select(x => new TeacherStudentResult(x.StudentId, x.AdmissionNumber, x.Name, x.ClassGroupId, x.ClassName)).DistinctBy(x => x.Id).OrderBy(x => x.Name).ToList();
        return new(new(staff.Id, staff.StaffNumber, staff.MiddleName == null ? staff.FirstName + " " + staff.LastName : staff.FirstName + " " + staff.MiddleName + " " + staff.LastName, staff.Email, staff.Phone, staff.JobTitle, staff.Department, staff.EmploymentDate), new(session?.Name, term?.Name, session?.StartDate, session?.EndDate), classes, subjects, students, timetable);
    }

    public async Task<IReadOnlyCollection<TeacherTimetableEntryResult>> GetClassTimetableAsync(Guid classGroupId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var userId = currentUser.UserId;
        var staffId = await database.StaffMembers.Where(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive && x.IsTeachingStaff).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Teacher profile was not found.");
        if (!await database.TeachingAssignments.AnyAsync(x => x.TenantId == tenantId && x.StaffMemberId == staffId && x.ClassGroupId == classGroupId && x.IsActive, cancellationToken)) throw new UnauthorizedAccessException("You do not teach this class.");
        var session = await database.AcademicSessions.Where(x => x.TenantId == tenantId && x.IsCurrent && x.IsActive).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        return await GetTimetableAsync(tenantId, null, classGroupId, session, null, cancellationToken);
    }

    private async Task<List<TeacherTimetableEntryResult>> GetTimetableAsync(Guid tenantId, Guid? staffId, Guid? classGroupId, Guid sessionId, Guid? termId, CancellationToken cancellationToken)
    {
        var query = from entry in database.GeneratedTimetableEntries.AsNoTracking()
                    join timetable in database.GeneratedTimetables.AsNoTracking() on entry.GeneratedTimetableId equals timetable.Id
                    join classGroup in database.ClassGroups.AsNoTracking() on entry.ClassGroupId equals classGroup.Id
                    join subject in database.Subjects.AsNoTracking() on entry.SubjectId equals subject.Id
                    where entry.TenantId == tenantId && timetable.IsActive && timetable.AcademicSessionId == sessionId && (termId == null || timetable.AcademicTermId == termId) && (staffId == null || entry.StaffMemberId == staffId) && (classGroupId == null || entry.ClassGroupId == classGroupId)
                    orderby entry.DayOfWeek, entry.PeriodNumber
                    select new TeacherTimetableEntryResult(entry.ClassGroupId, classGroup.Name, entry.SubjectId, subject.Name, entry.DayOfWeek, entry.PeriodNumber, entry.StartTime, entry.EndTime, entry.ParallelOccurrenceId);
        return await query.ToListAsync(cancellationToken);
    }
}
