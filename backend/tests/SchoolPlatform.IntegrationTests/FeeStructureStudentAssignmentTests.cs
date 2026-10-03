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
}
