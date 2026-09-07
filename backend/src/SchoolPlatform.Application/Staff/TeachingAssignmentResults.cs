namespace SchoolPlatform.Application.Staff;

public sealed record TeachingAssignmentResult(
    Guid Id,
    Guid StaffMemberId,
    string StaffNumber,
    string StaffName,
    Guid AcademicSessionId,
    string AcademicSessionName,
    Guid ClassGroupId,
    string ClassGroupName,
    Guid AcademicLevelId,
    string AcademicLevelName,
    Guid SubjectId,
    string SubjectName,
    bool IsActive);

public sealed record TeachingAssignmentSetupResult(
    TeachingAssignmentSessionOption? CurrentSession,
    IReadOnlyCollection<TeachingAssignmentStaffOption> TeachingStaff,
    IReadOnlyCollection<TeachingAssignmentClassOption> Classes,
    IReadOnlyCollection<TeachingAssignmentSubjectOption> Subjects);

public sealed record TeachingAssignmentSessionOption(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record TeachingAssignmentStaffOption(
    Guid Id,
    string StaffNumber,
    string Name,
    string JobTitle,
    string? Department,
    string EmploymentType);

public sealed record TeachingAssignmentClassOption(
    Guid Id,
    string Name,
    Guid AcademicLevelId,
    string AcademicLevelName);

public sealed record TeachingAssignmentSubjectOption(
    Guid Id,
    string Name);
