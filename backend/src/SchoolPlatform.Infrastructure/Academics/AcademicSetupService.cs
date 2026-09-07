using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Academics;

public sealed class AcademicSetupService : IAcademicSetupService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public AcademicSetupService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<AcademicSetupResult> GetSetupAsync(
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
            .Select(x => new AcademicSessionResult(
                x.Id,
                x.Name,
                x.StartDate,
                x.EndDate,
                x.IsCurrent,
                x.Terms
                    .Where(term =>
                        term.TenantId == tenantId &&
                        term.IsActive)
                    .OrderBy(term => term.SortOrder)
                    .Select(term => new AcademicTermResult(
                        term.Id,
                        term.Name,
                        term.StartDate,
                        term.EndDate,
                        term.SortOrder))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        var campuses = await _database.Campuses
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CampusResult(
                x.Id,
                x.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        var levels = await _database.AcademicLevels
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new AcademicLevelResult(
                x.Id,
                x.Name,
                x.Category,
                x.SortOrder,
                x.IsActive,
                x.Classes.Count(classGroup =>
                    classGroup.TenantId == tenantId &&
                    classGroup.IsActive)))
            .ToListAsync(cancellationToken);

        var classes = await _database.ClassGroups
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.AcademicLevel.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ClassGroupResult(
                x.Id,
                x.Name,
                x.CampusId,
                x.AcademicLevelId,
                x.AcademicLevel.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        var subjects = await _database.Subjects
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => new SubjectResult(
                x.Id,
                x.Name,
                x.Code,
                x.Category,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return new AcademicSetupResult(
            currentSession,
            campuses,
            levels,
            classes,
            subjects);
    }

    public async Task<AcademicSessionResult> CreateSessionAsync(
        CreateAcademicSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var name = request.Name.Trim();

        var exists = await _database.AcademicSessions
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Name == name,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Academic session '{name}' already exists.");
        }

        if (request.IsCurrent)
        {
            var currentSessions = await _database.AcademicSessions
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent)
                .ToListAsync(cancellationToken);

            foreach (var current in currentSessions)
            {
                current.RemoveCurrentStatus();
            }
        }

        var session = new AcademicSession(
            tenantId,
            name,
            request.StartDate,
            request.EndDate,
            request.IsCurrent);

        _database.AcademicSessions.Add(session);

        await _database.SaveChangesAsync(cancellationToken);

        return new AcademicSessionResult(
            session.Id,
            session.Name,
            session.StartDate,
            session.EndDate,
            session.IsCurrent,
            Array.Empty<AcademicTermResult>());
    }

    public async Task<AcademicTermResult> CreateTermAsync(
        CreateAcademicTermRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var session = await _database.AcademicSessions
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

        if (request.StartDate < session.StartDate ||
            request.EndDate > session.EndDate)
        {
            throw new InvalidOperationException(
                "Term dates must fall within the academic session.");
        }

        var name = request.Name.Trim();

        var duplicateName = await _database.AcademicTerms
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.Name == name,
                cancellationToken);

        if (duplicateName)
        {
            throw new InvalidOperationException(
                $"Academic term '{name}' already exists in this session.");
        }

        var duplicateSortOrder = await _database.AcademicTerms
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.SortOrder == request.SortOrder,
                cancellationToken);

        if (duplicateSortOrder)
        {
            throw new InvalidOperationException(
                $"Term order '{request.SortOrder}' is already in use.");
        }

        var term = new AcademicTerm(
            tenantId,
            session.Id,
            name,
            request.StartDate,
            request.EndDate,
            request.SortOrder);

        _database.AcademicTerms.Add(term);

        await _database.SaveChangesAsync(cancellationToken);

        return new AcademicTermResult(
            term.Id,
            term.Name,
            term.StartDate,
            term.EndDate,
            term.SortOrder);
    }

    public async Task<AcademicLevelResult> CreateLevelAsync(
        CreateAcademicLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var name = request.Name.Trim();
        var category = request.Category.Trim();

        var exists = await _database.AcademicLevels
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Name == name,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Academic level '{name}' already exists.");
        }

        var level = new AcademicLevel(
            tenantId,
            name,
            category,
            request.SortOrder);

        _database.AcademicLevels.Add(level);

        await _database.SaveChangesAsync(cancellationToken);

        return new AcademicLevelResult(
            level.Id,
            level.Name,
            level.Category,
            level.SortOrder,
            level.IsActive,
            0);
    }

    public async Task<ClassGroupResult> CreateClassAsync(
        CreateClassGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var campus = await _database.Campuses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.Id == request.CampusId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (campus is null)
        {
            throw new InvalidOperationException(
                "Campus was not found.");
        }

        var level = await _database.AcademicLevels
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

        var name = request.Name.Trim();

        var exists = await _database.ClassGroups
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.CampusId == request.CampusId &&
                    x.Name == name,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Class '{name}' already exists on {campus.Name}.");
        }

        var classGroup = new ClassGroup(
            tenantId,
            request.CampusId,
            level.Id,
            name);

        _database.ClassGroups.Add(classGroup);

        await _database.SaveChangesAsync(cancellationToken);

        return new ClassGroupResult(
            classGroup.Id,
            classGroup.Name,
            classGroup.CampusId,
            classGroup.AcademicLevelId,
            level.Name,
            classGroup.IsActive);
    }

    public async Task<SubjectResult> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();
        var category = request.Category.Trim();

        var duplicateName = await _database.Subjects
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Name == name,
                cancellationToken);

        if (duplicateName)
        {
            throw new InvalidOperationException(
                $"Subject '{name}' already exists.");
        }

        var duplicateCode = await _database.Subjects
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Code == code,
                cancellationToken);

        if (duplicateCode)
        {
            throw new InvalidOperationException(
                $"Subject code '{code}' is already in use.");
        }

        var subject = new Subject(
            tenantId,
            name,
            code,
            category);

        _database.Subjects.Add(subject);

        await _database.SaveChangesAsync(cancellationToken);

        return new SubjectResult(
            subject.Id,
            subject.Name,
            subject.Code,
            subject.Category,
            subject.IsActive);
    }

    public async Task<AcademicLevelResult> UpdateLevelAsync(
        Guid id,
        UpdateAcademicLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var name = request.Name.Trim();
        var category = request.Category.Trim();

        var exists = await _database.AcademicLevels
            .AnyAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId,
                cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Academic level was not found.");
        }

        var duplicate = await _database.AcademicLevels
            .AnyAsync(
                x =>
                    x.Id != id &&
                    x.TenantId == tenantId &&
                    x.Name == name,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Academic level '{name}' already exists.");
        }

        await _database.AcademicLevels
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Name, name)
                    .SetProperty(x => x.Category, category)
                    .SetProperty(x => x.SortOrder, request.SortOrder),
                cancellationToken);

        return await GetLevelAsync(
            id,
            tenantId,
            cancellationToken);
    }

    public async Task<AcademicLevelResult> SetLevelStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var affected = await _database.AcademicLevels
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsActive, isActive),
                cancellationToken);

        if (affected == 0)
        {
            throw new InvalidOperationException(
                "Academic level was not found.");
        }

        return await GetLevelAsync(
            id,
            tenantId,
            cancellationToken);
    }

    public async Task<ClassGroupResult> UpdateClassAsync(
        Guid id,
        UpdateClassGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var exists = await _database.ClassGroups
            .AnyAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId,
                cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        var campusExists = await _database.Campuses
            .AnyAsync(
                x =>
                    x.Id == request.CampusId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (!campusExists)
        {
            throw new InvalidOperationException(
                "Campus was not found.");
        }

        var levelExists = await _database.AcademicLevels
            .AnyAsync(
                x =>
                    x.Id == request.AcademicLevelId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (!levelExists)
        {
            throw new InvalidOperationException(
                "Academic level was not found.");
        }

        var name = request.Name.Trim();

        var duplicate = await _database.ClassGroups
            .AnyAsync(
                x =>
                    x.Id != id &&
                    x.TenantId == tenantId &&
                    x.CampusId == request.CampusId &&
                    x.Name == name,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Class '{name}' already exists on this campus.");
        }

        await _database.ClassGroups
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Name, name)
                    .SetProperty(x => x.CampusId, request.CampusId)
                    .SetProperty(x => x.AcademicLevelId, request.AcademicLevelId),
                cancellationToken);

        return await GetClassAsync(
            id,
            tenantId,
            cancellationToken);
    }

    public async Task<ClassGroupResult> SetClassStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var affected = await _database.ClassGroups
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsActive, isActive),
                cancellationToken);

        if (affected == 0)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        return await GetClassAsync(
            id,
            tenantId,
            cancellationToken);
    }

    public async Task<SubjectResult> UpdateSubjectAsync(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var exists = await _database.Subjects
            .AnyAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId,
                cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Subject was not found.");
        }

        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();
        var category = request.Category.Trim();

        var duplicateName = await _database.Subjects
            .AnyAsync(
                x =>
                    x.Id != id &&
                    x.TenantId == tenantId &&
                    x.Name == name,
                cancellationToken);

        if (duplicateName)
        {
            throw new InvalidOperationException(
                $"Subject '{name}' already exists.");
        }

        var duplicateCode = await _database.Subjects
            .AnyAsync(
                x =>
                    x.Id != id &&
                    x.TenantId == tenantId &&
                    x.Code == code,
                cancellationToken);

        if (duplicateCode)
        {
            throw new InvalidOperationException(
                $"Subject code '{code}' is already in use.");
        }

        await _database.Subjects
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Name, name)
                    .SetProperty(x => x.Code, code)
                    .SetProperty(x => x.Category, category),
                cancellationToken);

        return await GetSubjectAsync(
            id,
            tenantId,
            cancellationToken);
    }

    public async Task<SubjectResult> SetSubjectStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var affected = await _database.Subjects
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsActive, isActive),
                cancellationToken);

        if (affected == 0)
        {
            throw new InvalidOperationException(
                "Subject was not found.");
        }

        return await GetSubjectAsync(
            id,
            tenantId,
            cancellationToken);
    }

    private async Task<AcademicLevelResult> GetLevelAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _database.AcademicLevels
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .Select(x => new AcademicLevelResult(
                x.Id,
                x.Name,
                x.Category,
                x.SortOrder,
                x.IsActive,
                x.Classes.Count(classGroup =>
                    classGroup.TenantId == tenantId &&
                    classGroup.IsActive)))
            .SingleAsync(cancellationToken);
    }

    private async Task<ClassGroupResult> GetClassAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _database.ClassGroups
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .Select(x => new ClassGroupResult(
                x.Id,
                x.Name,
                x.CampusId,
                x.AcademicLevelId,
                x.AcademicLevel.Name,
                x.IsActive))
            .SingleAsync(cancellationToken);
    }

    private async Task<SubjectResult> GetSubjectAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _database.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.TenantId == tenantId)
            .Select(x => new SubjectResult(
                x.Id,
                x.Name,
                x.Code,
                x.Category,
                x.IsActive))
            .SingleAsync(cancellationToken);
    }
}
