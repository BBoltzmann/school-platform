using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Fees;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Fees;
using SchoolPlatform.Domain.Students;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Infrastructure.Fees;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class FeeStructureStudentAssignmentTests
{
    [PostgresTimetableFact]
    public async Task CreditCarryForwardTransfersUnallocatedOverpaymentExactlyOnce()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var sourceSession = new AcademicSession(tenant.Id, "2027/2028-credit", new(2027, 9, 1), new(2028, 7, 1), true);
        var sourceTerm = new AcademicTerm(tenant.Id, sourceSession.Id, "First Term", new(2027, 9, 1), new(2027, 12, 20), 1);
        var targetSession = new AcademicSession(tenant.Id, "2028/2029-credit", new(2028, 9, 1), new(2029, 7, 1), false);
        var targetTerm = new AcademicTerm(tenant.Id, targetSession.Id, "First Term", new(2028, 9, 1), new(2028, 12, 20), 1);
        var student = NewStudent(tenant.Id, "CREDIT-1", "Credit");
        var item = new FeeItem(tenant.Id, "Credit tuition", "CREDIT-TUI", null);
        var charge = new StudentFeeCharge(tenant.Id, student.Id, sourceSession.Id, sourceTerm.Id, item.Id, null, null, "Tuition", 100000m);
        database.AddRange(sourceSession, sourceTerm, targetSession, targetTerm, student, item, charge);
        await database.SaveChangesAsync();

        var payment = new FeePayment(tenant.Id, student.Id, sourceSession.Id, sourceTerm.Id, 120000m, "Cash", "CREDIT-RECEIPT", null, "Overpayment test");
        var allocation = new FeePaymentAllocation(tenant.Id, payment.Id, charge.Id, 100000m);
        charge.ApplyPayment(100000m);
        database.AddRange(payment, allocation);
        await database.SaveChangesAsync();

        var finance = new FinanceEnhancementsService(database, new FixedTenantContext(tenant.Id), new FixedUserContext());
        var runRequest = new CarryForwardRequest(sourceSession.Id, sourceTerm.Id, targetSession.Id, targetTerm.Id, "credit-overpayment-run");
        var preview = await finance.PreviewCarryForwardAsync(runRequest);
        Assert.Equal(1, preview.CreditStudents);
        Assert.Equal(20000m, preview.CreditAmount);

        var result = await finance.CarryForwardAsync(runRequest);
        Assert.False(result.AlreadyApplied);
        var entry = await database.CarryForwardEntries.SingleAsync(x => x.CarryForwardRunId == result.RunId);
        Assert.False(entry.IsDebit);
        Assert.Equal(20000m, entry.Amount);
        Assert.NotNull(entry.SourceAdjustmentId);
        Assert.NotNull(entry.TargetAdjustmentId);

        var sourceAdjustments = await database.StudentFeeAdjustments.Where(x => x.Id == entry.SourceAdjustmentId).ToListAsync();
        var targetAdjustments = await database.StudentFeeAdjustments.Where(x => x.Id == entry.TargetAdjustmentId).ToListAsync();
        Assert.Equal(FinancialAdjustmentType.CarryForwardTransferOutDebit, sourceAdjustments.Single().Type);
        Assert.Equal(FinancialAdjustmentType.CarryForwardCredit, targetAdjustments.Single().Type);
        Assert.Equal(20000m, sourceAdjustments.Single().Amount);
        Assert.Equal(20000m, targetAdjustments.Single().Amount);

        var repeated = await finance.CarryForwardAsync(runRequest);
        Assert.True(repeated.AlreadyApplied);
        Assert.Equal(1, await database.CarryForwardEntries.CountAsync(x => x.CarryForwardRunId == result.RunId));
        Assert.Equal(1, await database.StudentFeeAdjustments.CountAsync(x => x.CarryForwardEntryId == entry.Id && x.Type == FinancialAdjustmentType.CarryForwardCredit));
    }

    [PostgresTimetableFact]
    public async Task SharedFinanceImplementationIsIsolatedAcrossTwoTenants()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenantA = await database.Tenants.SingleAsync();
        var tenantB = new Tenant("Second finance tenant", "second-finance-tenant");
        var sessionA = new AcademicSession(tenantA.Id, "2027/2028-A", new(2027, 9, 1), new(2028, 7, 1), true);
        var termA = new AcademicTerm(tenantA.Id, sessionA.Id, "First Term", new(2027, 9, 1), new(2027, 12, 20), 1);
        var sessionB = new AcademicSession(tenantB.Id, "2027/2028-B", new(2027, 9, 1), new(2028, 7, 1), true);
        var termB = new AcademicTerm(tenantB.Id, sessionB.Id, "First Term", new(2027, 9, 1), new(2027, 12, 20), 1);
        var studentA = NewStudent(tenantA.Id, "TENANT-A-1", "Tenant A");
        var studentB = NewStudent(tenantB.Id, "TENANT-B-1", "Tenant B");
        var itemA = new FeeItem(tenantA.Id, "Tuition A", "TUI-A", null);
        var itemB = new FeeItem(tenantB.Id, "Tuition B", "TUI-B", null);
        database.AddRange(tenantB, sessionA, termA, sessionB, termB, studentA, studentB, itemA, itemB,
            new StudentFeeCharge(tenantA.Id, studentA.Id, sessionA.Id, termA.Id, itemA.Id, null, null, "Tuition", 20000m),
            new StudentFeeCharge(tenantB.Id, studentB.Id, sessionB.Id, termB.Id, itemB.Id, null, null, "Tuition", 30000m));
        await database.SaveChangesAsync();

        var serviceA = new FinanceEnhancementsService(database, new FixedTenantContext(tenantA.Id), new FixedUserContext());
        var serviceB = new FinanceEnhancementsService(database, new FixedTenantContext(tenantB.Id), new FixedUserContext());
        var definitionA = await serviceA.CreateDiscountAsync(new CreateDiscountDefinitionRequest("Staff Discount", null, 1000m));
        var definitionB = await serviceB.CreateDiscountAsync(new CreateDiscountDefinitionRequest("Staff Discount", null, 2000m));
        Assert.Single(await serviceA.GetDiscountsAsync());
        Assert.Single(await serviceB.GetDiscountsAsync());
        Assert.NotEqual(definitionA.Id, definitionB.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceA.PreviewDiscountAsync(new ApplyDiscountRequest(definitionA.Id, sessionA.Id, termA.Id, "SelectedStudents", null, null, [studentB.Id], null, "tenant-a-forged")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceA.CreateAdjustmentAsync(new CreateAdjustmentRequest(studentB.Id, sessionA.Id, termA.Id, "OpeningDebit", 500m, "Forged adjustment", null)));

        await serviceA.ApplyDiscountAsync(new ApplyDiscountRequest(definitionA.Id, sessionA.Id, termA.Id, "SelectedStudents", null, null, [studentA.Id], null, "same-logical-key"));
        await serviceB.ApplyDiscountAsync(new ApplyDiscountRequest(definitionB.Id, sessionB.Id, termB.Id, "SelectedStudents", null, null, [studentB.Id], null, "same-logical-key"));
        Assert.Equal(1, await database.StudentDiscountAssignments.CountAsync(x => x.TenantId == tenantA.Id));
        Assert.Equal(1, await database.StudentDiscountAssignments.CountAsync(x => x.TenantId == tenantB.Id));

        var carry = await serviceA.CarryForwardAsync(new CarryForwardRequest(sessionA.Id, termA.Id, sessionA.Id, null, "shared-carry-key"));
        Assert.True(carry.StudentsProcessed >= 1);
        Assert.Equal(0, await database.CarryForwardEntries.CountAsync(x => x.TenantId == tenantB.Id));
        var tenantBBalance = await new FeesService(database, new FixedTenantContext(tenantB.Id)).GetStudentAccountAsync(studentB.Id, termB.Id);
        Assert.Equal(30000m, tenantBBalance.TotalCharges);
    }
    [PostgresTimetableFact]
    public async Task SharedFinanceEnhancementsApplyDiscountsAdjustmentsAndCarryForwardIdempotently()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(tenant.Id, "2027/2028", new(2027, 9, 1), new(2028, 7, 1), true);
        var sourceTerm = new AcademicTerm(tenant.Id, session.Id, "First Term", new(2027, 9, 1), new(2027, 12, 20), 1);
        var targetSession = new AcademicSession(tenant.Id, "2028/2029", new(2028, 9, 1), new(2029, 7, 1), false);
        var targetTerm = new AcademicTerm(tenant.Id, targetSession.Id, "First Term", new(2028, 9, 1), new(2028, 12, 20), 1);
        var campus = new Campus(tenant.Id, "Main campus");
        var level = new AcademicLevel(tenant.Id, "SS1", "Senior", 1);
        var classGroup = new ClassGroup(tenant.Id, campus.Id, level.Id, "SS1 A");
        var student = NewStudent(tenant.Id, "FIN-1", "Finance");
        database.AddRange(session, sourceTerm, targetSession, targetTerm, campus, level, classGroup, student, new StudentEnrollment(tenant.Id, student.Id, session.Id, level.Id, classGroup.Id, new(2027, 9, 1), true));
        var item = new FeeItem(tenant.Id, "Tuition", "TUI-FIN", null);
        database.Add(item);
        database.Add(new StudentFeeCharge(tenant.Id, student.Id, session.Id, sourceTerm.Id, item.Id, null, null, "Tuition", 150000m));
        await database.SaveChangesAsync();

        var finance = new FinanceEnhancementsService(database, new FixedTenantContext(tenant.Id), new FixedUserContext());
        var definition = await finance.CreateDiscountAsync(new CreateDiscountDefinitionRequest("Staff Discount", null, 20000m));
        var preview = await finance.PreviewDiscountAsync(new ApplyDiscountRequest(definition.Id, session.Id, sourceTerm.Id, "SelectedStudents", null, null, [student.Id], null));
        Assert.Equal(1, preview.StudentCount);
        await finance.ApplyDiscountAsync(new ApplyDiscountRequest(definition.Id, session.Id, sourceTerm.Id, "SelectedStudents", null, null, [student.Id], null));
        await finance.CreateAdjustmentAsync(new CreateAdjustmentRequest(student.Id, session.Id, sourceTerm.Id, "OpeningCredit", 10000m, "Opening credit", "Prior term"));
        var account = await new FeesService(database, new FixedTenantContext(tenant.Id)).GetStudentAccountAsync(student.Id, sourceTerm.Id);
        Assert.Equal(20000m, account.Discounts);
        Assert.Equal(10000m, account.CreditAdjustments);
        Assert.Equal(120000m, account.OutstandingBalance);
        Assert.Equal(120000m, (await new FeesService(database, new FixedTenantContext(tenant.Id)).GetOutstandingAsync(sourceTerm.Id)).Single(x => x.StudentId == student.Id).OutstandingBalance);

        var carry = new CarryForwardRequest(session.Id, sourceTerm.Id, targetSession.Id, targetTerm.Id, "finance-test-run");
        var carryResult = await finance.CarryForwardAsync(carry);
        Assert.False(carryResult.AlreadyApplied);
        var repeated = await finance.CarryForwardAsync(carry);
        Assert.True(repeated.AlreadyApplied);
        Assert.Equal(carryResult.RunId, repeated.RunId);
        Assert.Equal(1, await database.CarryForwardEntries.CountAsync(x => x.TenantId == tenant.Id));
        Assert.Equal(120000m, await database.StudentFeeAdjustments.Where(x => x.TenantId == tenant.Id && x.AcademicSessionId == targetSession.Id).SumAsync(x => x.Amount));
    }
    [PostgresTimetableFact]
    public async Task ZeroAssignmentsReconcileHistoricalChargesAndProtectPaidCharges()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(tenant.Id, "2026/2027", new(2026, 9, 1), new(2027, 7, 1), true);
        var term = new AcademicTerm(tenant.Id, session.Id, "First Term", new(2026, 9, 1), new(2026, 12, 20), 1);
        var campus = new Campus(tenant.Id, "Main campus");
        var level = new AcademicLevel(tenant.Id, "SSS", "Senior", 1);
        var classGroup = new ClassGroup(tenant.Id, campus.Id, level.Id, "SS3");
        var student = NewStudent(tenant.Id, "ZERO-1", "Michael");
        var enrollment = new StudentEnrollment(tenant.Id, student.Id, session.Id, level.Id, classGroup.Id, new(2026, 9, 1), true);
        var items = new[]
        {
            new FeeItem(tenant.Id, "School Practicals", "PRACTICALS", null),
            new FeeItem(tenant.Id, "Half Yearly Tuition", "TUITION", null),
            new FeeItem(tenant.Id, "Half Year Lesson", "LESSON", null)
        };
        var structure = new FeeStructure(tenant.Id, session.Id, term.Id, "SS3 1ST HALF YEAR ART/COMM", "Class", classGroup.Id);
        var protectedStructure = new FeeStructure(tenant.Id, session.Id, term.Id, "Protected historical structure", "Class", classGroup.Id);

        database.AddRange(session, term, campus, level, classGroup, student, enrollment, structure, protectedStructure);
        database.AddRange(items);
        database.AddRange(
            new FeeStructureLine(tenant.Id, structure.Id, items[0].Id, 25000m, true),
            new FeeStructureLine(tenant.Id, structure.Id, items[1].Id, 97500m, true),
            new FeeStructureLine(tenant.Id, structure.Id, items[2].Id, 18000m, true),
            new FeeStructureLine(tenant.Id, protectedStructure.Id, items[0].Id, 25000m, true));
        await database.SaveChangesAsync();

        var service = new FeesService(database, new FixedTenantContext(tenant.Id));
        await service.ReplaceAssignedStudentsAsync(structure.Id, new([student.Id]));
        var generated = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(1, generated.StudentCount);
        Assert.Equal(3, generated.ChargesCreated);
        Assert.Equal(3, await database.StudentFeeCharges.CountAsync(x => x.FeeStructureId == structure.Id && x.IsActive));

        await service.RemoveStudentAssignmentAsync(structure.Id, student.Id);
        Assert.Empty(await service.GetAssignedStudentsAsync(structure.Id));
        var beforeSync = await service.GetStudentAccountAsync(student.Id, term.Id);
        Assert.Equal(3, beforeSync.Charges.Count);

        var synchronizedUnpaid = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(0, synchronizedUnpaid.StudentCount);
        Assert.Equal(0, synchronizedUnpaid.ChargesCreated);
        Assert.Equal(3, synchronizedUnpaid.DeactivatedCount);
        Assert.Equal(0, synchronizedUnpaid.ProtectedCount);

        var accountAfterUnpaidSync = await service.GetStudentAccountAsync(student.Id, term.Id);
        Assert.Empty(accountAfterUnpaidSync.Charges);
        Assert.Equal(0m, accountAfterUnpaidSync.OutstandingBalance);

        var outstanding = await service.GetOutstandingAsync(term.Id);
        Assert.Empty(outstanding);
        var overview = await service.GetOverviewAsync(term.Id);
        Assert.Equal(0m, overview.TotalBilled);
        Assert.Equal(0m, overview.TotalCollected);
        Assert.Equal(0m, overview.TotalOutstanding);

        await service.ReplaceAssignedStudentsAsync(structure.Id, new([student.Id]));
        var reactivated = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(0, reactivated.ChargesCreated);
        Assert.Equal(3, reactivated.ReactivatedCount);
        Assert.Equal(3, await database.StudentFeeCharges.CountAsync(x => x.FeeStructureId == structure.Id && x.IsActive));
        await service.RemoveStudentAssignmentAsync(structure.Id, student.Id);
        var secondEmptySync = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(3, secondEmptySync.DeactivatedCount);

        var additionalStudents = Enumerable.Range(2, 6)
            .Select(index => NewStudent(tenant.Id, $"ZERO-{index}", $"Student {index}"))
            .ToArray();
        var additionalEnrollments = additionalStudents.Select(currentStudent => new StudentEnrollment(
            tenant.Id,
            currentStudent.Id,
            session.Id,
            level.Id,
            classGroup.Id,
            new(2026, 9, 1),
            true));
        database.AddRange(additionalStudents);
        database.AddRange(additionalEnrollments);
        await database.SaveChangesAsync();

        var sevenStudentIds = additionalStudents.Select(currentStudent => currentStudent.Id)
            .Append(student.Id)
            .ToArray();
        await service.ReplaceAssignedStudentsAsync(structure.Id, new(sevenStudentIds));
        var sevenStudentSync = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(7, sevenStudentSync.StudentCount);
        Assert.Equal(18, sevenStudentSync.ChargesCreated);
        Assert.Equal(3, sevenStudentSync.ReactivatedCount);
        Assert.Equal(21, await database.StudentFeeCharges.CountAsync(
            x => x.FeeStructureId == structure.Id && x.IsActive));

        await service.RemoveStudentAssignmentAsync(structure.Id, student.Id);
        await service.GenerateChargesAsync(structure.Id);

        await service.ReplaceAssignedStudentsAsync(protectedStructure.Id, new([student.Id]));
        await service.GenerateChargesAsync(protectedStructure.Id);
        var paidAmount = await service.RecordPaymentAsync(
            student.Id,
            new RecordFeePaymentRequest(term.Id, 25000m, "Cash", "ZERO-RECEIPT", null));
        Assert.Equal(25000m, paidAmount.AllocatedAmount);
        await service.RemoveStudentAssignmentAsync(protectedStructure.Id, student.Id);

        var synchronized = await service.GenerateChargesAsync(protectedStructure.Id);
        Assert.Equal(0, synchronized.StudentCount);
        Assert.Equal(0, synchronized.ChargesCreated);
        Assert.Equal(0, synchronized.DeactivatedCount);
        Assert.Equal(1, synchronized.ProtectedCount);
        var protectedAccount = await service.GetStudentAccountAsync(student.Id, term.Id);
        Assert.Single(protectedAccount.Charges);
        Assert.Equal(25000m, protectedAccount.AppliedPayments);

        var termReconciled = await service.ReconcileTermFeesAsync(term.Id);
        Assert.Equal(2, termReconciled.StructuresChecked);
        Assert.Equal(0, termReconciled.ChargesCreated);
        Assert.Equal(0, termReconciled.ChargesDeactivated);
        Assert.Equal(1, termReconciled.ProtectedCharges);
        var repeatedTermReconciled = await service.ReconcileTermFeesAsync(term.Id);
        Assert.Equal(0, repeatedTermReconciled.ChargesCreated);
        Assert.Equal(0, repeatedTermReconciled.ChargesDeactivated);
    }

    [PostgresTimetableFact]
    public async Task AssignmentsDriveExplicitGenerationAndPreserveHistory()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(
            tenant.Id,
            "2026/2027",
            new(2026, 9, 1),
            new(2027, 7, 1),
            true);
        var term = new AcademicTerm(
            tenant.Id,
            session.Id,
            "First Term",
            new(2026, 9, 1),
            new(2026, 12, 20),
            1);
        var campus = new Campus(tenant.Id, "Main campus");
        var level = new AcademicLevel(tenant.Id, "JSS", "Junior", 1);
        var classGroup = new ClassGroup(tenant.Id, campus.Id, level.Id, "JSS 2");
        var students = new[]
        {
            NewStudent(tenant.Id, "ASSIGN-1", "Ada"),
            NewStudent(tenant.Id, "ASSIGN-2", "Bola"),
            NewStudent(tenant.Id, "ASSIGN-3", "Chidi")
        };
        var enrollments = students.Select(student => new StudentEnrollment(
            tenant.Id,
            student.Id,
            session.Id,
            level.Id,
            classGroup.Id,
            new(2026, 9, 1),
            true));
        var tuition = new FeeItem(tenant.Id, "Tuition", "TUI", null);
        var transport = new FeeItem(tenant.Id, "Transport", "TRN", null);
        var assignedStructure = new FeeStructure(
            tenant.Id,
            session.Id,
            term.Id,
            "Selected students",
            "Class",
            classGroup.Id);
        var secondStructure = new FeeStructure(
            tenant.Id,
            session.Id,
            term.Id,
            "Transport students",
            "School",
            null);
        var legacyStructure = new FeeStructure(
            tenant.Id,
            session.Id,
            term.Id,
            "Legacy class structure",
            "Class",
            classGroup.Id);

        database.AddRange(
            session,
            term,
            campus,
            level,
            classGroup,
            tuition,
            transport,
            assignedStructure,
            secondStructure,
            legacyStructure);
        database.AddRange(students);
        database.AddRange(enrollments);
        database.AddRange(
            new FeeStructureLine(tenant.Id, assignedStructure.Id, tuition.Id, 100m, true),
            new FeeStructureLine(tenant.Id, secondStructure.Id, transport.Id, 25m, true),
            new FeeStructureLine(tenant.Id, legacyStructure.Id, tuition.Id, 90m, true));
        await database.SaveChangesAsync();

        var service = new FeesService(
            database,
            new FixedTenantContext(tenant.Id));

        var assigned = await service.ReplaceAssignedStudentsAsync(
            assignedStructure.Id,
            new([students[0].Id, students[1].Id]));
        Assert.Equal(2, assigned.Count);

        var idempotent = await service.ReplaceAssignedStudentsAsync(
            assignedStructure.Id,
            new([students[0].Id, students[1].Id]));
        Assert.Equal(2, idempotent.Count);

        await service.ReplaceAssignedStudentsAsync(
            secondStructure.Id,
            new([students[0].Id]));

        var generated = await service.GenerateChargesAsync(assignedStructure.Id);
        Assert.Equal(2, generated.StudentCount);
        Assert.Equal(3, generated.ChargesCreated);

        var assignedAccount = await service.GetStudentAccountAsync(
            students[0].Id,
            term.Id);
        Assert.Contains(assignedAccount.Charges, x =>
            x.FeeItemName == "Tuition" &&
            x.FeeStructureId == assignedStructure.Id &&
            x.FeeStructureName == assignedStructure.Name);
        Assert.Contains(assignedAccount.Charges, x =>
            x.FeeItemName == "Transport" &&
            x.FeeStructureId == secondStructure.Id &&
            x.FeeStructureName == secondStructure.Name);

        await service.CreateStudentChargeAsync(
            students[0].Id,
            new CreateStudentChargeRequest(
                term.Id,
                tuition.Id,
                "Manual adjustment",
                5m));
        var accountWithManualCharge = await service.GetStudentAccountAsync(
            students[0].Id,
            term.Id);
        var manualCharge = Assert.Single(accountWithManualCharge.Charges.Where(x => x.Description == "Manual adjustment"));
        Assert.Null(manualCharge.FeeStructureId);
        Assert.Null(manualCharge.FeeStructureName);

        var repeated = await service.GenerateChargesAsync(assignedStructure.Id);
        Assert.Equal(0, repeated.ChargesCreated);

        await service.RemoveStudentAssignmentAsync(
            assignedStructure.Id,
            students[1].Id);
        var synchronized = await service.GenerateChargesAsync(assignedStructure.Id);
        Assert.Equal(1, synchronized.DeactivatedCount);
        Assert.Equal(1, await database.StudentFeeCharges.CountAsync(
            x => x.FeeStructureId == assignedStructure.Id && x.IsActive));

        var secondGenerated = await service.GenerateChargesAsync(secondStructure.Id);
        Assert.Equal(1, secondGenerated.StudentCount);
        Assert.Equal(0, secondGenerated.ChargesCreated);

        var legacyGenerated = await service.GenerateChargesAsync(legacyStructure.Id);
        Assert.Equal(0, legacyGenerated.StudentCount);
        Assert.Equal(0, legacyGenerated.ChargesCreated);
        Assert.Equal(0, await database.StudentFeeCharges.CountAsync(
            x => x.FeeStructureId == legacyStructure.Id && x.IsActive));

        await service.ReplaceAssignedStudentsAsync(
            legacyStructure.Id,
            new([students[0].Id]));
        var legacyAssigned = await service.GenerateChargesAsync(legacyStructure.Id);
        Assert.Equal(1, legacyAssigned.ChargesCreated);
        var accountWithTwoTuitionStructures = await service.GetStudentAccountAsync(
            students[0].Id,
            term.Id);
        var tuitionSources = accountWithTwoTuitionStructures.Charges
            .Where(x => x.FeeItemName == "Tuition" && x.FeeStructureId.HasValue)
            .Select(x => x.FeeStructureName)
            .ToHashSet();
        Assert.Contains(assignedStructure.Name, tuitionSources);
        Assert.Contains(legacyStructure.Name, tuitionSources);

        var repeatedLegacy = await service.GenerateChargesAsync(legacyStructure.Id);
        Assert.Equal(0, repeatedLegacy.ChargesCreated);
        Assert.Equal(0, repeatedLegacy.DeactivatedCount);

        await service.RemoveStudentAssignmentAsync(
            assignedStructure.Id,
            students[0].Id);
        Assert.Empty(await service.GetAssignedStudentsAsync(assignedStructure.Id));
        var emptySync = await service.GenerateChargesAsync(assignedStructure.Id);
        Assert.Equal(0, emptySync.StudentCount);
        Assert.Equal(0, emptySync.ChargesCreated);
        Assert.Equal(1, emptySync.DeactivatedCount);
        Assert.Equal(0, await database.StudentFeeCharges.CountAsync(
            x => x.FeeStructureId == assignedStructure.Id && x.IsActive));

        var assignedLine = await database.FeeStructureLines
            .SingleAsync(x => x.FeeStructureId == assignedStructure.Id);
        var updated = await service.UpdateStructureAsync(
            assignedStructure.Id,
            new UpdateFeeStructureRequest(
                "Selected students updated",
                "Class",
                classGroup.Id,
                new[]
                {
                    new UpdateFeeStructureLineRequest(
                        assignedLine.Id,
                        tuition.Id,
                        120m,
                        true)
                }));

        Assert.Equal("Selected students updated", updated.Name);
        Assert.Equal(120m, updated.TotalRequiredAmount);
        var historicalAssignedCharges = await database.StudentFeeCharges
            .Where(x => x.FeeStructureId == assignedStructure.Id)
            .ToListAsync();
        Assert.Equal(2, historicalAssignedCharges.Count);
        Assert.All(historicalAssignedCharges, charge =>
        {
            Assert.Equal(100m, charge.Amount);
            Assert.False(charge.IsActive);
        });

        var cleared = await service.ReplaceAssignedStudentsAsync(
            assignedStructure.Id,
            new(Array.Empty<Guid>()));
        Assert.Empty(cleared);
    }

    [PostgresTimetableFact]
    public async Task OptionalComponentIsolatedToTheSelectedStudent()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(tenant.Id, "2026/2027", new(2026, 9, 1), new(2027, 7, 1), true);
        var term = new AcademicTerm(tenant.Id, session.Id, "First Term", new(2026, 9, 1), new(2026, 12, 20), 1);
        var campus = new Campus(tenant.Id, "Main campus");
        var level = new AcademicLevel(tenant.Id, "JSS", "Junior", 1);
        var classGroup = new ClassGroup(tenant.Id, campus.Id, level.Id, "JSS 1");
        var students = Enumerable.Range(1, 3).Select(index => NewStudent(tenant.Id, $"ISOLATE-{index}", $"Student {index}")).ToArray();
        var enrollments = students.Select(student => new StudentEnrollment(tenant.Id, student.Id, session.Id, level.Id, classGroup.Id, new(2026, 9, 1), true));
        var tuition = new FeeItem(tenant.Id, "Tuition", "TUI", null);
        var books = new FeeItem(tenant.Id, "Books", "BOOKS", null);
        var structure = new FeeStructure(tenant.Id, session.Id, term.Id, "JSS 1 Standard", "Class", classGroup.Id);
        database.AddRange(session, term, campus, level, classGroup, structure, tuition, books);
        database.AddRange(students);
        database.AddRange(enrollments);
        var tuitionLine = new FeeStructureLine(tenant.Id, structure.Id, tuition.Id, 60000m, true);
        var booksLine = new FeeStructureLine(tenant.Id, structure.Id, books.Id, 20000m, false);
        database.AddRange(tuitionLine, booksLine);
        await database.SaveChangesAsync();
        var service = new FeesService(database, new FixedTenantContext(tenant.Id));
        await service.ReplaceAssignedStudentsAsync(structure.Id, new(students.Select(x => x.Id).ToArray()));
        var generated = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(3, generated.ChargesCreated);
        Assert.Equal(0, await database.StudentFeeCharges.CountAsync(x => x.FeeStructureLineId == booksLine.Id && x.IsActive));
        await service.AddOptionalFeeComponentAsync(students[0].Id, new(term.Id, booksLine.Id, 25000m));
        foreach (var student in students)
        {
            var account = await service.GetStudentAccountAsync(student.Id, term.Id);
            Assert.Equal(student.Id == students[0].Id ? 1 : 0, account.Charges.Count(x => x.FeeStructureLineId == booksLine.Id));
        }
        await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(1, await database.StudentFeeCharges.CountAsync(x => x.FeeStructureLineId == booksLine.Id && x.IsActive));
        await service.RemoveStudentChargeAsync(students[0].Id, await database.StudentFeeCharges.Where(x => x.FeeStructureLineId == booksLine.Id).Select(x => x.Id).SingleAsync());
        await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(0, await database.StudentFeeCharges.CountAsync(x => x.FeeStructureLineId == booksLine.Id && x.IsActive));
    }

    [PostgresTimetableFact]
    public async Task OptionalComponentKeepsCustomAmountAndPaymentAllocation()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(tenant.Id, "2026/2027", new(2026, 9, 1), new(2027, 7, 1), true);
        var term = new AcademicTerm(tenant.Id, session.Id, "First Term", new(2026, 9, 1), new(2026, 12, 20), 1);
        var campus = new Campus(tenant.Id, "Main campus");
        var level = new AcademicLevel(tenant.Id, "JSS", "Junior", 1);
        var classGroup = new ClassGroup(tenant.Id, campus.Id, level.Id, "JSS 2");
        var student = NewStudent(tenant.Id, "OPTIONAL-1", "Optional");
        var enrollment = new StudentEnrollment(tenant.Id, student.Id, session.Id, level.Id, classGroup.Id, new(2026, 9, 1), true);
        var tuition = new FeeItem(tenant.Id, "Tuition", "TUI", null);
        var books = new FeeItem(tenant.Id, "Books", "BOOKS", null);
        var structure = new FeeStructure(tenant.Id, session.Id, term.Id, "Returning JSS 2", "Class", classGroup.Id);
        database.AddRange(session, term, campus, level, classGroup, student, enrollment, tuition, books, structure);
        var requiredLine = new FeeStructureLine(tenant.Id, structure.Id, tuition.Id, 60000m, true);
        var optionalLine = new FeeStructureLine(tenant.Id, structure.Id, books.Id, 20000m, false);
        database.AddRange(requiredLine, optionalLine);
        await database.SaveChangesAsync();
        var service = new FeesService(database, new FixedTenantContext(tenant.Id));
        await service.ReplaceAssignedStudentsAsync(structure.Id, new([student.Id]));
        var generated = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(1, generated.ChargesCreated);
        Assert.Single(await database.StudentFeeCharges.Where(x => x.FeeStructureLineId == requiredLine.Id && x.IsActive).ToListAsync());
        Assert.Empty(await database.StudentFeeCharges.Where(x => x.FeeStructureLineId == optionalLine.Id && x.IsActive).ToListAsync());
        var eligible = await service.GetOptionalFeeComponentsAsync(student.Id, term.Id);
        Assert.Single(eligible);
        Assert.Equal(20000m, eligible.Single().TemplateAmount);
        var added = await service.AddOptionalFeeComponentAsync(student.Id, new(term.Id, optionalLine.Id, 25000m));
        Assert.Equal(25000m, added.Amount);
        var synced = await service.GenerateChargesAsync(structure.Id);
        Assert.Equal(0, synced.ChargesCreated);
        Assert.Single(await database.StudentFeeCharges.Where(x => x.FeeStructureLineId == optionalLine.Id && x.IsActive).ToListAsync());
        var payment = await service.RecordPaymentAsync(student.Id, new(term.Id, 85000m, "Cash", "OPTIONAL-RECEIPT", null));
        Assert.Equal(85000m, payment.AllocatedAmount);
        var account = await service.GetStudentAccountAsync(student.Id, term.Id);
        Assert.Equal(0m, account.OutstandingBalance);
        Assert.Equal(0m, account.CreditBalance);
        var receipt = account.Payments.Single(x => x.ReceiptNumber == payment.ReceiptNumber);
        Assert.Equal(2, receipt.Allocations!.Count);
        Assert.Contains(receipt.Allocations, x => x.ChargeType == "Optional" && x.AmountAllocated == 25000m);
    }

    [PostgresTimetableFact]
    public async Task OptionalChargeRemovalIgnoresVoidedPaymentAllocationsButBlocksActiveOnes()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(tenant.Id, "2027/2028-removal", new(2027, 9, 1), new(2028, 7, 1), true);
        var term = new AcademicTerm(tenant.Id, session.Id, "First Term", new(2027, 9, 1), new(2027, 12, 20), 1);
        var campus = new Campus(tenant.Id, "Main campus");
        var level = new AcademicLevel(tenant.Id, "JSS", "Junior", 1);
        var classGroup = new ClassGroup(tenant.Id, campus.Id, level.Id, "JSS 1");
        var student = NewStudent(tenant.Id, "REMOVE-OPTIONAL", "Optional");
        var enrollment = new StudentEnrollment(tenant.Id, student.Id, session.Id, level.Id, classGroup.Id, new(2027, 9, 1), true);
        var item = new FeeItem(tenant.Id, "Sports Wear", "SPORTS", null);
        var structure = new FeeStructure(tenant.Id, session.Id, term.Id, "JSS 1 extras", "Class", classGroup.Id);
        var line = new FeeStructureLine(tenant.Id, structure.Id, item.Id, 15000m, false);
        database.AddRange(session, term, campus, level, classGroup, student, enrollment, item, structure, line);
        await database.SaveChangesAsync();
        var service = new FeesService(database, new FixedTenantContext(tenant.Id));
        await service.ReplaceAssignedStudentsAsync(structure.Id, new([student.Id]));
        var charge = await service.AddOptionalFeeComponentAsync(student.Id, new(term.Id, line.Id, 15000m));

        var first = await service.RecordPaymentAsync(student.Id, new(term.Id, 5000m, "Cash", "REMOVE-VOIDED", null));
        var allocation = await database.FeePaymentAllocations.SingleAsync(x => x.FeePaymentId == first.Id && x.StudentFeeChargeId == charge.Id);
        await service.VoidPaymentAsync(student.Id, first.Id, new("Entered in error"));
        var afterVoid = await database.StudentFeeCharges.SingleAsync(x => x.Id == charge.Id);
        Assert.Equal(0m, afterVoid.AmountPaid);
        Assert.True(await database.FeePayments.Where(x => x.Id == first.Id).Select(x => x.IsReversed).SingleAsync());
        Assert.True(await database.FeePaymentAllocations.AnyAsync(x => x.Id == allocation.Id));

        var second = await service.RecordPaymentAsync(student.Id, new(term.Id, 2000m, "Cash", "REMOVE-ACTIVE", null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveStudentChargeAsync(student.Id, charge.Id));
        await service.VoidPaymentAsync(student.Id, second.Id, new("Entered in error"));
        await service.RemoveStudentChargeAsync(student.Id, charge.Id);

        Assert.False(await database.StudentFeeCharges.Where(x => x.Id == charge.Id).Select(x => x.IsActive).SingleAsync());
        Assert.Equal(2, await database.FeePaymentAllocations.CountAsync(x => x.StudentFeeChargeId == charge.Id));
        var account = await service.GetStudentAccountAsync(student.Id, term.Id);
        Assert.Empty(account.Charges);
        Assert.Equal(0m, account.OutstandingBalance);
        Assert.Equal(2, account.Payments.Count);
        Assert.All(account.Payments, payment => Assert.True(payment.IsReversed));
        Assert.All(account.Payments, payment => Assert.Single(payment.Allocations!));
    }

    [PostgresTimetableFact]
    public async Task AssignmentRejectsCrossTenantStudent()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(tenant.Id, "2026/2027", new(2026, 9, 1), new(2027, 7, 1), true);
        var term = new AcademicTerm(tenant.Id, session.Id, "First Term", new(2026, 9, 1), new(2026, 12, 20), 1);
        var structure = new FeeStructure(tenant.Id, session.Id, term.Id, "Structure", "School", null);
        var foreignTenant = new Tenant("Other school", "other-school");
        var foreignStudent = NewStudent(foreignTenant.Id, "FOREIGN-1", "Foreign");
        database.AddRange(session, term, structure, foreignTenant, foreignStudent);
        await database.SaveChangesAsync();

        var service = new FeesService(database, new FixedTenantContext(tenant.Id));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReplaceAssignedStudentsAsync(
                structure.Id,
                new([foreignStudent.Id])));

        Assert.Contains("not found in this school", exception.Message);
    }

    [PostgresTimetableFact]
    public async Task VoidingPaymentReversesOnlyItsAllocationsAndKeepsHistory()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await database.Tenants.SingleAsync();
        var session = new AcademicSession(tenant.Id, "2026/2027", new(2026, 9, 1), new(2027, 7, 1), true);
        var term = new AcademicTerm(tenant.Id, session.Id, "First Term", new(2026, 9, 1), new(2026, 12, 20), 1);
        var campus = new Campus(tenant.Id, "Main campus");
        var level = new AcademicLevel(tenant.Id, "JSS", "Junior", 1);
        var classGroup = new ClassGroup(tenant.Id, campus.Id, level.Id, "JSS 2");
        var student = NewStudent(tenant.Id, "VOID-1", "Void");
        var enrollment = new StudentEnrollment(tenant.Id, student.Id, session.Id, level.Id, classGroup.Id, new(2026, 9, 1), true);
        var item = new FeeItem(tenant.Id, "Tuition", "TUI", null);
        var structure = new FeeStructure(tenant.Id, session.Id, term.Id, "Void test", "Class", classGroup.Id);
        database.AddRange(session, term, campus, level, classGroup, student, enrollment, item, structure);
        var line = new FeeStructureLine(tenant.Id, structure.Id, item.Id, 60000m, true);
        database.Add(line);
        await database.SaveChangesAsync();
        var service = new FeesService(database, new FixedTenantContext(tenant.Id));
        await service.ReplaceAssignedStudentsAsync(structure.Id, new([student.Id]));
        await service.GenerateChargesAsync(structure.Id);
        var first = await service.RecordPaymentAsync(student.Id, new(term.Id, 20000m, "Cash", "VOID-A", null));
        var second = await service.RecordPaymentAsync(student.Id, new(term.Id, 40000m, "Cash", "VOID-B", null));

        await service.VoidPaymentAsync(student.Id, first.Id, new("Entered in error"));

        var charge = await database.StudentFeeCharges.SingleAsync(x => x.FeeStructureLineId == line.Id && x.StudentId == student.Id);
        Assert.Equal(40000m, charge.AmountPaid);
        Assert.True(await database.FeePayments.Where(x => x.Id == first.Id).Select(x => x.IsReversed).SingleAsync());
        Assert.False(await database.FeePayments.Where(x => x.Id == second.Id).Select(x => x.IsReversed).SingleAsync());
        var account = await service.GetStudentAccountAsync(student.Id, term.Id);
        Assert.Equal(40000m, account.AppliedPayments);
        Assert.Equal(20000m, account.OutstandingBalance);
        Assert.Contains(account.Payments, payment => payment.Id == first.Id && payment.IsReversed);
        Assert.Contains(account.Payments, payment => payment.Id == second.Id && !payment.IsReversed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.VoidPaymentAsync(student.Id, first.Id, new("Again")));
    }

    private static Student NewStudent(Guid tenantId, string admissionNumber, string firstName) =>
        new(
            tenantId,
            admissionNumber,
            firstName,
            null,
            "Student",
            new(2012, 1, 1),
            "Female",
            new(2026, 9, 1),
            null,
            null);

    private sealed class FixedTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public string TenantSlug => "test-school";
    }

    private sealed class FixedUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => true;
        public bool IsPlatformSuperAdmin => false;
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid MembershipId { get; } = Guid.NewGuid();
        public string Email => "finance@test.local";
        public IReadOnlyCollection<string> Roles => ["Administrator"];
        public IReadOnlyCollection<string> Permissions => ["finance.read", "finance.configure"];
        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }
}
