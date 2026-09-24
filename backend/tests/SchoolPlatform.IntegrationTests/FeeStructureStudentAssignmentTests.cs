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
        Assert.Equal(2, generated.ChargesCreated);

        var repeated = await service.GenerateChargesAsync(assignedStructure.Id);
        Assert.Equal(0, repeated.ChargesCreated);

        await service.RemoveStudentAssignmentAsync(
            assignedStructure.Id,
            students[1].Id);
        Assert.Equal(2, await database.StudentFeeCharges.CountAsync(
            x => x.FeeStructureId == assignedStructure.Id));

        var secondGenerated = await service.GenerateChargesAsync(secondStructure.Id);
        Assert.Equal(1, secondGenerated.StudentCount);
        Assert.Equal(1, secondGenerated.ChargesCreated);

        var legacyGenerated = await service.GenerateChargesAsync(legacyStructure.Id);
        Assert.Equal(3, legacyGenerated.StudentCount);
        Assert.Equal(3, legacyGenerated.ChargesCreated);

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
        Assert.Equal(200m, await database.StudentFeeCharges
            .Where(x => x.FeeStructureId == assignedStructure.Id)
            .Select(x => x.Amount)
            .SumAsync());

        var clearException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReplaceAssignedStudentsAsync(
                assignedStructure.Id,
                new(Array.Empty<Guid>())));
        Assert.Contains("Keep at least one student assigned", clearException.Message);
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
