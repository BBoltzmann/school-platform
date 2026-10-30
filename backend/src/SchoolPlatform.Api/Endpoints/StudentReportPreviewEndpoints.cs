using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Api.Endpoints;

public static class StudentReportPreviewEndpoints
{
    public static void MapStudentReportPreviewEndpoints(WebApplication app)
    {
        app.MapGet("/api/students/{studentId:guid}/report-preview", async (
            Guid studentId,
            Guid? academicTermId,
            SchoolPlatformDbContext database,
            ITenantContext tenantContext,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            // Admins can review student reports. Teachers can review only pupils
            // in a class they currently teach (verified below, on the server).
            // Guardians have neither permission; no public/report-generation route.
            var adminAccess = currentUser.HasPermission("students.read");
            var teacherAccess = currentUser.HasPermission("teacher.students.read");
            if (!adminAccess && !teacherAccess)
                return Results.Forbid();

            var tenantId = tenantContext.TenantId;
            var student = await database.Students.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == studentId)
                .Select(x => new
                {
                    x.Id, x.AdmissionNumber, x.FirstName, x.MiddleName,
                    x.LastName, x.Status
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (student is null) return Results.NotFound();

            // Inactive / withdrawn students may still have historical reports.
            var enrollment = await database.StudentEnrollments.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.StudentId == studentId)
                .OrderByDescending(x => x.IsCurrent)
                .ThenByDescending(x => x.EnrollmentDate)
                .Select(x => new
                {
                    x.AcademicSessionId, SessionName = x.AcademicSession.Name,
                    x.ClassGroupId, ClassName = x.ClassGroup.Name,
                    x.IsCurrent, x.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (enrollment is null)
                return Results.Conflict(new { error = "Student has no enrollment to report against." });

            if (!adminAccess)
            {
                // A teaching assignment is the only existing class-scoped authority.
                // Do not grant access through a student's ID alone, and do not expose
                // past students or classes a teacher no longer teaches.
                if (!enrollment.IsCurrent || !enrollment.IsActive ||
                    !await database.Students.AsNoTracking().AnyAsync(x =>
                        x.TenantId == tenantId && x.Id == studentId && x.IsActive,
                        cancellationToken))
                    return Results.Forbid();

                var staffId = await database.StaffMembers.AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.UserId == currentUser.UserId &&
                        x.IsActive && x.IsTeachingStaff)
                    .Select(x => (Guid?)x.Id)
                    .SingleOrDefaultAsync(cancellationToken);
                if (!staffId.HasValue ||
                    !await database.TeachingAssignments.AsNoTracking().AnyAsync(x =>
                        x.TenantId == tenantId && x.StaffMemberId == staffId.Value &&
                        x.AcademicSessionId == enrollment.AcademicSessionId &&
                        x.ClassGroupId == enrollment.ClassGroupId && x.IsActive,
                        cancellationToken))
                    return Results.Forbid();
            }

            var terms = await database.AcademicTerms.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                    x.AcademicSessionId == enrollment.AcademicSessionId && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .Select(x => new { x.Id, x.Name, x.SortOrder })
                .ToListAsync(cancellationToken);

            var chosen = academicTermId.HasValue
                ? terms.FirstOrDefault(x => x.Id == academicTermId.Value)
                : terms.FirstOrDefault();

            if (chosen is null)
                return Results.BadRequest(new { error = "Selected term is not in this student's enrolled session." });

            var classInfo = await database.ClassGroups.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == enrollment.ClassGroupId)
                .Select(x => new { x.UsesCustomSubjectOffering })
                .SingleAsync(cancellationToken);

            var offeredSubjects = classInfo.UsesCustomSubjectOffering
                ? await database.ClassSubjects.AsNoTracking()
                    .Where(x => x.TenantId == tenantId &&
                        x.ClassGroupId == enrollment.ClassGroupId && x.IsActive && x.Subject.IsActive)
                    .Select(x => new ReportSubject(x.SubjectId, x.Subject.Name))
                    .ToListAsync(cancellationToken)
                : await database.TeachingAssignments.AsNoTracking()
                    .Where(x => x.TenantId == tenantId &&
                        x.AcademicSessionId == enrollment.AcademicSessionId &&
                        x.ClassGroupId == enrollment.ClassGroupId && x.IsActive && x.Subject.IsActive)
                    .Select(x => new ReportSubject(x.SubjectId, x.Subject.Name))
                    .Distinct()
                    .ToListAsync(cancellationToken);

            // Parallel groups do not currently store each pupil's chosen subject.
            // Show available marks, but do not assert that every offered elective is taken.
            var hasUnresolvedElectives = await database.ParallelSubjectGroups.AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId &&
                    x.AcademicSessionId == enrollment.AcademicSessionId &&
                    x.ClassGroupId == enrollment.ClassGroupId && x.IsActive,
                    cancellationToken);

            var assessmentRows = await database.AcademicAssessments.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive &&
                    x.AcademicTermId == chosen.Id && x.ClassGroupId == enrollment.ClassGroupId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ReportAssessment(
                    x.Id, x.SubjectId, x.Subject.Name, x.Title, x.Type,
                    x.MaximumScore, x.WeightPercentage))
                .ToListAsync(cancellationToken);

            var assessmentIds = assessmentRows.Select(x => x.Id).ToArray();
            var scoreRows = await database.AcademicAssessmentScores.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.StudentId == studentId &&
                    assessmentIds.Contains(x.AcademicAssessmentId))
                .Select(x => new { x.AcademicAssessmentId, x.RawScore })
                .ToListAsync(cancellationToken);
            var scores = scoreRows.ToDictionary(x => x.AcademicAssessmentId, x => x.RawScore);

