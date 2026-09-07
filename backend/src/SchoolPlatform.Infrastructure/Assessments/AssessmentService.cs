using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Assessments;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Domain.Assessments;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Assessments;

public sealed class AssessmentService : IAssessmentService
{
    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Test",
            "Assignment",
            "Homework",
            "Project",
            "Quiz",
            "Exam",
            "Other"
        };

    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public AssessmentService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyCollection<AssessmentResult>>
        GetAssessmentsAsync(
            Guid academicTermId,
            Guid classGroupId,
            Guid subjectId,
            CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await BuildAssessmentQuery(
                tenantId,
                academicTermId,
                classGroupId,
                subjectId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new AssessmentResult(
                x.Id,
                x.AcademicSessionId,
                x.AcademicTermId,
                x.ClassGroupId,
                x.ClassGroupName,
                x.AcademicLevelName,
                x.SubjectId,
                x.SubjectName,
                x.Type,
                x.Title,
                x.MaximumScore,
                x.WeightPercentage,
                x.SortOrder,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<AssessmentResult> CreateAsync(
        CreateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateType(request.Type);

        var tenantId = _tenantContext.TenantId;

        var session = await GetCurrentSessionAsync(
            tenantId,
            cancellationToken);

        await ValidateTermAsync(
            tenantId,
            session.Id,
            request.AcademicTermId,
            cancellationToken);

        await ValidateClassAndSubjectAsync(
            tenantId,
            request.ClassGroupId,
            request.SubjectId,
            cancellationToken);

        var existingWeight =
            await _database.AcademicAssessments
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicTermId == request.AcademicTermId &&
                    x.ClassGroupId == request.ClassGroupId &&
                    x.SubjectId == request.SubjectId &&
                    x.IsActive)
                .SumAsync(
                    x => (decimal?)x.WeightPercentage,
                    cancellationToken)
            ?? 0m;

        ValidateWeightTotal(
            existingWeight +
            request.WeightPercentage);

        var sortOrder =
            await _database.AcademicAssessments
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicTermId == request.AcademicTermId &&
                    x.ClassGroupId == request.ClassGroupId &&
                    x.SubjectId == request.SubjectId &&
                    x.IsActive)
                .MaxAsync(
                    x => (int?)x.SortOrder,
                    cancellationToken)
            ?? 0;

        var assessment =
            new AcademicAssessment(
                tenantId,
                session.Id,
                request.AcademicTermId,
                request.ClassGroupId,
                request.SubjectId,
                NormalizeType(request.Type),
                request.Title,
                request.MaximumScore,
                request.WeightPercentage,
                sortOrder + 1);

        _database.AcademicAssessments.Add(
            assessment);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetAssessmentResultAsync(
            assessment.Id,
            tenantId,
            cancellationToken);
    }

    public async Task<AssessmentResult> UpdateAsync(
        Guid assessmentId,
        UpdateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateType(request.Type);

        var tenantId =
            _tenantContext.TenantId;

        var assessment =
            await _database.AcademicAssessments
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == assessmentId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Assessment was not found.");

        if (request.MaximumScore <= 0)
        {
            throw new InvalidOperationException(
                "Maximum score must be greater than zero.");
        }

        var highestExistingScore =
            await _database.AcademicAssessmentScores
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicAssessmentId == assessment.Id)
                .MaxAsync(
                    x => (decimal?)x.RawScore,
                    cancellationToken);

        if (highestExistingScore.HasValue &&
            highestExistingScore.Value >
                request.MaximumScore)
        {
            throw new InvalidOperationException(
                $"Maximum score cannot be reduced below an existing student score of {highestExistingScore.Value:0.##}.");
        }

        var otherWeight =
            await _database.AcademicAssessments
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicTermId ==
                        assessment.AcademicTermId &&
                    x.ClassGroupId ==
                        assessment.ClassGroupId &&
                    x.SubjectId ==
                        assessment.SubjectId &&
                    x.IsActive &&
                    x.Id != assessment.Id)
                .SumAsync(
                    x => (decimal?)x.WeightPercentage,
                    cancellationToken)
            ?? 0m;

        ValidateWeightTotal(
            otherWeight +
            request.WeightPercentage);

        assessment.Update(
            NormalizeType(request.Type),
            request.Title,
            request.MaximumScore,
            request.WeightPercentage,
            request.SortOrder);

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetAssessmentResultAsync(
            assessment.Id,
            tenantId,
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var assessment =
            await _database.AcademicAssessments
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == assessmentId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Assessment was not found.");

        assessment.Deactivate();

        await _database.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<AssessmentScoreSheetResult>
        GetScoreSheetAsync(
            Guid assessmentId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var assessment =
            await GetAssessmentResultAsync(
                assessmentId,
                tenantId,
                cancellationToken);

        var students =
            await GetStudentsForAssessmentAsync(
                assessment,
                tenantId,
                cancellationToken);

        var scores =
            await _database.AcademicAssessmentScores
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicAssessmentId ==
                        assessment.Id)
                .ToDictionaryAsync(
                    x => x.StudentId,
                    x => x.RawScore,
                    cancellationToken);

        var weightTotal =
            await GetConfiguredWeightTotalAsync(
                tenantId,
                assessment.AcademicTermId,
                assessment.ClassGroupId,
                assessment.SubjectId,
                cancellationToken);

        var rows =
            students
                .Select(student =>
                {
                    decimal? rawScore =
                        scores.TryGetValue(
                            student.Id,
                            out var value)
                            ? value
                            : null;

                    return BuildScoreRow(
                        student.Id,
                        student.AdmissionNumber,
                        student.Name,
                        rawScore,
                        assessment.MaximumScore,
                        assessment.WeightPercentage);
                })
                .ToList();

        return new AssessmentScoreSheetResult(
            assessment,
            weightTotal,
            rows);
    }

    public async Task<AssessmentScoreSheetResult>
        SaveScoresAsync(
            Guid assessmentId,
            SaveAssessmentScoresRequest request,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var assessment =
            await GetAssessmentResultAsync(
                assessmentId,
                tenantId,
                cancellationToken);

        var students =
            await GetStudentsForAssessmentAsync(
                assessment,
                tenantId,
                cancellationToken);

        var allowedStudentIds =
            students
                .Select(x => x.Id)
                .ToHashSet();

        var duplicateStudentId =
            request.Scores
                .GroupBy(x => x.StudentId)
                .FirstOrDefault(x =>
                    x.Count() > 1);

        if (duplicateStudentId is not null)
        {
            throw new InvalidOperationException(
                "The same student cannot appear more than once in a score submission.");
        }

        foreach (var item in request.Scores)
        {
            if (!allowedStudentIds.Contains(
                    item.StudentId))
            {
                throw new InvalidOperationException(
                    "One or more students do not belong to this class.");
            }

            if (item.RawScore.HasValue &&
                (item.RawScore.Value < 0 ||
                 item.RawScore.Value >
                    assessment.MaximumScore))
            {
                throw new InvalidOperationException(
                    $"Scores must be between 0 and {assessment.MaximumScore:0.##}.");
            }
        }

        var submittedIds =
            request.Scores
                .Select(x => x.StudentId)
                .ToList();

        var existingScores =
            await _database.AcademicAssessmentScores
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicAssessmentId ==
                        assessment.Id &&
                    submittedIds.Contains(
                        x.StudentId))
                .ToDictionaryAsync(
                    x => x.StudentId,
                    cancellationToken);

        foreach (var item in request.Scores)
        {
            if (!item.RawScore.HasValue)
            {
                if (existingScores.TryGetValue(
                        item.StudentId,
                        out var scoreToRemove))
                {
                    _database.AcademicAssessmentScores.Remove(
                        scoreToRemove);
                }

                continue;
            }

            if (existingScores.TryGetValue(
                    item.StudentId,
                    out var existing))
            {
                existing.UpdateScore(
                    item.RawScore.Value);
            }
            else
            {
                _database.AcademicAssessmentScores.Add(
                    new AcademicAssessmentScore(
                        tenantId,
                        assessment.Id,
                        item.StudentId,
                        item.RawScore.Value));
            }
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetScoreSheetAsync(
            assessment.Id,
            cancellationToken);
    }

    public async Task<AssessmentGradebookResult>
        GetGradebookAsync(
            Guid academicTermId,
            Guid classGroupId,
            Guid subjectId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var assessments =
            await GetAssessmentsAsync(
                academicTermId,
                classGroupId,
                subjectId,
                cancellationToken);

        if (assessments.Count == 0)
        {
            throw new InvalidOperationException(
                "No assessments have been configured.");
        }

        var first =
            assessments.First();

        var students =
            await GetStudentsForAssessmentAsync(
                first,
                tenantId,
                cancellationToken);

        var assessmentIds =
            assessments
                .Select(x => x.Id)
                .ToList();

        var scores =
            await _database.AcademicAssessmentScores
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    assessmentIds.Contains(
                        x.AcademicAssessmentId))
                .ToListAsync(
                    cancellationToken);

        var scoreLookup =
            scores.ToDictionary(
                x => (
                    x.AcademicAssessmentId,
                    x.StudentId),
                x => x.RawScore);

        var rows =
            new List<AssessmentGradebookStudentResult>();

        foreach (var student in students)
        {
            var cells =
                new List<AssessmentGradebookScoreResult>();

            decimal total = 0m;

            foreach (var assessment in assessments)
            {
                decimal? raw =
                    scoreLookup.TryGetValue(
                        (
                            assessment.Id,
                            student.Id
                        ),
                        out var rawValue)
                        ? rawValue
                        : null;

                decimal? percentage = null;
                decimal? contribution = null;

                if (raw.HasValue)
                {
                    percentage =
                        Round(
                            raw.Value /
                            assessment.MaximumScore *
                            100m);

                    contribution =
                        Round(
                            raw.Value /
                            assessment.MaximumScore *
                            assessment.WeightPercentage);

                    total +=
                        contribution.Value;
                }

                cells.Add(
                    new AssessmentGradebookScoreResult(
                        assessment.Id,
                        raw,
                        percentage,
                        contribution));
            }

            rows.Add(
                new AssessmentGradebookStudentResult(
                    student.Id,
                    student.AdmissionNumber,
                    student.Name,
                    Round(total),
                    cells));
        }

        var configuredWeight =
            assessments.Sum(
                x => x.WeightPercentage);

        return new AssessmentGradebookResult(
            academicTermId,
            classGroupId,
            first.ClassGroupName,
            subjectId,
            first.SubjectName,
            configuredWeight,
            configuredWeight == 100m,
            assessments
                .Select(x =>
                    new AssessmentGradebookColumnResult(
                        x.Id,
                        x.Type,
                        x.Title,
                        x.MaximumScore,
                        x.WeightPercentage))
                .ToList(),
            rows);
    }

    private IQueryable<AssessmentProjection>
        BuildAssessmentQuery(
            Guid tenantId,
            Guid academicTermId,
            Guid classGroupId,
            Guid subjectId)
    {
        return _database.AcademicAssessments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AcademicTermId == academicTermId &&
                x.ClassGroupId == classGroupId &&
                x.SubjectId == subjectId &&
                x.IsActive)
            .Select(x => new AssessmentProjection(
                x.Id,
                x.AcademicSessionId,
                x.AcademicTermId,
                x.ClassGroupId,
                x.ClassGroup.Name,
                x.ClassGroup.AcademicLevel.Name,
                x.SubjectId,
                x.Subject.Name,
                x.Type,
                x.Title,
                x.MaximumScore,
                x.WeightPercentage,
                x.SortOrder,
                x.IsActive,
                x.CreatedAtUtc));
    }

    private async Task<AssessmentResult>
        GetAssessmentResultAsync(
            Guid assessmentId,
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        return await _database.AcademicAssessments
            .AsNoTracking()
            .Where(x =>
                x.Id == assessmentId &&
                x.TenantId == tenantId &&
                x.IsActive)
            .Select(x => new AssessmentResult(
                x.Id,
                x.AcademicSessionId,
                x.AcademicTermId,
                x.ClassGroupId,
                x.ClassGroup.Name,
                x.ClassGroup.AcademicLevel.Name,
                x.SubjectId,
                x.Subject.Name,
                x.Type,
                x.Title,
                x.MaximumScore,
                x.WeightPercentage,
                x.SortOrder,
                x.IsActive))
            .SingleOrDefaultAsync(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Assessment was not found.");
    }

    private async Task<IReadOnlyCollection<StudentProjection>>
        GetStudentsForAssessmentAsync(
            AssessmentResult assessment,
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        return await _database.StudentEnrollments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AcademicSessionId ==
                    assessment.AcademicSessionId &&
                x.ClassGroupId ==
                    assessment.ClassGroupId &&
                x.IsCurrent &&
                x.IsActive &&
                x.Student.IsActive)
            .OrderBy(x =>
                x.Student.LastName)
            .ThenBy(x =>
                x.Student.FirstName)
            .Select(x => new StudentProjection(
                x.StudentId,
                x.Student.AdmissionNumber,
                x.Student.MiddleName == null
                    ? x.Student.FirstName +
                      " " +
                      x.Student.LastName
                    : x.Student.FirstName +
                      " " +
                      x.Student.MiddleName +
                      " " +
                      x.Student.LastName))
            .ToListAsync(
                cancellationToken);
    }

    private async Task<decimal>
        GetConfiguredWeightTotalAsync(
            Guid tenantId,
            Guid academicTermId,
            Guid classGroupId,
            Guid subjectId,
            CancellationToken cancellationToken)
    {
        return await _database.AcademicAssessments
            .Where(x =>
                x.TenantId == tenantId &&
                x.AcademicTermId == academicTermId &&
                x.ClassGroupId == classGroupId &&
                x.SubjectId == subjectId &&
                x.IsActive)
            .SumAsync(
                x => (decimal?)x.WeightPercentage,
                cancellationToken)
            ?? 0m;
    }

    private async Task<SessionProjection>
        GetCurrentSessionAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        return await _database.AcademicSessions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsCurrent &&
                x.IsActive)
            .Select(x =>
                new SessionProjection(
                    x.Id,
                    x.Name))
            .SingleOrDefaultAsync(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "A current academic session is required.");
    }

    private async Task ValidateTermAsync(
        Guid tenantId,
        Guid sessionId,
        Guid termId,
        CancellationToken cancellationToken)
    {
        var valid =
            await _database.AcademicTerms
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == termId &&
                        x.TenantId == tenantId &&
                        x.AcademicSessionId ==
                            sessionId &&
                        x.IsActive,
                    cancellationToken);

        if (!valid)
        {
            throw new InvalidOperationException(
                "The selected academic term is not part of the current session.");
        }
    }

    private async Task ValidateClassAndSubjectAsync(
        Guid tenantId,
        Guid classGroupId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        var classExists =
            await _database.ClassGroups
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == classGroupId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (!classExists)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        var subjectExists =
            await _database.Subjects
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == subjectId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (!subjectExists)
        {
            throw new InvalidOperationException(
                "Subject was not found.");
        }
    }

    private static void ValidateType(
        string type)
    {
        if (string.IsNullOrWhiteSpace(type) ||
            !AllowedTypes.Contains(
                type.Trim()))
        {
            throw new InvalidOperationException(
                "Assessment type must be Test, Assignment, Homework, Project, Quiz, Exam or Other.");
        }
    }

    private static string NormalizeType(
        string type)
    {
        var value = type.Trim();

        return AllowedTypes
            .First(x =>
                string.Equals(
                    x,
                    value,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateWeightTotal(
        decimal total)
    {
        if (total > 100m)
        {
            throw new InvalidOperationException(
                $"Assessment weights cannot exceed 100%. The proposed total is {total:0.##}%.");
        }
    }

    private static AssessmentStudentScoreResult BuildScoreRow(
        Guid studentId,
        string admissionNumber,
        string studentName,
        decimal? rawScore,
        decimal maximumScore,
        decimal weight)
    {
        if (!rawScore.HasValue)
        {
            return new AssessmentStudentScoreResult(
                studentId,
                admissionNumber,
                studentName,
                null,
                null,
                null);
        }

        var percentage =
            Round(
                rawScore.Value /
                maximumScore *
                100m);

        var contribution =
            Round(
                rawScore.Value /
                maximumScore *
                weight);

        return new AssessmentStudentScoreResult(
            studentId,
            admissionNumber,
            studentName,
            rawScore,
            percentage,
            contribution);
    }

    private static decimal Round(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private sealed record SessionProjection(
        Guid Id,
        string Name);

    private sealed record StudentProjection(
        Guid Id,
        string AdmissionNumber,
        string Name);

    private sealed record AssessmentProjection(
        Guid Id,
        Guid AcademicSessionId,
        Guid AcademicTermId,
        Guid ClassGroupId,
        string ClassGroupName,
        string AcademicLevelName,
        Guid SubjectId,
        string SubjectName,
        string Type,
        string Title,
        decimal MaximumScore,
        decimal WeightPercentage,
        int SortOrder,
        bool IsActive,
        DateTime CreatedAtUtc);
}
