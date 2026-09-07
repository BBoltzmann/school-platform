namespace SchoolPlatform.Application.Staff;

public sealed record CreateTeachingAssignmentRequest(
    Guid StaffMemberId,
    Guid AcademicSessionId,
    Guid ClassGroupId,
    Guid SubjectId);
