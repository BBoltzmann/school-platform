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

            // Resolve explicit membership from the class's real parallel timetable groups.
            // A mark is evidence to REVIEW, not permission to auto-enrol a child.
            var memberRows = await database.ParallelSubjectGroupMembers.AsNoTracking()
                .Where(m => m.TenantId == tenantId && m.ParallelSubjectGroup.TenantId == tenantId &&
                    m.ParallelSubjectGroup.IsActive &&
                    m.ParallelSubjectGroup.AcademicSessionId == enrollment.AcademicSessionId &&
                    m.ParallelSubjectGroup.ClassGroupId == enrollment.ClassGroupId &&
                    m.ClassSubject.IsActive && m.ClassSubject.ClassGroupId == enrollment.ClassGroupId)
                .Select(m => new { GroupId = m.ParallelSubjectGroupId,
                    GroupName = m.ParallelSubjectGroup.DisplayName,
                    m.ClassSubject.SubjectId, SubjectName = m.ClassSubject.Subject.Name })
                .ToListAsync(cancellationToken);
            var groupChoices = await database.StudentParallelSubjectChoices.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.StudentId == studentId &&
                    x.AcademicSessionId == enrollment.AcademicSessionId &&
                    x.ClassGroupId == enrollment.ClassGroupId)
                .Select(x => new { x.ParallelSubjectGroupId, x.SubjectId })
                .ToListAsync(cancellationToken);
            var parallelGroups = memberRows.GroupBy(x => x.GroupId).Select(g => new
            {
                groupId = g.Key,
                groupName = g.First().GroupName ?? "Parallel subject choice",
                selectedSubjectId = groupChoices.Where(c => c.ParallelSubjectGroupId == g.Key &&
                    g.Any(m => m.SubjectId == c.SubjectId)).Select(c => (Guid?)c.SubjectId).FirstOrDefault(),
                options = g.GroupBy(m => m.SubjectId).Select(m => new
                    { subjectId = m.Key, subjectName = m.First().SubjectName }).ToArray()
            }).ToArray();
            var offeredBeforeChoice = offeredSubjects.Select(x => x.Id).ToHashSet();
            var hasUnresolvedElectives = parallelGroups.Any(g =>
                !g.selectedSubjectId.HasValue || !offeredBeforeChoice.Contains(g.selectedSubjectId.Value));
            var parallelIds = memberRows.Select(x => x.SubjectId).ToHashSet();
            var selectedIds = parallelGroups.Where(g => g.selectedSubjectId.HasValue)
                .Select(g => g.selectedSubjectId!.Value).ToHashSet();
            offeredSubjects = offeredSubjects.Where(x =>
                !parallelIds.Contains(x.Id) || selectedIds.Contains(x.Id)).ToList();

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
                    assessmentIds.Contains(x.AcademicAssessmentId) && !x.IsVoided)
                .Select(x => new { x.AcademicAssessmentId, x.RawScore })
                .ToListAsync(cancellationToken);
            var scores = scoreRows.ToDictionary(x => x.AcademicAssessmentId, x => x.RawScore);
            var voidedMarks = await database.AcademicAssessmentScores.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.StudentId == studentId &&
                    assessmentIds.Contains(x.AcademicAssessmentId) && x.IsVoided)
                .Select(x => new { x.AcademicAssessmentId, x.RawScore, x.VoidReason, x.VoidedAtUtc })
                .ToArrayAsync(cancellationToken);

            // Only current, active classmates taking the SAME subject and with a
            // complete 20%-weighted checkpoint contribute to its class average.
            var classStudentIds = await database.StudentEnrollments.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.ClassGroupId == enrollment.ClassGroupId &&
                    x.AcademicSessionId == enrollment.AcademicSessionId &&
                    x.IsCurrent && x.IsActive && x.Student.IsActive)
                .Select(x => x.StudentId).Distinct().ToListAsync(cancellationToken);
            var classSelections = await database.StudentParallelSubjectChoices.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.ClassGroupId == enrollment.ClassGroupId &&
                    x.AcademicSessionId == enrollment.AcademicSessionId &&
                    classStudentIds.Contains(x.StudentId))
                .Select(x => new { x.StudentId, x.SubjectId, x.ParallelSubjectGroupId })
                .ToListAsync(cancellationToken);
            var classScores = await database.AcademicAssessmentScores.AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsVoided &&
                    assessmentIds.Contains(x.AcademicAssessmentId) &&
                    classStudentIds.Contains(x.StudentId))
                .Select(x => new { x.AcademicAssessmentId, x.StudentId, x.RawScore })
                .ToListAsync(cancellationToken);
            var classScoresLookup = classScores.ToDictionary(x => (x.StudentId, x.AcademicAssessmentId), x => x.RawScore);
            const decimal checkpointWeight = 20m;

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
                    var weight = items.Sum(x => x.WeightPercentage);
                    var complete = items.Length > 0 && missing == 0;
                    var projected = complete && weight == checkpointWeight
                        ? Math.Round(items.Sum(x => x.WeightedContribution ?? 0m) * 100m / checkpointWeight,
                            2, MidpointRounding.AwayFromZero) : (decimal?)null;
                    var isParallel = parallelIds.Contains(subject.Id);
                    var peers = classStudentIds.Where(id => !isParallel ||
                        classSelections.Any(c => c.StudentId == id && c.SubjectId == subject.Id)).ToArray();
                    var comparable = new List<decimal>();
                    if (weight == checkpointWeight && items.Length > 0)
                    {
                        foreach (var peer in peers)
                        {
                            if (!items.All(a => classScoresLookup.ContainsKey((peer, a.AssessmentId)))) continue;
                            decimal total = 0m;
                            foreach (var assessment in items)
                                total += classScoresLookup[(peer, assessment.AssessmentId)] /
                                    assessment.MaximumScore * assessment.WeightPercentage;
                            comparable.Add(Math.Round(total * 100m / checkpointWeight,
                                2, MidpointRounding.AwayFromZero));
                        }
                    }
                    decimal? classAverage = comparable.Count == 0 ? null :
                        Math.Round(comparable.Average(), 2, MidpointRounding.AwayFromZero);
                    var grade = projected.HasValue ? Grade(projected.Value) : null;
                    var advice = grade switch
                    {
                        "D" => "Parent/guardian: please contact the subject teacher to discuss an improvement plan.",
                        "F" => "Parent/guardian: please arrange an urgent discussion with the subject teacher and an improvement plan.",
                        "C" => "Further practice is recommended to improve this subject.",
                        "A" or "B" => "Good progress at this provisional checkpoint.",
                        _ => "Awaiting complete first-half assessment data."
                    };
                    return new ReportSubjectResult(
                        subject.Id, subject.Name, recorded, missing, weight,
                        items.Sum(x => x.WeightedContribution ?? 0m),
                        complete, items, projected, grade, classAverage, comparable.Count,
                        classAverage.HasValue && projected.HasValue
                            ? Math.Round(projected.Value - classAverage.Value, 2) : null, advice);
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

            // Overall projection is shown only when every confirmed subject is complete.
            // No score or unresolved subject is ever treated as zero.
            decimal? overallProjection = subjects.Length > 0 &&
                subjects.All(x => x.ProjectedPercentage.HasValue) && !hasUnresolvedElectives &&
                unmatchedRecordedSubjects.Length == 0
                    ? Math.Round(subjects.Average(x => x.ProjectedPercentage!.Value), 2,
                        MidpointRounding.AwayFromZero) : null;
            var fullyComparablePeers = new List<decimal>();
            if (!hasUnresolvedElectives && subjects.Length > 0 &&
                subjects.All(x => x.ConfiguredWeight == firstHalfWeight && x.Assessments.Count > 0))
            {
                foreach (var peer in classStudentIds)
                {
                    // Compare overall outcomes only within the same selected
                    // parallel-subject track; do not mix Chemistry and Accounting.
                    if (!parallelGroups.All(g => g.selectedSubjectId.HasValue &&
                        classSelections.Any(c => c.StudentId == peer &&
                            c.ParallelSubjectGroupId == g.groupId && c.SubjectId == g.selectedSubjectId.Value)))
                        continue;
                    var peerSubjects = new List<decimal>();
                    foreach (var subject in subjects)
                    {
                        if (!subject.Assessments.All(a =>
                            classScoresLookup.ContainsKey((peer, a.AssessmentId)))) break;
                        decimal obtained = 0m;
                        foreach (var a in subject.Assessments)
                            obtained += classScoresLookup[(peer, a.AssessmentId)] /
                                a.MaximumScore * a.WeightPercentage;
                        peerSubjects.Add(obtained * 100m / firstHalfWeight);
                    }
                    if (peerSubjects.Count == subjects.Length)
                        fullyComparablePeers.Add(peerSubjects.Average());
                }
            }
            decimal? classOverallAverage = fullyComparablePeers.Count > 0
                ? Math.Round(fullyComparablePeers.Average(), 2, MidpointRounding.AwayFromZero) : null;

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
                academicSessionId = enrollment.AcademicSessionId,
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
                parallelGroups,
                canManageSubjectChoices = currentUser.HasPermission("students.update") ||
                    currentUser.HasPermission("academics.configure"),
                canCorrectScores = currentUser.HasPermission("students.update") ||
                    currentUser.HasPermission("teacher.subjects.read"),
                voidedMarks = voidedMarks.Select(x => new {
                    x.AcademicAssessmentId, x.RawScore, x.VoidReason, x.VoidedAtUtc,
                    assessmentTitle = assessmentRows.First(a => a.Id == x.AcademicAssessmentId).Title,
                    subjectName = assessmentRows.First(a => a.Id == x.AcademicAssessmentId).SubjectName
                }).ToArray(),
                overallProjection,
                overallProjectedGrade = overallProjection.HasValue ? Grade(overallProjection.Value) : null,
                classOverallAverage,
                classOverallCount = fullyComparablePeers.Count,
                overallDifferenceFromClass = overallProjection.HasValue && classOverallAverage.HasValue
                    ? Math.Round(overallProjection.Value - classOverallAverage.Value, 2) : (decimal?)null,
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

    private static string Grade(decimal percent) => percent >= 70m ? "A" :
        percent >= 60m ? "B" : percent >= 50m ? "C" : percent >= 40m ? "D" : "F";

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
        IReadOnlyCollection<ReportAssessmentMark> Assessments,
        decimal? ProjectedPercentage, string? ProjectedGrade,
        decimal? ClassAverage, int ClassAverageCount, decimal? DifferenceFromClassAverage,
        string Advice);
}