            var subjects = offeredSubjects
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .OrderBy(x => x.Name)
                .Select(subject =>
                {
                    var items = assessmentRows.Where(a => a.SubjectId == subject.Id)
                        .Select(a =>
                        {
                            decimal? raw = scores.TryGetValue(a.Id, out var value) ? value : null;
                            decimal? weighted = raw.HasValue && a.MaximumScore > 0
                                ? Math.Round(raw.Value / a.MaximumScore * a.WeightPercentage, 2,
                                    MidpointRounding.AwayFromZero) : null;
                            return new ReportAssessmentMark(
                                a.Id, a.Title, a.Type, a.MaximumScore, a.WeightPercentage,
                                raw, weighted);
                        }).ToArray();
                    var recorded = items.Count(x => x.RawScore.HasValue);
                    var missing = items.Length - recorded;
                    return new ReportSubjectResult(
                        subject.Id, subject.Name, recorded, missing,
                        items.Sum(x => x.WeightPercentage),
                        items.Sum(x => x.WeightedContribution ?? 0m),
                        items.Length > 0 && missing == 0,
                        items);
                }).ToArray();

            // A saved score outside today's class-offering list is genuine history.
            // Do not invent an enrollment or silently remove that score from review.
            // Assessments with NO score for this student are not a mismatch.
            var offeredIds = subjects.Select(x => x.SubjectId).ToHashSet();
            var unmatchedRecordedSubjects = assessmentRows
                .Where(a => !offeredIds.Contains(a.SubjectId) && scores.ContainsKey(a.Id))
                .GroupBy(a => new { a.SubjectId, a.SubjectName })
                .OrderBy(g => g.Key.SubjectName)
                .Select(g => new
                {
                    subjectId = g.Key.SubjectId,
                    subjectName = g.Key.SubjectName,
                    marks = g.Select(a => new ReportAssessmentMark(
                        a.Id, a.Title, a.Type, a.MaximumScore,
                        a.WeightPercentage, scores[a.Id],
                        a.MaximumScore > 0m
                            ? Math.Round(scores[a.Id] / a.MaximumScore * a.WeightPercentage,
                                2, MidpointRounding.AwayFromZero)
                            : null))
                        .ToArray()
                }).ToArray();

