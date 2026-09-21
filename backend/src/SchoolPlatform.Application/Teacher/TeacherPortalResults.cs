namespace SchoolPlatform.Application.Teacher;

public sealed record TeacherPortalResult(
    TeacherProfileResult Profile,
    TeacherAcademicContext Academic,
    IReadOnlyCollection<TeacherClassResult> Classes,
    IReadOnlyCollection<TeacherSubjectResult> Subjects,
    IReadOnlyCollection<TeacherStudentResult> Students,
    IReadOnlyCollection<TeacherTimetableEntryResult> Timetable);

public sealed record TeacherProfileResult(Guid Id, string StaffNumber, string Name, string? Email, string Phone, string JobTitle, string? Department, DateOnly EmploymentDate);
public sealed record TeacherAcademicContext(string? SessionName, string? TermName, DateOnly? SessionStartDate, DateOnly? SessionEndDate);
public sealed record TeacherClassResult(Guid Id, string Name, string LevelName, int StudentCount, IReadOnlyCollection<string> Subjects);
public sealed record TeacherSubjectResult(Guid SubjectId, string SubjectName, string SubjectCode, Guid ClassGroupId, string ClassName, string LevelName, int StudentCount);
public sealed record TeacherStudentResult(Guid Id, string AdmissionNumber, string Name, Guid ClassGroupId, string ClassName);
public sealed record TeacherTimetableEntryResult(Guid ClassGroupId, string ClassName, Guid SubjectId, string SubjectName, DayOfWeek DayOfWeek, int PeriodNumber, TimeOnly StartTime, TimeOnly EndTime, Guid? ParallelOccurrenceId = null);
public sealed record LinkTeacherUserRequest(Guid UserId);

public interface ITeacherPortalService
{
    Task<TeacherPortalResult?> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeacherTimetableEntryResult>> GetClassTimetableAsync(Guid classGroupId, CancellationToken cancellationToken = default);
}
