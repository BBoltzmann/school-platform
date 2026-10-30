namespace SchoolPlatform.Application.Academics;

public sealed record CreateCampusRequest(string Name);

public sealed record CreateAcademicSessionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent);

public sealed record CreateAcademicTermRequest(
    Guid AcademicSessionId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int SortOrder,
    decimal FirstHalfWeight = 20m,
    decimal SecondHalfWeight = 20m,
    decimal FinalExamWeight = 60m,
    DateOnly? FirstHalfDueDate = null,
    DateOnly? MidtermBreakStartDate = null,
    DateOnly? ResumeDate = null,
    DateOnly? MidtermReportDueDate = null,
    DateOnly? SecondHalfDueDate = null,
    DateOnly? FinalExamDueDate = null,
    DateOnly? FinalReportDueDate = null);

public sealed record UpdateTermAssessmentScheduleRequest(
    decimal FirstHalfWeight,
    decimal SecondHalfWeight,
    decimal FinalExamWeight,
    DateOnly? FirstHalfDueDate,
    DateOnly? MidtermBreakStartDate,
    DateOnly? ResumeDate,
    DateOnly? MidtermReportDueDate,
    DateOnly? SecondHalfDueDate,
    DateOnly? FinalExamDueDate,
    DateOnly? FinalReportDueDate);

public sealed record CreateAcademicLevelRequest(
    string Name,
    string Category,
    int SortOrder);

public sealed record CreateClassGroupRequest(
    Guid CampusId,
    Guid AcademicLevelId,
    string Name);

public sealed record CreateSubjectRequest(
    string Name,
    string Code,
    string Category);

public sealed record UpdateAcademicLevelRequest(
    string Name,
    string Category,
    int SortOrder);

public sealed record UpdateClassGroupRequest(
    Guid CampusId,
    Guid AcademicLevelId,
    string Name);

public sealed record UpdateSubjectRequest(
    string Name,
    string Code,
    string Category);

public sealed record SetAcademicItemStatusRequest(
    bool IsActive);