            // School default: first-half CA 20%, second-half CA 20%, exam 60%.
            // The raw final exam can be /100; the stored assessment WeightPercentage
            // determines its /60 term contribution. Nothing here alters scores.
            // This existing schema has no Midterm/Final identifier, so 20% can
            // only establish a PROVISIONAL draft checkpoint when each subject has
            // exactly 20% of assessments configured and every mark is recorded.
            const decimal firstHalfWeight = 20m;
            const decimal secondHalfWeight = 20m;
            const decimal finalExamWeight = 60m;
            var subjectsAtFirstHalfWeight = subjects.Count(x =>
                x.Assessments.Count > 0 && x.ConfiguredWeight == firstHalfWeight);
            var provisionalMidtermReady = subjects.Length > 0 &&
                subjectsAtFirstHalfWeight == subjects.Length &&
                subjects.All(x => x.MarksComplete) &&
                !hasUnresolvedElectives && unmatchedRecordedSubjects.Length == 0;

            var problems = new List<string>();
            if (offeredSubjects.Count == 0)
                problems.Add("No class subjects are configured for this student’s enrollment.");
            if (hasUnresolvedElectives)
                problems.Add("This class has parallel/elective subjects without a confirmed per-student selection. Subject membership requires review.");
            if (subjects.Any(x => x.Assessments.Count == 0))
                problems.Add("One or more offered subjects have no assessments configured for the selected term.");
            if (subjects.Any(x => x.MissingScores > 0))
                problems.Add("One or more configured assessments are missing this student's score.");
            if (unmatchedRecordedSubjects.Length > 0)
                problems.Add("This student has saved marks for subjects outside the current class offering: " +
                    string.Join(", ", unmatchedRecordedSubjects.Select(x => x.subjectName)) +
                    ". Review the subject offering; those marks are preserved.");
            if (subjects.Any(x => x.ConfiguredWeight > firstHalfWeight))
                problems.Add("One or more subjects already have over 20% of term assessments configured. " +
                    "The first-half marks cannot be separated safely without reporting-period labels.");
            if (subjects.Any(x => x.ConfiguredWeight < firstHalfWeight))
                problems.Add("First-half assessment weight is below the default 20% in: " +
                    string.Join(", ", subjects.Where(x => x.ConfiguredWeight < firstHalfWeight)
                        .Select(x => $"{x.SubjectName} ({x.ConfiguredWeight:0.##}%)")) + ".");

            var studentName = string.Join(" ", new[] {
                student.FirstName, student.MiddleName, student.LastName
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            return Results.Ok(new
            {
                studentId = student.Id,
                studentName,
                student.AdmissionNumber,
                student.Status,
                enrollment.ClassGroupId,
                enrollment.ClassName,
                enrollment.SessionName,
                enrollment.IsCurrent,
                enrollment.IsActive,
                academicTermId = chosen.Id,
                academicTermName = chosen.Name,
                terms,
                subjects,
                totalSubjects = subjects.Length,
                subjectsWithMarksComplete = subjects.Count(x => x.MarksComplete),
                totalRecordedMarks = subjects.Sum(x => x.RecordedScores),
                totalMissingMarks = subjects.Sum(x => x.MissingScores),
                hasUnresolvedElectives,
                unmatchedRecordedSubjects,
                firstHalfWeight,
                secondHalfWeight,
                finalExamWeight,
                subjectsAtFirstHalfWeight,
                provisionalMidtermReady,
                problems,
                // Draft readiness is NOT approval: no official or guardian publishing.
                canGenerateOfficial = false,
                reportKind = provisionalMidtermReady ? "ProvisionalMidtermDraft" : "TermToDateDraft"
            });
        }).RequireAuthorization();
    }

    private sealed record ReportSubject(Guid Id, string Name);
    private sealed record ReportAssessment(
        Guid Id, Guid SubjectId, string SubjectName, string Title, string Type,
        decimal MaximumScore, decimal WeightPercentage);
    private sealed record ReportAssessmentMark(
        Guid AssessmentId, string Title, string Type, decimal MaximumScore,
        decimal WeightPercentage, decimal? RawScore, decimal? WeightedContribution);
    private sealed record ReportSubjectResult(
        Guid SubjectId, string SubjectName, int RecordedScores, int MissingScores,
        decimal ConfiguredWeight, decimal TotalWeightedScore, bool MarksComplete,
        IReadOnlyCollection<ReportAssessmentMark> Assessments);
}
