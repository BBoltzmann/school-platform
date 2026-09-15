using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Audit;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Domain.Students;
using SchoolPlatform.Domain.Admissions;
using SchoolPlatform.Domain.Staff;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence;

public sealed class SchoolPlatformDbContext : DbContext
{
    public SchoolPlatformDbContext(
        DbContextOptions<SchoolPlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants =>
        Set<Tenant>();

    public DbSet<Campus> Campuses =>
        Set<Campus>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<TeacherPortalInvitation> TeacherPortalInvitations => Set<TeacherPortalInvitation>();

    public DbSet<PasswordRecoveryJob> PasswordRecoveryJobs => Set<PasswordRecoveryJob>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<TenantMembership> TenantMemberships =>
        Set<TenantMembership>();

    public DbSet<Role> Roles =>
        Set<Role>();

    public DbSet<Permission> Permissions =>
        Set<Permission>();

    public DbSet<MembershipRole> MembershipRoles =>
        Set<MembershipRole>();

    public DbSet<RolePermission> RolePermissions =>
        Set<RolePermission>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

    public DbSet<AcademicSession> AcademicSessions =>
        Set<AcademicSession>();

    public DbSet<AcademicTerm> AcademicTerms =>
        Set<AcademicTerm>();

    public DbSet<AcademicLevel> AcademicLevels =>
        Set<AcademicLevel>();

    public DbSet<ClassGroup> ClassGroups =>
        Set<ClassGroup>();

    public DbSet<Subject> Subjects =>
        Set<Subject>();

    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();

    
    public DbSet<Student> Students => Set<Student>();

    public DbSet<StudentEnrollment> StudentEnrollments
        => Set<StudentEnrollment>();

    
    public DbSet<Guardian> Guardians
        => Set<Guardian>();

    public DbSet<StudentGuardian> StudentGuardians
        => Set<StudentGuardian>();

    
    public DbSet<AdmissionApplication> AdmissionApplications
        => Set<AdmissionApplication>();

    public DbSet<AdmissionDocument> AdmissionDocuments
        => Set<AdmissionDocument>();



    public DbSet<StaffMember> StaffMembers
        => Set<StaffMember>();

        public DbSet<StaffAvailability> StaffAvailability
        => Set<StaffAvailability>();

        public DbSet<TeachingAssignment> TeachingAssignments
        => Set<TeachingAssignment>();

        public DbSet<TimetableSettings> TimetableSettings
        => Set<TimetableSettings>();

    public DbSet<TimetableDay> TimetableDays
        => Set<TimetableDay>();

    public DbSet<TimetableNonTeachingBlock> TimetableNonTeachingBlocks
        => Set<TimetableNonTeachingBlock>();

    public DbSet<ClassSubjectRequirement> ClassSubjectRequirements
        => Set<ClassSubjectRequirement>();

        public DbSet<GeneratedTimetable> GeneratedTimetables
        => Set<GeneratedTimetable>();

    public DbSet<GeneratedTimetableEntry> GeneratedTimetableEntries
        => Set<GeneratedTimetableEntry>();

    
    public DbSet<SchoolPlatform.Domain.Assessments.AcademicAssessment>
        AcademicAssessments
        => Set<SchoolPlatform.Domain.Assessments.AcademicAssessment>();

    public DbSet<SchoolPlatform.Domain.Assessments.AcademicAssessmentScore>
        AcademicAssessmentScores
        => Set<SchoolPlatform.Domain.Assessments.AcademicAssessmentScore>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryCategory>
        InventoryCategories
        => Set<SchoolPlatform.Domain.Inventory.InventoryCategory>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryItem>
        InventoryItems
        => Set<SchoolPlatform.Domain.Inventory.InventoryItem>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryItemVariant>
        InventoryItemVariants
        => Set<SchoolPlatform.Domain.Inventory.InventoryItemVariant>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryLocation>
        InventoryLocations
        => Set<SchoolPlatform.Domain.Inventory.InventoryLocation>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryStockBalance>
        InventoryStockBalances
        => Set<SchoolPlatform.Domain.Inventory.InventoryStockBalance>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryTransaction>
        InventoryTransactions
        => Set<SchoolPlatform.Domain.Inventory.InventoryTransaction>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryList>
        InventoryLists
        => Set<SchoolPlatform.Domain.Inventory.InventoryList>();

        public DbSet<SchoolPlatform.Domain.Inventory.InventoryListItem>
        InventoryListItems
        => Set<SchoolPlatform.Domain.Inventory.InventoryListItem>();

    
    public DbSet<SchoolPlatform.Domain.Fees.FeeItem>
        FeeItems
        => Set<SchoolPlatform.Domain.Fees.FeeItem>();

    public DbSet<SchoolPlatform.Domain.Fees.FeeStructure>
        FeeStructures
        => Set<SchoolPlatform.Domain.Fees.FeeStructure>();

    public DbSet<SchoolPlatform.Domain.Fees.FeeStructureLine>
        FeeStructureLines
        => Set<SchoolPlatform.Domain.Fees.FeeStructureLine>();

    public DbSet<SchoolPlatform.Domain.Fees.StudentFeeCharge>
        StudentFeeCharges
        => Set<SchoolPlatform.Domain.Fees.StudentFeeCharge>();

    public DbSet<SchoolPlatform.Domain.Fees.FeePayment>
        FeePayments
        => Set<SchoolPlatform.Domain.Fees.FeePayment>();

    public DbSet<SchoolPlatform.Domain.Fees.FeePaymentAllocation>
        FeePaymentAllocations
        => Set<SchoolPlatform.Domain.Fees.FeePaymentAllocation>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SchoolPlatformDbContext).Assembly);
    }
}
