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
                        x.AdmissionNumber))
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

        var enrollmentQuery =
            _database.StudentEnrollments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId ==
                        structure.AcademicSessionId &&
                    x.IsCurrent &&
                    x.IsActive &&
                    x.Student.IsActive);

        if (structure.AudienceType == "Level")
        {
            enrollmentQuery =
                enrollmentQuery.Where(x =>
                    x.ClassGroup.AcademicLevelId ==
                        structure.AudienceId);
        }
        else if (
            structure.AudienceType == "Class")
        {
            enrollmentQuery =
                enrollmentQuery.Where(x =>
                    x.ClassGroupId ==
                        structure.AudienceId);
        }

        var studentIds =
            await enrollmentQuery
                .Select(x => x.StudentId)
                .Distinct()
                .ToListAsync(
                    cancellationToken);

        var requiredLines =
            structure.Lines
                .Where(x =>
                    x.IsRequired)
                .ToList();

        var created = 0;
        decimal total = 0m;

        foreach (var studentId in studentIds)
        {
            foreach (var line in requiredLines)
            {
                var exists =
                    await _database.StudentFeeCharges
                        .AnyAsync(
                            x =>
                                x.TenantId == tenantId &&
                                x.StudentId == studentId &&
                                x.AcademicTermId ==
                                    structure.AcademicTermId &&
                                x.FeeStructureLineId ==
                                    line.Id &&
                                x.IsActive,
                            cancellationToken);

                if (exists)
                {
                    continue;
                }

                _database.StudentFeeCharges.Add(
                    new StudentFeeCharge(
                        tenantId,
                        studentId,
                        structure.AcademicSessionId,
                        structure.AcademicTermId,
                        line.FeeItemId,
                        structure.Id,
                        line.Id,
                        line.FeeItem.Name,
                        line.Amount));

                created++;
                total += line.Amount;
            }
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        return new GenerateChargesResult(
            structure.Id,
            studentIds.Count,
            created,
            total);
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

        return new StudentFeeAccountResult(
            student.Id,
            student.AdmissionNumber,
            student.Name,
            academicTermId,
            totalCharges,
            applied,
            outstanding,
            credit,
            charges
                .Select(x =>
                    ToChargeResult(
                        x,
                        x.FeeItem.Name))
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
                        x.CreatedAtUtc);
                })
                .ToList());
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
            $"ARC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

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
            payment.CreatedAtUtc);
    }

    public async Task ReversePaymentAsync(
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
                        x.TenantId == tenantId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Payment was not found.");

        if (payment.IsReversed)
        {
            throw new InvalidOperationException(
                "Payment has already been reversed.");
        }

        foreach (var allocation in payment.Allocations)
        {
            allocation.StudentFeeCharge
                .ReversePayment(
                    allocation.Amount);
        }

        payment.Reverse(
            request.Reason);

        await _database.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutstandingStudentResult>>
        GetOutstandingAsync(
            Guid academicTermId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var rows =
            await _database.StudentFeeCharges
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicTermId ==
                        academicTermId &&
                    x.IsActive)
                .GroupBy(x => new
                {
                    x.StudentId,
                    x.Student.AdmissionNumber,
                    x.Student.FirstName,
                    x.Student.LastName
                })
                .Select(group =>
                    new OutstandingStudentResult(
                        group.Key.StudentId,
                        group.Key.AdmissionNumber,
                        group.Key.FirstName +
                            " " +
                            group.Key.LastName,
                        group.Sum(x =>
                            x.Amount),
                        group.Sum(x =>
                            x.AmountPaid),
                        group.Sum(x =>
                            x.Amount -
                            x.AmountPaid)))
                .Where(x =>
                    x.OutstandingBalance > 0)
                .OrderByDescending(x =>
                    x.OutstandingBalance)
                .ToListAsync(
                    cancellationToken);

        return rows;
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
        string feeItemName)
    {
        return new StudentFeeChargeResult(
            charge.Id,
            charge.FeeItemId,
            feeItemName,
            charge.Description,
            charge.Amount,
            charge.AmountPaid,
            charge.Balance,
            charge.Balance <= 0m);
    }
}
