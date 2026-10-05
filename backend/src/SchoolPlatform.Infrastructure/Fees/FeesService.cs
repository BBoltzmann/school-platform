using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Fees;
using SchoolPlatform.Domain.Fees;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Fees;

public sealed class FeesService : IFeesService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public FeesService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<FeesSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var session =
            await _database.AcademicSessions
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (session is null)
        {
            return new FeesSetupResult(
                null,
                [],
                [],
                [],
                [],
                [],
                []);
        }

        var terms =
            await _database.AcademicTerms
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x =>
                    new FeesOptionResult(
                        x.Id,
                        x.Name))
                .ToListAsync(
                    cancellationToken);

        var levels =
            await _database.AcademicLevels
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x =>
                    new FeesOptionResult(
                        x.Id,
                        x.Name))
                .ToListAsync(
                    cancellationToken);

        var classes =
            await _database.ClassGroups
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.AcademicLevel.Name)
                .ThenBy(x => x.Name)
                .Select(x =>
                    new FeesOptionResult(
                        x.Id,
                        x.AcademicLevel.Name +
                        " — " +
                        x.Name))
                .ToListAsync(
                    cancellationToken);

        var students =
            await _database.Students
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x =>
                    new FeesStudentOptionResult(
                        x.Id,
                        x.FirstName +
                        " " +
                        x.LastName,
                        x.AdmissionNumber,
                        x.Enrollments
                            .Where(enrollment =>
                                enrollment.TenantId == tenantId &&
                                enrollment.IsCurrent &&
                                enrollment.IsActive &&
                                enrollment.AcademicSessionId == session.Id)
                            .Select(enrollment =>
                                enrollment.ClassGroup.AcademicLevel.Name +
                                " — " +
                                enrollment.ClassGroup.Name)
                            .FirstOrDefault()))
                .ToListAsync(
                    cancellationToken);

        var feeItems =
            await _database.FeeItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x =>
                    new FeeItemResult(
                        x.Id,
                        x.Name,
                        x.Code,
                        x.Description))
                .ToListAsync(
                    cancellationToken);

        var structureIds =
            await _database.FeeStructures
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.IsActive)
                .OrderByDescending(x =>
                    x.CreatedAtUtc)
                .Select(x => x.Id)
                .ToListAsync(
                    cancellationToken);

        var structures =
            new List<FeeStructureResult>();

        foreach (var id in structureIds)
        {
            structures.Add(
                await GetStructureAsync(
                    id,
                    tenantId,
                    cancellationToken));
        }

        return new FeesSetupResult(
            session,
            terms,
            levels,
            classes,
            students,
            feeItems,
            structures);
    }

    public async Task<FeeItemResult> CreateFeeItemAsync(
        CreateFeeItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var item =
            new FeeItem(
                tenantId,
                request.Name,
                request.Code,
                request.Description);

        _database.FeeItems.Add(item);

        await _database.SaveChangesAsync(
            cancellationToken);

        return new FeeItemResult(
            item.Id,
            item.Name,
            item.Code,
            item.Description);
    }

    public async Task<FeeStructureResult> CreateStructureAsync(
        CreateFeeStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var sessionId =
            await GetCurrentSessionIdAsync(
                tenantId,
                cancellationToken);

        await ValidateTermAsync(
            tenantId,
            sessionId,
            request.AcademicTermId,
            cancellationToken);

        await ValidateAudienceAsync(
            tenantId,
            request.AudienceType,
            request.AudienceId,
            cancellationToken);

        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Add at least one fee item to the structure.");
        }

        var duplicate =
            request.Lines
                .GroupBy(x => x.FeeItemId)
                .FirstOrDefault(x =>
                    x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                "The same fee item cannot appear twice.");
        }

        var feeItemIds =
            request.Lines
                .Select(x => x.FeeItemId)
                .Distinct()
                .ToList();

        var validCount =
            await _database.FeeItems
                .CountAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.IsActive &&
                        feeItemIds.Contains(x.Id),
                    cancellationToken);

        if (validCount != feeItemIds.Count)
        {
            throw new InvalidOperationException(
                "One or more fee items were not found.");
        }

        var structure =
            new FeeStructure(
                tenantId,
                sessionId,
                request.AcademicTermId,
                request.Name,
                request.AudienceType,
                request.AudienceId);

        _database.FeeStructures.Add(
            structure);

        foreach (var line in request.Lines)
        {
            _database.FeeStructureLines.Add(
                new FeeStructureLine(
                    tenantId,
                    structure.Id,
                    line.FeeItemId,
                    line.Amount,
                    line.IsRequired));
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetStructureAsync(
            structure.Id,
            tenantId,
            cancellationToken);
    }

    public async Task<FeeStructureResult> UpdateStructureAsync(
        Guid feeStructureId,
        UpdateFeeStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var structure = await _database.FeeStructures
            .Include(x => x.Lines)
                .ThenInclude(x => x.FeeItem)
            .SingleOrDefaultAsync(x =>
                x.Id == feeStructureId &&
                x.TenantId == tenantId &&
                x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Fee structure was not found.");

        await ValidateAudienceAsync(
            tenantId,
            request.AudienceType,
            request.AudienceId,
            cancellationToken);

        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "A fee structure must contain at least one fee item.");
        }

        var duplicate = request.Lines
            .GroupBy(x => x.FeeItemId)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                "The same fee item cannot appear twice.");
        }

        var feeItemIds = request.Lines
            .Select(x => x.FeeItemId)
            .Distinct()
            .ToList();

        var validFeeItems = await _database.FeeItems
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive &&
                feeItemIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (validFeeItems.Count != feeItemIds.Count)
        {
            throw new InvalidOperationException(
                "One or more fee items were not found.");
        }

        var requestedExistingIds = request.Lines
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        var unknownLineId = requestedExistingIds
            .Except(structure.Lines.Select(x => x.Id))
            .FirstOrDefault();

        if (unknownLineId != Guid.Empty)
        {
            throw new InvalidOperationException(
                "One or more fee structure lines were not found.");
        }

        foreach (var existingLine in structure.Lines.ToList())
        {
            if (requestedExistingIds.Contains(existingLine.Id))
            {
                continue;
            }

            var hasCharges = await _database.StudentFeeCharges.AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.FeeStructureLineId == existingLine.Id,
                cancellationToken);

            if (hasCharges)
            {
                throw new InvalidOperationException(
                    $"{existingLine.FeeItem.Name} cannot be removed because charges already exist for it. Add a new fee item instead; historical charges are preserved.");
            }

            _database.FeeStructureLines.Remove(existingLine);
        }

        foreach (var requestedLine in request.Lines)
        {
            if (requestedLine.Id is Guid lineId)
            {
                var existingLine = structure.Lines.Single(x => x.Id == lineId);
                var hasCharges = await _database.StudentFeeCharges.AnyAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.FeeStructureLineId == lineId,
                    cancellationToken);

                if (hasCharges && existingLine.FeeItemId != requestedLine.FeeItemId)
                {
                    throw new InvalidOperationException(
                        $"{existingLine.FeeItem.Name} cannot be changed because charges already exist for it.");
                }

                existingLine.Update(
                    requestedLine.FeeItemId,
                    requestedLine.Amount,
                    requestedLine.IsRequired);
            }
            else
            {
                _database.FeeStructureLines.Add(
                    new FeeStructureLine(
                        tenantId,
                        structure.Id,
                        requestedLine.FeeItemId,
                        requestedLine.Amount,
                        requestedLine.IsRequired));
            }
        }

        structure.UpdateDetails(
            request.Name,
            request.AudienceType,
            request.AudienceId);

        await _database.SaveChangesAsync(cancellationToken);

        return await GetStructureAsync(
            structure.Id,
            tenantId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<FeeStructureStudentResult>>
        GetAssignedStudentsAsync(
            Guid feeStructureId,
            CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureStructureAsync(
            feeStructureId,
            tenantId,
            cancellationToken);

        return await _database.FeeStructureStudentAssignments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.FeeStructureId == feeStructureId &&
                x.Student.TenantId == tenantId &&
                x.Student.IsActive)
            .OrderBy(x => x.Student.LastName)
            .ThenBy(x => x.Student.FirstName)
            .Select(x => new FeeStructureStudentResult(
                x.StudentId,
                x.Student.FirstName + " " + x.Student.LastName,
                x.Student.AdmissionNumber,
                x.Student.Enrollments
                    .Where(enrollment =>
                        enrollment.TenantId == tenantId &&
                        enrollment.IsCurrent &&
                        enrollment.IsActive &&
                        enrollment.AcademicSessionId == x.FeeStructure.AcademicSessionId)
                    .Select(enrollment =>
                        enrollment.ClassGroup.AcademicLevel.Name +
                        " — " +
                        enrollment.ClassGroup.Name)
                    .FirstOrDefault(),
                x.AssignedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FeeStructureStudentResult>>
        ReplaceAssignedStudentsAsync(
            Guid feeStructureId,
            ReplaceFeeStructureStudentsRequest request,
            CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var structure = await EnsureStructureAsync(
            feeStructureId,
            tenantId,
            cancellationToken);

        var studentIds = request.StudentIds
            .Distinct()
            .ToList();

        var students = await _database.Students
            .Where(x =>
                studentIds.Contains(x.Id) &&
                x.TenantId == tenantId &&
                x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (students.Count != studentIds.Count)
        {
            throw new InvalidOperationException(
                "One or more selected students were not found in this school.");
        }

        var existing = await _database.FeeStructureStudentAssignments
            .Where(x =>
                x.TenantId == tenantId &&
                x.FeeStructureId == feeStructureId)
            .ToListAsync(cancellationToken);

        var desired = studentIds.ToHashSet();

        _database.FeeStructureStudentAssignments.RemoveRange(
            existing.Where(x => !desired.Contains(x.StudentId)));

        var existingIds = existing
            .Select(x => x.StudentId)
            .ToHashSet();

        foreach (var studentId in studentIds.Where(x => !existingIds.Contains(x)))
        {
            _database.FeeStructureStudentAssignments.Add(
                new FeeStructureStudentAssignment(
                    tenantId,
                    structure.Id,
                    studentId));
        }

        await _database.SaveChangesAsync(cancellationToken);

        return await GetAssignedStudentsAsync(
            feeStructureId,
            cancellationToken);
    }

    public async Task RemoveStudentAssignmentAsync(
        Guid feeStructureId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureStructureAsync(
            feeStructureId,
            tenantId,
            cancellationToken);

        var assignment = await _database.FeeStructureStudentAssignments
            .SingleOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.FeeStructureId == feeStructureId &&
                x.StudentId == studentId,
                cancellationToken);

        if (assignment is not null)
        {
            _database.FeeStructureStudentAssignments.Remove(assignment);
            await _database.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<GenerateChargesResult> GenerateChargesAsync(
        Guid feeStructureId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var structure =
            await _database.FeeStructures
                .AsNoTracking()
                .Include(x => x.Lines)
                    .ThenInclude(x =>
                        x.FeeItem)
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == feeStructureId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Fee structure was not found.");

        // The selected structure determines the affected population. Existing
        // charges from this structure are included so removing an assignment
        // can deactivate obsolete unpaid charges during the same sync.
        var selectedTargetIds = await GetStructureTargetStudentIdsAsync(
            structure,
            tenantId,
            cancellationToken);
        var historicalTargetIds = await _database.StudentFeeCharges
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AcademicTermId == structure.AcademicTermId &&
                x.FeeStructureId == structure.Id &&
                x.IsActive)
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var studentIds = selectedTargetIds
            .Concat(historicalTargetIds)
            .Distinct()
            .ToList();

        var structures = await _database.FeeStructures
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AcademicSessionId == structure.AcademicSessionId &&
                x.AcademicTermId == structure.AcademicTermId &&
                x.IsActive)
            .Include(x => x.Lines)
                .ThenInclude(x => x.FeeItem)
            .ToListAsync(cancellationToken);
        var structureIds = structures.Select(x => x.Id).ToArray();
        var assignments = await _database.FeeStructureStudentAssignments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && structureIds.Contains(x.FeeStructureId))
            .Select(x => new { x.FeeStructureId, x.StudentId })
            .ToListAsync(cancellationToken);
        var assignmentLookup = assignments
            .GroupBy(x => x.FeeStructureId)
            .ToDictionary(x => x.Key, x => x.Select(v => v.StudentId).ToHashSet());
        var expected = new Dictionary<Guid, List<(Guid StructureId, FeeStructureLine Line)>>();
        foreach (var studentId in studentIds)
        {
            foreach (var candidate in structures)
            {
                var assigned = assignmentLookup.GetValueOrDefault(candidate.Id);
                var applicable = assigned?.Contains(studentId) == true;
                if (!applicable) continue;
                foreach (var line in candidate.Lines.Where(x => x.IsRequired))
                {
                    if (!expected.TryGetValue(studentId, out var lines))
                        expected[studentId] = lines = [];
                    lines.Add((candidate.Id, line));
                }
            }
        }

        var generatedCharges = await _database.StudentFeeCharges
            .Where(x =>
                x.TenantId == tenantId &&
                studentIds.Contains(x.StudentId) &&
                x.AcademicTermId == structure.AcademicTermId &&
                x.FeeStructureId.HasValue &&
                x.FeeStructureLineId.HasValue)
            .ToListAsync(cancellationToken);
        var activeCharges = generatedCharges.Where(x => x.IsActive).ToList();
        var chargesByKey = generatedCharges
            .GroupBy(x => (x.StudentId, FeeStructureLineId: x.FeeStructureLineId!.Value))
            .ToDictionary(x => x.Key, x => x.OrderByDescending(v => v.IsActive).First());
        var chargeIds = generatedCharges.Select(x => x.Id).ToArray();
        var allocatedChargeIds = (await _database.FeePaymentAllocations
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && chargeIds.Contains(x.StudentFeeChargeId))
            .Select(x => x.StudentFeeChargeId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var created = 0;
        var retained = 0;
        var deactivated = 0;
        var reactivated = 0;
        var protectedCharges = new List<ProtectedFeeChargeResult>();
        decimal total = 0m;
        foreach (var studentId in studentIds)
        {
            var expectedLines = expected.GetValueOrDefault(studentId, []);
            var studentCharges = activeCharges.Where(x => x.StudentId == studentId).ToList();
            var expectedKeys = expectedLines
                .Select(x => (x.StructureId, LineId: x.Line.Id))
                .ToHashSet();
            var optionalKeys = structures
                .Where(candidate =>
                    assignmentLookup.GetValueOrDefault(candidate.Id)?.Contains(studentId) == true)
                .SelectMany(candidate => candidate.Lines
                    .Where(line => !line.IsRequired)
                    .Select(line => (StructureId: candidate.Id, LineId: line.Id)))
                .ToHashSet();
            var retainedKeys = new HashSet<(Guid StructureId, Guid LineId)>();
            foreach (var charge in studentCharges)
            {
                var key = (charge.FeeStructureId!.Value, charge.FeeStructureLineId!.Value);
                if ((expectedKeys.Contains(key) || optionalKeys.Contains(key)) && retainedKeys.Add(key))
                {
                    retained++;
                    continue;
                }
                if (charge.AmountPaid == 0m && !allocatedChargeIds.Contains(charge.Id))
                {
                    charge.Deactivate();
                    deactivated++;
                }
                else
                {
                    protectedCharges.Add(new ProtectedFeeChargeResult(
                        charge.Id, charge.StudentId, charge.Description, charge.Amount, charge.AmountPaid));
                }
            }
            foreach (var (structureId, line) in expectedLines)
            {
                if (retainedKeys.Contains((structureId, line.Id))) continue;
                var provenanceKey = (studentId, FeeStructureLineId: line.Id);
                if (chargesByKey.TryGetValue(provenanceKey, out var historicalCharge))
                {
                    if (historicalCharge.AmountPaid == 0m && !allocatedChargeIds.Contains(historicalCharge.Id))
                    {
                        historicalCharge.Reactivate();
                        reactivated++;
                        retainedKeys.Add((structureId, line.Id));
                        continue;
                    }

                    protectedCharges.Add(new ProtectedFeeChargeResult(
                        historicalCharge.Id,
                        historicalCharge.StudentId,
                        historicalCharge.Description,
                        historicalCharge.Amount,
                        historicalCharge.AmountPaid));
                    retainedKeys.Add((structureId, line.Id));
                    continue;
                }
                _database.StudentFeeCharges.Add(new StudentFeeCharge(
                    tenantId, studentId, structure.AcademicSessionId, structure.AcademicTermId,
                    line.FeeItemId, structureId, line.Id, line.FeeItem.Name, line.Amount));
                retainedKeys.Add((structureId, line.Id));
                created++;
                total += line.Amount;
            }
        }

        await _database.SaveChangesAsync(cancellationToken);
        return new GenerateChargesResult(
            structure.Id, selectedTargetIds.Count, created, total, retained,
            reactivated, deactivated, protectedCharges.Count, protectedCharges);
    }

    public async Task<ReconcileTermFeesResult> ReconcileTermFeesAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var termExists = await _database.AcademicTerms.AnyAsync(x =>
            x.TenantId == tenantId && x.Id == academicTermId && x.IsActive,
            cancellationToken);
        if (!termExists)
        {
            throw new InvalidOperationException("Academic term was not found.");
        }

        var structureIds = await _database.FeeStructures
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AcademicTermId == academicTermId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var summaries = new List<GenerateChargesResult>();
        foreach (var structureId in structureIds)
        {
            summaries.Add(await GenerateChargesAsync(structureId, cancellationToken));
        }

        return new ReconcileTermFeesResult(
            academicTermId,
            structureIds.Count,
            summaries.Sum(x => x.StudentCount),
            summaries.Sum(x => x.ChargesCreated),
            summaries.Sum(x => x.RetainedCount),
            summaries.Sum(x => x.DeactivatedCount),
            summaries.Sum(x => x.ProtectedCount));
    }

    private async Task<List<Guid>> GetStructureTargetStudentIdsAsync(
        FeeStructure structure,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var assigned = await _database.FeeStructureStudentAssignments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FeeStructureId == structure.Id && x.Student.IsActive)
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return assigned;
    }

    public async Task<StudentFeeChargeResult> CreateStudentChargeAsync(
        Guid studentId,
        CreateStudentChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var sessionId =
            await GetCurrentSessionIdAsync(
                tenantId,
                cancellationToken);

        await ValidateTermAsync(
            tenantId,
            sessionId,
            request.AcademicTermId,
            cancellationToken);

        var studentExists =
            await _database.Students.AnyAsync(
                x =>
                    x.Id == studentId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (!studentExists)
        {
            throw new InvalidOperationException(
                "Student was not found.");
        }

        var feeItem =
            await _database.FeeItems
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == request.FeeItemId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Fee item was not found.");

        var charge =
            new StudentFeeCharge(
                tenantId,
                studentId,
                sessionId,
                request.AcademicTermId,
                feeItem.Id,
                null,
                null,
                request.Description,
                request.Amount);

        _database.StudentFeeCharges.Add(
            charge);

        await _database.SaveChangesAsync(
            cancellationToken);

        return ToChargeResult(
            charge,
            feeItem.Name);
    }

    public async Task<IReadOnlyCollection<OptionalFeeComponentResult>>
        GetOptionalFeeComponentsAsync(
            Guid studentId,
            Guid academicTermId,
            CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var sessionId = await GetCurrentSessionIdAsync(tenantId, cancellationToken);
        await ValidateTermAsync(tenantId, sessionId, academicTermId, cancellationToken);
        var studentExists = await _database.Students.AnyAsync(
            x => x.TenantId == tenantId && x.Id == studentId && x.IsActive,
            cancellationToken);
        if (!studentExists) throw new InvalidOperationException("Student was not found.");

        var assignedStructureIds = await _database.FeeStructureStudentAssignments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.StudentId == studentId)
            .Select(x => x.FeeStructureId)
            .ToListAsync(cancellationToken);
        var lines = await _database.FeeStructureLines
            .AsNoTracking()
            .Include(x => x.FeeStructure)
            .Include(x => x.FeeItem)
            .Where(x =>
                x.TenantId == tenantId &&
                assignedStructureIds.Contains(x.FeeStructureId) &&
                x.FeeStructure.AcademicSessionId == sessionId &&
                x.FeeStructure.AcademicTermId == academicTermId &&
                x.FeeStructure.IsActive &&
                !x.IsRequired)
            .OrderBy(x => x.FeeStructure.Name)
            .ThenBy(x => x.FeeItem.Name)
            .ToListAsync(cancellationToken);
        var lineIds = lines.Select(x => x.Id).ToArray();
        var existing = await _database.StudentFeeCharges
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId && x.StudentId == studentId &&
                x.AcademicTermId == academicTermId &&
                x.FeeStructureLineId.HasValue && lineIds.Contains(x.FeeStructureLineId.Value))
            .ToDictionaryAsync(x => x.FeeStructureLineId!.Value, cancellationToken);
        return lines.Select(line =>
        {
            existing.TryGetValue(line.Id, out var charge);
            return new OptionalFeeComponentResult(
                line.FeeStructureId,
                line.FeeStructure.Name,
                line.Id,
                line.FeeItemId,
                line.FeeItem.Name,
                line.FeeItem.Code,
                line.Amount,
                charge is not null,
                charge?.Id,
                charge?.Amount);
        }).ToList();
    }

    public async Task<StudentFeeChargeResult> AddOptionalFeeComponentAsync(
        Guid studentId,
        AddOptionalFeeComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var sessionId = await GetCurrentSessionIdAsync(tenantId, cancellationToken);
        await ValidateTermAsync(tenantId, sessionId, request.AcademicTermId, cancellationToken);
        if (request.Amount <= 0) throw new InvalidOperationException("Charge amount must be greater than zero.");
        var line = await _database.FeeStructureLines
            .Include(x => x.FeeStructure)
            .Include(x => x.FeeItem)
            .SingleOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == request.FeeStructureLineId &&
                x.FeeStructure.AcademicSessionId == sessionId &&
                x.FeeStructure.AcademicTermId == request.AcademicTermId &&
                x.FeeStructure.IsActive && !x.IsRequired, cancellationToken)
            ?? throw new InvalidOperationException("Optional fee component was not found.");
        var assigned = await _database.FeeStructureStudentAssignments.AnyAsync(x =>
            x.TenantId == tenantId && x.FeeStructureId == line.FeeStructureId && x.StudentId == studentId, cancellationToken);
        if (!assigned) throw new InvalidOperationException("Student is not explicitly assigned to this fee structure.");
        var existing = await _database.StudentFeeCharges.SingleOrDefaultAsync(x =>
            x.TenantId == tenantId && x.StudentId == studentId && x.AcademicTermId == request.AcademicTermId &&
            x.FeeStructureLineId == line.Id, cancellationToken);
        if (existing is not null)
        {
            if (existing.AmountPaid > 0 || await _database.FeePaymentAllocations.AnyAsync(x => x.TenantId == tenantId && x.StudentFeeChargeId == existing.Id, cancellationToken))
                throw new InvalidOperationException("This optional component already has payment history.");
            existing.UpdateAmount(request.Amount);
            existing.Reactivate();
            await _database.SaveChangesAsync(cancellationToken);
            return ToChargeResult(existing, line.FeeItem.Name, line.FeeStructure.Name);
        }
        var charge = new StudentFeeCharge(tenantId, studentId, sessionId, request.AcademicTermId, line.FeeItemId, line.FeeStructureId, line.Id, line.FeeItem.Name, request.Amount);
        _database.StudentFeeCharges.Add(charge);
        await _database.SaveChangesAsync(cancellationToken);
        return ToChargeResult(charge, line.FeeItem.Name, line.FeeStructure.Name);
    }

    public async Task<StudentFeeChargeResult> UpdateOptionalFeeChargeAsync(
        Guid studentId,
        Guid chargeId,
        UpdateOptionalFeeChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var charge = await _database.StudentFeeCharges
            .Include(x => x.FeeItem)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == chargeId && x.StudentId == studentId && x.IsActive && x.FeeStructureLineId.HasValue, cancellationToken)
            ?? throw new InvalidOperationException("Optional fee charge was not found.");
        var line = await _database.FeeStructureLines.AsNoTracking().Include(x => x.FeeStructure).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == charge.FeeStructureLineId && !x.IsRequired && x.FeeStructure.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Only optional structure charges can be edited.");
        if (await _database.FeePaymentAllocations.AnyAsync(x => x.TenantId == tenantId && x.StudentFeeChargeId == chargeId, cancellationToken) || charge.AmountPaid > 0)
        {
            charge.UpdateAmount(request.Amount);
        }
        else
        {
            charge.UpdateAmount(request.Amount);
        }
        await _database.SaveChangesAsync(cancellationToken);
        var structureName = charge.FeeStructureId.HasValue
            ? await _database.FeeStructures.Where(x => x.TenantId == tenantId && x.Id == charge.FeeStructureId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken)
            : null;
        return ToChargeResult(charge, charge.FeeItem.Name, structureName);
    }

    public async Task RemoveStudentChargeAsync(
        Guid studentId,
        Guid chargeId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var charge = await _database.StudentFeeCharges
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == chargeId && x.StudentId == studentId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Charge was not found for this student.");
        if (charge.FeeStructureLineId.HasValue)
        {
            var line = await _database.FeeStructureLines.AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == charge.FeeStructureLineId.Value, cancellationToken);
            if (line?.IsRequired == true)
                throw new InvalidOperationException("Required generated charges cannot be removed from an individual account.");
        }
        var hasAllocations = await _database.FeePaymentAllocations.AnyAsync(
            x => x.TenantId == tenantId && x.StudentFeeChargeId == chargeId,
            cancellationToken);
        if (charge.AmountPaid > 0m || hasAllocations)
            throw new InvalidOperationException("This charge has payment history and cannot be removed. Reverse or reallocate the payment first.");
        charge.Deactivate();
        await _database.SaveChangesAsync(cancellationToken);
    }

    public async Task<StudentFeeAccountResult> GetStudentAccountAsync(
        Guid studentId,
        Guid academicTermId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var student =
            await _database.Students
                .AsNoTracking()
                .Where(x =>
                    x.Id == studentId &&
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.AdmissionNumber,
                    Name =
                        x.FirstName +
                        " " +
                        x.LastName
                })
                .SingleOrDefaultAsync(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Student was not found.");

        var charges =
            await _database.StudentFeeCharges
                .AsNoTracking()
                .Include(x => x.FeeItem)
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.StudentId == studentId &&
                    x.AcademicTermId == academicTermId &&
                    x.IsActive)
                .OrderBy(x =>
                    x.CreatedAtUtc)
                .ToListAsync(
                    cancellationToken);

        var structureIds = charges
            .Where(x => x.FeeStructureId.HasValue)
            .Select(x => x.FeeStructureId!.Value)
            .Distinct()
            .ToArray();
        var structureNames = await _database.FeeStructures
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && structureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var lineIds = charges.Where(x => x.FeeStructureLineId.HasValue).Select(x => x.FeeStructureLineId!.Value).ToArray();
        var requiredByLine = await _database.FeeStructureLines
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && lineIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.IsRequired, cancellationToken);

        var payments =
            await _database.FeePayments
                .AsNoTracking()
                .Include(x => x.Allocations)
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.StudentId == studentId &&
                    x.AcademicTermId == academicTermId)
                .OrderByDescending(x =>
                    x.CreatedAtUtc)
                .ToListAsync(
                    cancellationToken);
        var paymentIds = payments.Select(x => x.Id).ToArray();
        var allocationRows = await _database.FeePaymentAllocations
            .AsNoTracking()
            .Include(x => x.StudentFeeCharge)
                .ThenInclude(x => x.FeeItem)
            .Where(x => x.TenantId == tenantId && paymentIds.Contains(x.FeePaymentId))
            .ToListAsync(cancellationToken);
        var allocationStructureIds = allocationRows
            .Where(x => x.StudentFeeCharge.FeeStructureId.HasValue)
            .Select(x => x.StudentFeeCharge.FeeStructureId!.Value)
            .Distinct()
            .ToArray();
        var allocationStructureNames = await _database.FeeStructures
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && allocationStructureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var allocationLineIds = allocationRows
            .Where(x => x.StudentFeeCharge.FeeStructureLineId.HasValue)
            .Select(x => x.StudentFeeCharge.FeeStructureLineId!.Value)
            .Distinct()
            .ToArray();
        var allocationRequired = await _database.FeeStructureLines
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && allocationLineIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.IsRequired, cancellationToken);

        var activePayments =
            payments.Where(x =>
                !x.IsReversed);

        var totalPayments =
            activePayments.Sum(x =>
                x.Amount);

        var applied =
            activePayments.Sum(x =>
                x.Allocations.Sum(a =>
                    a.Amount));

        var totalCharges =
            charges.Sum(x =>
                x.Amount);

        var outstanding =
            charges.Sum(x =>
                x.Balance);

        var credit =
            Math.Max(
                totalPayments - applied,
                0m);

        var discountsApplied = await _database.StudentDiscountAssignments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.StudentId == studentId && (x.AcademicTermId == academicTermId || x.AcademicTermId == null) && x.Status == FinanceRecordStatus.Active)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var discountDefinitionIds = discountsApplied.Select(x => x.DiscountDefinitionId).Distinct().ToArray();
        var discountNames = await _database.DiscountDefinitions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && discountDefinitionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var adjustments = await _database.StudentFeeAdjustments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.StudentId == studentId && (x.AcademicTermId == academicTermId || x.AcademicTermId == null))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var activeDiscountTotal = discountsApplied.Sum(x => x.AppliedAmount);
        var debitAdjustments = adjustments.Where(x => x.Status == FinanceRecordStatus.Active && (x.Type is FinancialAdjustmentType.OpeningDebit or FinancialAdjustmentType.ManualDebit or FinancialAdjustmentType.CarryForwardDebit or FinancialAdjustmentType.CarryForwardTransferOutDebit)).Sum(x => x.Amount);
        var creditAdjustments = adjustments.Where(x => x.Status == FinanceRecordStatus.Active && (x.Type is FinancialAdjustmentType.OpeningCredit or FinancialAdjustmentType.ManualCredit or FinancialAdjustmentType.CarryForwardCredit or FinancialAdjustmentType.CarryForwardTransferOutCredit)).Sum(x => x.Amount);
        var unallocatedCredit = credit;
        var netBalance = outstanding - activeDiscountTotal + debitAdjustments - creditAdjustments - unallocatedCredit;
        var ledger = new List<StudentFeeLedgerEntryResult>();
        ledger.AddRange(charges.Select(x => new StudentFeeLedgerEntryResult("charge", x.Description, x.Amount, 0m, x.CreatedAtUtc)));
        ledger.AddRange(discountsApplied.Select(x => new StudentFeeLedgerEntryResult("discount", discountNames.GetValueOrDefault(x.DiscountDefinitionId) ?? "Discount", 0m, x.AppliedAmount, x.CreatedAtUtc)));
        ledger.AddRange(adjustments.Select(x => new StudentFeeLedgerEntryResult("adjustment", x.Description, x.Type is FinancialAdjustmentType.OpeningDebit or FinancialAdjustmentType.ManualDebit or FinancialAdjustmentType.CarryForwardDebit or FinancialAdjustmentType.CarryForwardTransferOutDebit ? x.Amount : 0m, x.Type is FinancialAdjustmentType.OpeningCredit or FinancialAdjustmentType.ManualCredit or FinancialAdjustmentType.CarryForwardCredit or FinancialAdjustmentType.CarryForwardTransferOutCredit ? x.Amount : 0m, x.CreatedAtUtc)));
        ledger.AddRange(activePayments.Select(x => new StudentFeeLedgerEntryResult("payment", x.ReceiptNumber, 0m, x.Amount, x.CreatedAtUtc)));

        return new StudentFeeAccountResult(
            student.Id,
            student.AdmissionNumber,
            student.Name,
            academicTermId,
            totalCharges,
            applied,
            Math.Max(netBalance, 0m),
            Math.Max(-netBalance, 0m),
            charges
                .Select(x =>
                    ToChargeResult(
                        x,
                        x.FeeItem.Name,
                        x.FeeStructureId.HasValue
                            ? structureNames.GetValueOrDefault(x.FeeStructureId.Value)
                            : null,
                        x.FeeStructureLineId.HasValue && requiredByLine.TryGetValue(x.FeeStructureLineId.Value, out var required)
                            ? required
                            : null))
                .ToList(),
            payments
                .Select(x =>
                {
                    var allocated =
                        x.IsReversed
                            ? 0m
                            : x.Allocations.Sum(a =>
                                a.Amount);

                    return new FeePaymentResult(
                        x.Id,
                        x.Amount,
                        allocated,
                        x.IsReversed
                            ? 0m
                            : Math.Max(
                                x.Amount - allocated,
                                0m),
                        x.PaymentMethod,
                        x.ReceiptNumber,
                        x.Reference,
                        x.Notes,
                        x.IsReversed,
                        x.CreatedAtUtc,
                        allocationRows
                            .Where(row => row.FeePaymentId == x.Id)
                            .Select(row =>
                            {
                                var charge = row.StudentFeeCharge;
                                var chargeType = charge.FeeStructureLineId.HasValue && allocationRequired.TryGetValue(charge.FeeStructureLineId.Value, out var required)
                                    ? (required ? "Required" : "Optional")
                                    : "Manual";
                                return new FeePaymentAllocationResult(
                                    charge.Id,
                                    charge.Description,
                                    charge.FeeItem.Name,
                                    charge.FeeStructureId.HasValue
                                        ? allocationStructureNames.GetValueOrDefault(charge.FeeStructureId.Value)
                                        : null,
                                    chargeType,
                                    row.Amount);
                            })
                            .ToList());
                })
                .ToList(),
            activeDiscountTotal,
            debitAdjustments,
            creditAdjustments,
            unallocatedCredit,
            adjustments.Select(x => new StudentFeeAdjustmentResult(x.Id, x.Type.ToString(), x.Amount, x.Description, x.Reason, x.Status == FinanceRecordStatus.Reversed, x.CreatedAtUtc)).ToList(),
            discountsApplied.Select(x => new StudentDiscountResult(x.Id, x.DiscountApplicationId, discountNames.GetValueOrDefault(x.DiscountDefinitionId) ?? "Discount", x.AppliedAmount, x.Status == FinanceRecordStatus.Reversed, x.CreatedAtUtc)).ToList(),
            ledger.OrderBy(x => x.OccurredAtUtc).ToList());
    }

    public async Task<FeePaymentResult> RecordPaymentAsync(
        Guid studentId,
        RecordFeePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var sessionId =
            await GetCurrentSessionIdAsync(
                tenantId,
                cancellationToken);

        await ValidateTermAsync(
            tenantId,
            sessionId,
            request.AcademicTermId,
            cancellationToken);

        var studentExists =
            await _database.Students.AnyAsync(
                x =>
                    x.Id == studentId &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);

        if (!studentExists)
        {
            throw new InvalidOperationException(
                "Student was not found.");
        }

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException(
                "Payment amount must be greater than zero.");
        }

        var receiptNumber =
            $"RCPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var payment =
            new FeePayment(
                tenantId,
                studentId,
                sessionId,
                request.AcademicTermId,
                request.Amount,
                request.PaymentMethod,
                receiptNumber,
                request.Reference,
                request.Notes);

        _database.FeePayments.Add(
            payment);

        var charges =
            await _database.StudentFeeCharges
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.StudentId == studentId &&
                    x.AcademicTermId ==
                        request.AcademicTermId &&
                    x.IsActive &&
                    x.AmountPaid < x.Amount)
                .OrderBy(x =>
                    x.CreatedAtUtc)
                .ToListAsync(
                    cancellationToken);

        var remaining =
            request.Amount;

        foreach (var charge in charges)
        {
            if (remaining <= 0)
            {
                break;
            }

            var allocation =
                Math.Min(
                    remaining,
                    charge.Balance);

            if (allocation <= 0)
            {
                continue;
            }

            charge.ApplyPayment(
                allocation);

            _database.FeePaymentAllocations.Add(
                new FeePaymentAllocation(
                    tenantId,
                    payment.Id,
                    charge.Id,
                    allocation));

            remaining -= allocation;
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        var allocated =
            request.Amount - remaining;
        var receiptAllocations = await _database.FeePaymentAllocations
            .AsNoTracking()
            .Include(x => x.StudentFeeCharge)
                .ThenInclude(x => x.FeeItem)
            .Where(x => x.TenantId == tenantId && x.FeePaymentId == payment.Id)
            .ToListAsync(cancellationToken);
        var receiptStructureIds = receiptAllocations
            .Where(x => x.StudentFeeCharge.FeeStructureId.HasValue)
            .Select(x => x.StudentFeeCharge.FeeStructureId!.Value)
            .Distinct()
            .ToArray();
        var receiptStructures = await _database.FeeStructures
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && receiptStructureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var receiptLineIds = receiptAllocations
            .Where(x => x.StudentFeeCharge.FeeStructureLineId.HasValue)
            .Select(x => x.StudentFeeCharge.FeeStructureLineId!.Value)
            .Distinct()
            .ToArray();
        var receiptRequired = await _database.FeeStructureLines
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && receiptLineIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.IsRequired, cancellationToken);
        var allocationResults = receiptAllocations.Select(row =>
        {
            var charge = row.StudentFeeCharge;
            var type = charge.FeeStructureLineId.HasValue && receiptRequired.TryGetValue(charge.FeeStructureLineId.Value, out var required)
                ? (required ? "Required" : "Optional")
                : "Manual";
            return new FeePaymentAllocationResult(
                charge.Id,
                charge.Description,
                charge.FeeItem.Name,
                charge.FeeStructureId.HasValue ? receiptStructures.GetValueOrDefault(charge.FeeStructureId.Value) : null,
                type,
                row.Amount);
        }).ToList();

        return new FeePaymentResult(
            payment.Id,
            payment.Amount,
            allocated,
            remaining,
            payment.PaymentMethod,
            payment.ReceiptNumber,
            payment.Reference,
            payment.Notes,
            false,
            payment.CreatedAtUtc,
            allocationResults);
    }

    public async Task ReversePaymentAsync(
        Guid paymentId,
        ReverseFeePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _database.FeePayments
            .AsNoTracking()
            .Where(x => x.TenantId == _tenantContext.TenantId && x.Id == paymentId)
            .Select(x => new { x.StudentId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Payment was not found.");

        await VoidPaymentAsync(payment.StudentId, paymentId, request, cancellationToken);
    }

    public async Task VoidPaymentAsync(
        Guid studentId,
        Guid paymentId,
        ReverseFeePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var payment =
            await _database.FeePayments
                .Include(x => x.Allocations)
                    .ThenInclude(x =>
                        x.StudentFeeCharge)
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == paymentId &&
                        x.TenantId == tenantId &&
                        x.StudentId == studentId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Payment was not found.");

        if (payment.IsReversed)
        {
            throw new InvalidOperationException(
                "Payment has already been reversed.");
        }

        await using var transaction = await _database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var allocation in payment.Allocations)
            {
                allocation.StudentFeeCharge.ReversePayment(allocation.Amount);
            }

            payment.Reverse(request.Reason);
            await _database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<OutstandingStudentResult>>
        GetOutstandingAsync(
            Guid academicTermId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var sessionId = await _database.AcademicTerms
            .Where(x => x.TenantId == tenantId && x.Id == academicTermId && x.IsActive)
            .Select(x => x.AcademicSessionId)
            .SingleOrDefaultAsync(cancellationToken);
        if (sessionId == Guid.Empty)
        {
            throw new InvalidOperationException("Academic term was not found.");
        }

        var charges = await _database.StudentFeeCharges
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AcademicTermId == academicTermId &&
                x.IsActive)
            .Select(x => new
            {
                x.StudentId,
                x.Student.AdmissionNumber,
                StudentName = x.Student.FirstName + " " + x.Student.LastName,
                x.Amount,
                x.AmountPaid
            })
            .ToListAsync(cancellationToken);

        var studentIds = charges.Select(x => x.StudentId).Distinct().ToArray();
        var classes = await _database.StudentEnrollments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                studentIds.Contains(x.StudentId) &&
                x.AcademicSessionId == sessionId &&
                x.IsCurrent && x.IsActive)
            .Select(x => new
            {
                x.StudentId,
                ClassName = x.ClassGroup.AcademicLevel.Name + " — " + x.ClassGroup.Name
            })
            .ToListAsync(cancellationToken);
        var classByStudent = classes
            .GroupBy(x => x.StudentId)
            .ToDictionary(x => x.Key, x => x.Select(v => v.ClassName).FirstOrDefault());

        var grouped = charges.GroupBy(x => new { x.StudentId, x.AdmissionNumber, x.StudentName }).ToList();
        var ids = grouped.Select(x => x.Key.StudentId).ToArray();
        var discounts = await _database.StudentDiscountAssignments.AsNoTracking().Where(x => x.TenantId == tenantId && ids.Contains(x.StudentId) && (x.AcademicTermId == academicTermId || x.AcademicTermId == null) && x.Status == FinanceRecordStatus.Active).GroupBy(x => x.StudentId).ToDictionaryAsync(x => x.Key, x => x.Sum(v => v.AppliedAmount), cancellationToken);
        var adjustments = await _database.StudentFeeAdjustments.AsNoTracking().Where(x => x.TenantId == tenantId && ids.Contains(x.StudentId) && (x.AcademicTermId == academicTermId || x.AcademicTermId == null) && x.Status == FinanceRecordStatus.Active).ToListAsync(cancellationToken);
        var payments = await _database.FeePayments.AsNoTracking().Include(x => x.Allocations).Where(x => x.TenantId == tenantId && ids.Contains(x.StudentId) && x.AcademicTermId == academicTermId && !x.IsReversed).ToListAsync(cancellationToken);
        return grouped
            .Select(group => {
                var debit = adjustments.Where(x => x.StudentId == group.Key.StudentId && x.Type is FinancialAdjustmentType.OpeningDebit or FinancialAdjustmentType.ManualDebit or FinancialAdjustmentType.CarryForwardDebit or FinancialAdjustmentType.CarryForwardTransferOutDebit).Sum(x => x.Amount);
                var credit = adjustments.Where(x => x.StudentId == group.Key.StudentId && x.Type is FinancialAdjustmentType.OpeningCredit or FinancialAdjustmentType.ManualCredit or FinancialAdjustmentType.CarryForwardCredit or FinancialAdjustmentType.CarryForwardTransferOutCredit).Sum(x => x.Amount);
                var unallocated = payments.Where(x => x.StudentId == group.Key.StudentId).Sum(x => Math.Max(0m, x.Amount - x.Allocations.Sum(a => a.Amount)));
                var balance = group.Sum(x => x.Amount - x.AmountPaid) - discounts.GetValueOrDefault(group.Key.StudentId) + debit - credit - unallocated;
                return new OutstandingStudentResult(group.Key.StudentId, group.Key.AdmissionNumber, group.Key.StudentName, group.Sum(x => x.Amount), group.Sum(x => x.AmountPaid), Math.Max(balance, 0m), classByStudent.GetValueOrDefault(group.Key.StudentId));
            })
            .Where(x => x.OutstandingBalance > 0)
            .OrderByDescending(x => x.OutstandingBalance)
            .ToList();
    }

    public async Task<FeesOverviewResult> GetOverviewAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var sessionId =
            await GetCurrentSessionIdAsync(
                tenantId,
                cancellationToken);

        await ValidateTermAsync(
            tenantId,
            sessionId,
            academicTermId,
            cancellationToken);

        var charges =
            await _database.StudentFeeCharges
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicTermId ==
                        academicTermId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.StudentId,
                    x.Amount,
                    x.AmountPaid
                })
                .ToListAsync(
                    cancellationToken);

        var totalBilled =
            charges.Sum(x =>
                x.Amount);

        var allocatedPaid =
            charges.Sum(x =>
                x.AmountPaid);

        var studentAccountCount =
            charges
                .Select(x =>
                    x.StudentId)
                .Distinct()
                .Count();

        var balancesByStudent =
            charges
                .GroupBy(x =>
                    x.StudentId)
                .Select(group => new
                {
                    StudentId =
                        group.Key,

                    Total =
                        group.Sum(x =>
                            x.Amount),

                    Paid =
                        group.Sum(x =>
                            x.AmountPaid),

                    Balance =
                        group.Sum(x =>
                            x.Amount -
                            x.AmountPaid)
                })
                .ToList();

        var studentsOwing =
            balancesByStudent.Count(x =>
                x.Balance > 0m);

        var fullyPaidStudents =
            balancesByStudent.Count(x =>
                x.Total > 0m &&
                x.Balance <= 0m);

        var activePayments =
            await _database.FeePayments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicTermId ==
                        academicTermId &&
                    !x.IsReversed)
                .Select(x => new
                {
                    x.Id,
                    x.StudentId,
                    x.Amount,
                    x.PaymentMethod,
                    x.ReceiptNumber,
                    x.Reference,
                    x.CreatedAtUtc
                })
                .ToListAsync(
                    cancellationToken);

        var totalCollected =
            activePayments.Sum(x =>
                x.Amount);

        var creditBalance =
            Math.Max(
                totalCollected -
                allocatedPaid,
                0m);

        var totalOutstanding =
            Math.Max(
                totalBilled -
                allocatedPaid,
                0m);

        var collectionRate =
            totalBilled <= 0m
                ? 0m
                : Math.Round(
                    allocatedPaid /
                    totalBilled *
                    100m,
                    1,
                    MidpointRounding.AwayFromZero);

        var topBalanceIds =
            balancesByStudent
                .Where(x =>
                    x.Balance > 0m)
                .OrderByDescending(x =>
                    x.Balance)
                .Take(10)
                .Select(x =>
                    x.StudentId)
                .ToList();

        var recentPayments =
            activePayments
                .OrderByDescending(x =>
                    x.CreatedAtUtc)
                .Take(10)
                .ToList();

        var studentIds =
            topBalanceIds
                .Concat(
                    recentPayments.Select(x =>
                        x.StudentId))
                .Distinct()
                .ToList();

        var studentDetails =
            await _database.Students
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    studentIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.AdmissionNumber,

                    Name =
                        x.MiddleName == null
                            ? x.FirstName +
                              " " +
                              x.LastName
                            : x.FirstName +
                              " " +
                              x.MiddleName +
                              " " +
                              x.LastName
                })
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

        var topOutstanding =
            balancesByStudent
                .Where(x =>
                    x.Balance > 0m)
                .OrderByDescending(x =>
                    x.Balance)
                .Take(10)
                .Select(x =>
                {
                    studentDetails.TryGetValue(
                        x.StudentId,
                        out var student);

                    return new OutstandingStudentResult(
                        x.StudentId,
                        student?.AdmissionNumber ??
                            "",
                        student?.Name ??
                            "Unknown Student",
                        x.Total,
                        x.Paid,
                        x.Balance);
                })
                .ToList();

        var paymentResults =
            recentPayments
                .Select(x =>
                {
                    studentDetails.TryGetValue(
                        x.StudentId,
                        out var student);

                    return new RecentFeePaymentResult(
                        x.Id,
                        x.StudentId,
                        student?.AdmissionNumber ??
                            "",
                        student?.Name ??
                            "Unknown Student",
                        x.Amount,
                        x.PaymentMethod,
                        x.ReceiptNumber,
                        x.Reference,
                        x.CreatedAtUtc);
                })
                .ToList();

        return new FeesOverviewResult(
            academicTermId,
            totalBilled,
            totalCollected,
            totalOutstanding,
            creditBalance,
            collectionRate,
            studentAccountCount,
            studentsOwing,
            fullyPaidStudents,
            topOutstanding,
            paymentResults);
    }

    private async Task<Guid> GetCurrentSessionIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _database.AcademicSessions
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsCurrent &&
                x.IsActive)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(
                cancellationToken)
            is var id && id != Guid.Empty
                ? id
                : throw new InvalidOperationException(
                    "A current academic session is required.");
    }

    private async Task<FeeStructure> EnsureStructureAsync(
        Guid feeStructureId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _database.FeeStructures
            .SingleOrDefaultAsync(x =>
                x.Id == feeStructureId &&
                x.TenantId == tenantId &&
                x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Fee structure was not found.");
    }

    private async Task ValidateTermAsync(
        Guid tenantId,
        Guid sessionId,
        Guid termId,
        CancellationToken cancellationToken)
    {
        var valid =
            await _database.AcademicTerms
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
                "Academic term was not found in the current session.");
        }
    }

    private async Task ValidateAudienceAsync(
        Guid tenantId,
        string audienceType,
        Guid? audienceId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(
                audienceType,
                "School",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!audienceId.HasValue)
        {
            throw new InvalidOperationException(
                "Select an audience.");
        }

        if (string.Equals(
                audienceType,
                "Level",
                StringComparison.OrdinalIgnoreCase))
        {
            var exists =
                await _database.AcademicLevels
                    .AnyAsync(
                        x =>
                            x.Id ==
                                audienceId.Value &&
                            x.TenantId ==
                                tenantId &&
                            x.IsActive,
                        cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(
                    "Academic level was not found.");
            }

            return;
        }

        if (string.Equals(
                audienceType,
                "Class",
                StringComparison.OrdinalIgnoreCase))
        {
            var exists =
                await _database.ClassGroups
                    .AnyAsync(
                        x =>
                            x.Id ==
                                audienceId.Value &&
                            x.TenantId ==
                                tenantId &&
                            x.IsActive,
                        cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(
                    "Class was not found.");
            }

            return;
        }

        throw new InvalidOperationException(
            "Audience must be School, Level or Class.");
    }

    private async Task<FeeStructureResult> GetStructureAsync(
        Guid structureId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var structure =
            await _database.FeeStructures
                .AsNoTracking()
                .Include(x => x.Lines)
                    .ThenInclude(x =>
                        x.FeeItem)
                .SingleAsync(
                    x =>
                        x.Id == structureId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        var audienceName =
            structure.AudienceType;

        if (structure.AudienceType == "School")
        {
            audienceName = "Entire School";
        }
        else if (
            structure.AudienceType == "Level" &&
            structure.AudienceId.HasValue)
        {
            audienceName =
                await _database.AcademicLevels
                    .Where(x =>
                        x.Id ==
                            structure.AudienceId.Value &&
                        x.TenantId ==
                            tenantId)
                    .Select(x => x.Name)
                    .SingleOrDefaultAsync(
                        cancellationToken)
                ?? "Level";
        }
        else if (
            structure.AudienceType == "Class" &&
            structure.AudienceId.HasValue)
        {
            audienceName =
                await _database.ClassGroups
                    .Where(x =>
                        x.Id ==
                            structure.AudienceId.Value &&
                        x.TenantId ==
                            tenantId)
                    .Select(x =>
                        x.AcademicLevel.Name +
                        " — " +
                        x.Name)
                    .SingleOrDefaultAsync(
                        cancellationToken)
                ?? "Class";
        }

        var lines =
            structure.Lines
                .Select(x =>
                    new FeeStructureLineResult(
                        x.Id,
                        x.FeeItemId,
                        x.FeeItem.Name,
                        x.Amount,
                        x.IsRequired))
                .ToList();

        return new FeeStructureResult(
            structure.Id,
            structure.AcademicSessionId,
            structure.AcademicTermId,
            structure.Name,
            structure.AudienceType,
            structure.AudienceId,
            audienceName,
            lines
                .Where(x =>
                    x.IsRequired)
                .Sum(x =>
                    x.Amount),
            lines);
    }

    private static StudentFeeChargeResult ToChargeResult(
        StudentFeeCharge charge,
        string feeItemName,
        string? feeStructureName = null,
        bool? isRequired = null)
    {
        return new StudentFeeChargeResult(
            charge.Id,
            charge.FeeItemId,
            charge.FeeStructureId,
            charge.FeeStructureLineId,
            feeStructureName,
            feeItemName,
            charge.Description,
            charge.Amount,
            charge.AmountPaid,
            charge.Balance,
            charge.Balance <= 0m,
            isRequired);
    }
}
