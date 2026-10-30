using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Audit;
using SchoolPlatform.Infrastructure.Persistence;
using System.Text.Json;

namespace SchoolPlatform.Api.Endpoints;

public static class StudentReportCorrectionsEndpoints
{
    public sealed record ChooseParallelSubjectRequest(Guid AcademicSessionId, Guid ClassGroupId,
        Guid ParallelSubjectGroupId, Guid SubjectId);
    public sealed record CorrectScoreRequest(string Reason);

    public static void MapStudentReportCorrectionsEndpoints(WebApplication app)
    {
        app.MapPut("/api/students/{studentId:guid}/parallel-subject-choice", async (
            Guid studentId, ChooseParallelSubjectRequest request,
            SchoolPlatformDbContext db, ITenantContext tenant, ICurrentUserContext user,
            CancellationToken ct) =>
        {
            if (!user.HasPermission("students.update") && !user.HasPermission("academics.configure"))
                return Results.Forbid();
            var tid = tenant.TenantId;
            var enrolled = await db.StudentEnrollments.AsNoTracking().AnyAsync(x =>
                x.TenantId == tid && x.StudentId == studentId && x.IsCurrent && x.IsActive &&
                x.AcademicSessionId == request.AcademicSessionId && x.ClassGroupId == request.ClassGroupId, ct);
            if (!enrolled) return Results.BadRequest(new { error = "Student must currently belong to this class and session." });
            var allowed = await db.ParallelSubjectGroupMembers.AsNoTracking().AnyAsync(m =>
                m.TenantId == tid && m.ParallelSubjectGroupId == request.ParallelSubjectGroupId &&
                m.ParallelSubjectGroup.TenantId == tid && m.ParallelSubjectGroup.IsActive &&
                m.ParallelSubjectGroup.AcademicSessionId == request.AcademicSessionId &&
                m.ParallelSubjectGroup.ClassGroupId == request.ClassGroupId &&
                m.ClassSubject.IsActive && m.ClassSubject.SubjectId == request.SubjectId &&
                m.ClassSubject.ClassGroupId == request.ClassGroupId, ct);
            if (!allowed) return Results.BadRequest(new { error = "Subject is not an active member of this class's parallel group." });
            var choice = await db.StudentParallelSubjectChoices.SingleOrDefaultAsync(x =>
                x.TenantId == tid && x.StudentId == studentId &&
                x.AcademicSessionId == request.AcademicSessionId && x.ClassGroupId == request.ClassGroupId &&
                x.ParallelSubjectGroupId == request.ParallelSubjectGroupId, ct);
            Guid? previous = choice?.SubjectId;
            if (choice is null) db.StudentParallelSubjectChoices.Add(new StudentParallelSubjectChoice(
                tid, studentId, request.AcademicSessionId, request.ClassGroupId,
                request.ParallelSubjectGroupId, request.SubjectId));
            else choice.Select(request.SubjectId);
            db.AuditLogs.Add(new AuditLog(tid, user.UserId, "StudentParallelSubjectChoiceUpdated",
                "Student", studentId,
                JsonSerializer.Serialize(new { subjectId = previous, request.ParallelSubjectGroupId }),
                JsonSerializer.Serialize(new { request.SubjectId, request.ParallelSubjectGroupId })));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPost("/api/students/{studentId:guid}/report-scores/{assessmentId:guid}/{action}", async (
            Guid studentId, Guid assessmentId, string action, CorrectScoreRequest request,
            SchoolPlatformDbContext db, ITenantContext tenant, ICurrentUserContext user,
            CancellationToken ct) =>
        {
            if (action != "void" && action != "restore") return Results.NotFound();
            if (!user.HasPermission("students.update") && !user.HasPermission("teacher.subjects.read"))
                return Results.Forbid();
            var tid = tenant.TenantId;
            var assessment = await db.AcademicAssessments.AsNoTracking().Where(x =>
                x.Id == assessmentId && x.TenantId == tid && x.IsActive)
                .Select(x => new { x.AcademicSessionId, x.ClassGroupId, x.SubjectId }).SingleOrDefaultAsync(ct);
            if (assessment is null) return Results.NotFound();
            var enrolled = await db.StudentEnrollments.AsNoTracking().AnyAsync(x =>
                x.TenantId == tid && x.StudentId == studentId && x.AcademicSessionId == assessment.AcademicSessionId &&
                x.ClassGroupId == assessment.ClassGroupId, ct);
            if (!enrolled) return Results.NotFound();
            if (!user.HasPermission("students.update"))
            {
                var currentEnrollment = await db.StudentEnrollments.AsNoTracking().AnyAsync(x =>
                    x.TenantId == tid && x.StudentId == studentId && x.IsCurrent && x.IsActive &&
                    x.Student.IsActive && x.AcademicSessionId == assessment.AcademicSessionId &&
                    x.ClassGroupId == assessment.ClassGroupId, ct);
                if (!currentEnrollment) return Results.Forbid();
                var assigned = await db.StaffMembers.AsNoTracking().AnyAsync(s =>
                    s.TenantId == tid && s.UserId == user.UserId && s.IsActive && s.IsTeachingStaff &&
                    db.TeachingAssignments.Any(a => a.TenantId == tid && a.StaffMemberId == s.Id &&
                        a.AcademicSessionId == assessment.AcademicSessionId &&
                        a.ClassGroupId == assessment.ClassGroupId && a.SubjectId == assessment.SubjectId && a.IsActive), ct);
                if (!assigned) return Results.Forbid();
            }
            var score = await db.AcademicAssessmentScores.SingleOrDefaultAsync(x =>
                x.TenantId == tid && x.AcademicAssessmentId == assessmentId && x.StudentId == studentId, ct);
            if (score is null) return Results.NotFound();
            if (action == "void")
            {
                if (score.IsVoided) return Results.BadRequest(new { error = "Mark is already voided." });
                try { score.Void(user.UserId, request.Reason); }
                catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            }
            else
            {
                if (!score.IsVoided) return Results.BadRequest(new { error = "Mark is not voided." });
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
                    return Results.BadRequest(new { error = "A restoration reason is required." });
                score.Restore();
            }
            db.AuditLogs.Add(new AuditLog(tid, user.UserId,
                action == "void" ? "AssessmentScoreVoided" : "AssessmentScoreRestored",
                "AcademicAssessmentScore", score.Id,
                JsonSerializer.Serialize(new { score.RawScore, score.StudentId, assessmentId }),
                JsonSerializer.Serialize(new { action, request.Reason })));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
