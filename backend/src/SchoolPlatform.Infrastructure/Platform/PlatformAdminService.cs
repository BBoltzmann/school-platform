using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using SchoolPlatform.Application.Email;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Platform;

public sealed class PlatformAdminService(
    SchoolPlatformDbContext database,
    ISchoolBootstrapService bootstrap,
    IEmailSender emailSender,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<PlatformAdminService> logger,
    IWebsiteImportService websiteImport) : IPlatformAdminService
{
    public async Task<PlatformDashboardResult> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var schoolCounts = await database.Tenants.AsNoTracking()
            .GroupBy(x => x.IsActive)
            .Select(x => new { Active = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);
        var metrics = await BuildMetricsAsync(cancellationToken);
        return new(
            schoolCounts.Sum(x => x.Count),
            schoolCounts.SingleOrDefault(x => x.Active)?.Count ?? 0,
            schoolCounts.SingleOrDefault(x => !x.Active)?.Count ?? 0,
            metrics.Sum(x => x.Students), metrics.Sum(x => x.ActiveStudents),
            metrics.Sum(x => x.Staff), metrics.Sum(x => x.TeachingStaff), metrics.Sum(x => x.NonTeachingStaff),
            metrics.Sum(x => x.Guardians), metrics.Sum(x => x.ActivatedAccounts), metrics.Sum(x => x.PendingTeacherInvitations));
    }

    public async Task<IReadOnlyCollection<PlatformSchoolSummary>> GetSchoolsAsync(CancellationToken cancellationToken = default)
        => await BuildMetricsAsync(cancellationToken);

    public async Task<PlatformSchoolDetails> GetSchoolAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var summary = (await BuildMetricsAsync(cancellationToken)).SingleOrDefault(x => x.TenantId == tenantId)
            ?? throw new InvalidOperationException("School was not found.");
        var campuses = await database.Campuses.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => new PlatformCampusSummary(x.Id, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);
        var administrators = await (from membership in database.TenantMemberships.AsNoTracking()
            join user in database.Users.AsNoTracking() on membership.UserId equals user.Id
            join assignment in database.MembershipRoles.AsNoTracking() on membership.Id equals assignment.MembershipId
            join role in database.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where membership.TenantId == tenantId && membership.IsActive && role.IsActive && role.Name == "Administrator"
            select new { user.Id, user.Email, user.FirstName, user.LastName, user.PasswordHash }).Distinct().ToListAsync(cancellationToken);
        var pendingIds = await database.PasswordResetTokens.AsNoTracking().Where(x => x.TenantId == tenantId && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).Select(x => x.UserId).ToListAsync(cancellationToken);
        return new(summary, campuses, administrators.Select(x => new PlatformAdministratorSummary(x.Id, $"{x.FirstName} {x.LastName}".Trim(), x.Email, x.PasswordHash is not null ? "Active" : pendingIds.Contains(x.Id) ? "Pending setup" : "Invitation expired")).ToList());
    }

    public async Task<PlatformSchoolProvisionedResult> CreateSchoolAsync(CreatePlatformSchoolRequest request, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? await FindAvailableSlugAsync(SchoolSlug.FromName(request.Name), cancellationToken)
            : SchoolSlug.Normalize(request.Slug);
        if (string.IsNullOrWhiteSpace(slug)) throw new InvalidOperationException("A school name is required to generate a login code.");
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var result = await bootstrap.BootstrapAsync(new BootstrapSchoolRequest(
            request.Name, slug, request.CampusName,
            request.AdministratorEmail, request.AdministratorFirstName, request.AdministratorLastName), cancellationToken);
        var profile = new SchoolPlatform.Domain.Tenancy.TenantProfile(result.TenantId);
        var branding = new UpdateTenantBrandingRequest(
            request.WebsiteUrl, request.ContactEmail, request.ContactPhone, request.Address,
            request.Motto, request.Mission, request.Vision, request.ShortAbout,
            request.LogoDataUrl, request.IconDataUrl, request.PrimaryColor,
            request.SecondaryColor, request.AccentColor);
        ValidateProfile(branding);
        profile.Update(branding.WebsiteUrl, branding.ContactEmail, branding.ContactPhone, branding.Address,
            branding.Motto, branding.Mission, branding.Vision, branding.ShortAbout,
            branding.LogoDataUrl, branding.IconDataUrl, branding.PrimaryColor,
            branding.SecondaryColor, branding.AccentColor);
        database.TenantProfiles.Add(profile);
        await database.SaveChangesAsync(cancellationToken);
        var pending = await database.Users.AsNoTracking()
            .Where(x => x.Id == result.UserId)
            .Select(x => x.PasswordHash == null)
            .SingleAsync(cancellationToken);
        var setupLink = pending
            ? await CreateAdministratorActivationAsync(result.UserId, result.TenantId, request.AdministratorEmail, cancellationToken)
            : null;
        await transaction.CommitAsync(cancellationToken);
        return new(await GetSchoolAsync(result.TenantId, cancellationToken), pending, setupLink);
    }

    private async Task<string> FindAvailableSlugAsync(string baseSlug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseSlug)) return string.Empty;
        var candidate = baseSlug; var suffix = 2;
        while (await database.Tenants.AnyAsync(x => x.Slug == candidate, cancellationToken)) candidate = $"{baseSlug}-{suffix++}";
        return candidate;
    }

    public async Task<string> ReissueAdministratorInvitationAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var identity = await (from membership in database.TenantMemberships
            join user in database.Users on membership.UserId equals user.Id
            join roleAssignment in database.MembershipRoles on membership.Id equals roleAssignment.MembershipId
            join role in database.Roles on roleAssignment.RoleId equals role.Id
            where membership.TenantId == tenantId && membership.UserId == userId && membership.IsActive && role.Name == "Administrator" && role.IsActive && user.IsActive
            select new { user.Email, user.PasswordHash }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Administrator was not found for this school.");
        if (identity.PasswordHash is not null) throw new InvalidOperationException("This administrator already has an active account.");
        await database.PasswordResetTokens.Where(x => x.TenantId == tenantId && x.UserId == userId && x.UsedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, DateTime.UtcNow), cancellationToken);
        return await CreateAdministratorActivationAsync(userId, tenantId, identity.Email, cancellationToken)
            ?? throw new InvalidOperationException("Administrator setup links are not configured.");
    }

    public async Task<PlatformSchoolDeletionEligibility> GetDeletionEligibilityAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!await database.Tenants.AnyAsync(x => x.Id == tenantId, cancellationToken)) throw new InvalidOperationException("School was not found.");
        var counts = new Dictionary<string, int>();
        async Task Add(string name, Func<Task<int>> count) => counts[name] = await count();
        await Add("Students", () => database.Students.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Staff", () => database.StaffMembers.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Guardians", () => database.Guardians.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Admissions", () => database.AdmissionApplications.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("AcademicSessions", () => database.AcademicSessions.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("AcademicTerms", () => database.AcademicTerms.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("AcademicLevels", () => database.AcademicLevels.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Classes", () => database.ClassGroups.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Subjects", () => database.Subjects.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("SubjectsOffered", () => database.ClassSubjects.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Enrollments", () => database.StudentEnrollments.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("StudentGuardians", () => database.StudentGuardians.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("AdmissionDocuments", () => database.AdmissionDocuments.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("StaffAssignments", () => database.TeachingAssignments.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("StaffAvailability", () => database.StaffAvailability.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("TimetableSettings", () => database.TimetableSettings.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("TimetableDays", () => database.TimetableDays.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("TimetableBlocks", () => database.TimetableNonTeachingBlocks.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("ClassSubjectRequirements", () => database.ClassSubjectRequirements.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("ParallelSubjectGroups", () => database.ParallelSubjectGroups.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Timetables", () => database.GeneratedTimetables.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("TimetableEntries", () => database.GeneratedTimetableEntries.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("Assessments", () => database.AcademicAssessments.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("AssessmentScores", () => database.AcademicAssessmentScores.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("InventoryCategories", () => database.InventoryCategories.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("InventoryItems", () => database.InventoryItems.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("InventoryLocations", () => database.InventoryLocations.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("InventoryTransactions", () => database.InventoryTransactions.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("InventoryLists", () => database.InventoryLists.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("FeeItems", () => database.FeeItems.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("FeeStructures", () => database.FeeStructures.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("FeeStructureLines", () => database.FeeStructureLines.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("FeeAssignments", () => database.FeeStructureStudentAssignments.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("FeeCharges", () => database.StudentFeeCharges.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("FeePayments", () => database.FeePayments.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("PaymentAllocations", () => database.FeePaymentAllocations.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        await Add("AuditLogs", () => database.AuditLogs.CountAsync(x => x.TenantId == tenantId, cancellationToken));
        foreach (var key in counts.Where(x => x.Value == 0).Select(x => x.Key).ToArray()) counts.Remove(key);
        return new(counts.Count == 0, counts);
    }

    public async Task DeleteEmptySchoolAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var eligibility = await GetDeletionEligibilityAsync(tenantId, cancellationToken);
        if (!eligibility.CanDeletePermanently) throw new InvalidOperationException("This school contains operational records and cannot be permanently deleted. Suspend it instead.");
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await database.PasswordResetTokens.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.TeacherPortalInvitations.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.MembershipRoles.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.RolePermissions.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.Roles.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.TenantProfiles.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.TenantMemberships.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.Campuses.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await database.Tenants.Where(x => x.Id == tenantId).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetSchoolActiveAsync(Guid tenantId, bool active, CancellationToken cancellationToken = default)
    {
        var tenant = await database.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("School was not found.");
        if (active) tenant.Reactivate(); else tenant.Suspend();
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task PromoteSuperAdminAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await database.Users.SingleOrDefaultAsync(x => x.Email == normalized && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        if (!await database.PlatformRoleAssignments.AnyAsync(x => x.UserId == user.Id && x.Role == PlatformRoles.SuperAdmin, cancellationToken))
            database.PlatformRoleAssignments.Add(new SchoolPlatform.Domain.Identity.PlatformRoleAssignment(user.Id, PlatformRoles.SuperAdmin));
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task<WebsiteImportDraft> ScanWebsiteAsync(string url, CancellationToken cancellationToken = default)
        => websiteImport.ScanAsync(url, cancellationToken);

    public async Task<TenantBrandingResult> GetBrandingAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await database.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken) ?? throw new InvalidOperationException("School was not found.");
        var profile = await database.TenantProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        return ToBranding(tenant.Id, tenant.Name, tenant.Slug, profile);
    }

    public async Task<TenantBrandingResult> UpdateBrandingAsync(Guid tenantId, UpdateTenantBrandingRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await database.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken) ?? throw new InvalidOperationException("School was not found.");
        var profile = await database.TenantProfiles.SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        if (profile is null)
        {
            profile = new SchoolPlatform.Domain.Tenancy.TenantProfile(tenantId);
            database.TenantProfiles.Add(profile);
        }
        ValidateProfile(request);
        profile.Update(request.WebsiteUrl, request.ContactEmail, request.ContactPhone, request.Address, request.Motto, request.Mission, request.Vision, request.ShortAbout, request.LogoDataUrl, request.IconDataUrl, request.PrimaryColor, request.SecondaryColor, request.AccentColor);
        await database.SaveChangesAsync(cancellationToken);
        return ToBranding(tenant.Id, tenant.Name, tenant.Slug, profile);
    }

    private static TenantBrandingResult ToBranding(Guid id, string name, string slug, SchoolPlatform.Domain.Tenancy.TenantProfile? p)
        => new(id.ToString(), name, slug, p?.WebsiteUrl, p?.ContactEmail, p?.ContactPhone, p?.Address, p?.Motto, p?.Mission, p?.Vision, p?.ShortAbout, p?.LogoDataUrl, p?.IconDataUrl, p?.PrimaryColor ?? "#F5D900", p?.SecondaryColor ?? "#0B0B0B", p?.AccentColor ?? "#FFF8C9");

    private static void ValidateProfile(UpdateTenantBrandingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.WebsiteUrl) && (!Uri.TryCreate(request.WebsiteUrl, UriKind.Absolute, out var uri) || uri.Scheme is not "http" and not "https")) throw new InvalidOperationException("Website URL must use HTTP or HTTPS.");
        foreach (var color in new[] { request.PrimaryColor, request.SecondaryColor, request.AccentColor }.Where(x => !string.IsNullOrWhiteSpace(x)))
            if (!System.Text.RegularExpressions.Regex.IsMatch(color!, "^#[0-9A-Fa-f]{6}$")) throw new InvalidOperationException("Colours must be six-digit hexadecimal values.");
        foreach (var asset in new[] { request.LogoDataUrl, request.IconDataUrl }.Where(x => !string.IsNullOrWhiteSpace(x)))
            if (asset!.Length > 2_000_000 || !System.Text.RegularExpressions.Regex.IsMatch(asset, "^data:image/(png|jpeg|webp);base64,[A-Za-z0-9+/]+=*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) throw new InvalidOperationException("Logo and icon uploads must be small PNG, JPEG or WEBP images.");
    }

    private async Task<List<PlatformSchoolSummary>> BuildMetricsAsync(CancellationToken cancellationToken)
    {
        var tenants = await database.Tenants.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var students = await database.Students.AsNoTracking().GroupBy(x => x.TenantId).Select(x => new { x.Key, Total = x.Count(), Active = x.Count(s => s.IsActive) }).ToDictionaryAsync(x => x.Key, cancellationToken);
        var staff = await database.StaffMembers.AsNoTracking().GroupBy(x => x.TenantId).Select(x => new { x.Key, Total = x.Count(), Teaching = x.Count(s => s.IsTeachingStaff), NonTeaching = x.Count(s => !s.IsTeachingStaff) }).ToDictionaryAsync(x => x.Key, cancellationToken);
        var guardians = await database.Guardians.AsNoTracking().GroupBy(x => x.TenantId).Select(x => new { x.Key, Total = x.Count() }).ToDictionaryAsync(x => x.Key, cancellationToken);
        var accounts = await (from membership in database.TenantMemberships.AsNoTracking()
                              join user in database.Users.AsNoTracking() on membership.UserId equals user.Id
                              where membership.IsActive && user.IsActive && user.PasswordHash != null
                              group user by membership.TenantId into grouped
                              select new { TenantId = grouped.Key, Count = grouped.Select(x => x.Id).Distinct().Count() }).ToDictionaryAsync(x => x.TenantId, cancellationToken);
        var admins = await (from membership in database.TenantMemberships.AsNoTracking()
                            join assignment in database.MembershipRoles.AsNoTracking() on membership.Id equals assignment.MembershipId
                            join role in database.Roles.AsNoTracking() on assignment.RoleId equals role.Id
                            where membership.IsActive && role.IsActive && role.Name == "Administrator"
                            group membership by membership.TenantId into grouped
                            select new { TenantId = grouped.Key, Count = grouped.Select(x => x.UserId).Distinct().Count() }).ToDictionaryAsync(x => x.TenantId, cancellationToken);
        var invites = await database.TeacherPortalInvitations.AsNoTracking().Where(x => x.UsedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).GroupBy(x => x.TenantId).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, cancellationToken);
        var activity = await database.AuditLogs.AsNoTracking().GroupBy(x => x.TenantId).Select(x => new { x.Key, Last = x.Max(a => (DateTime?)a.CreatedAtUtc) }).ToDictionaryAsync(x => x.Key, cancellationToken);
        return tenants.Select(tenant =>
        {
            students.TryGetValue(tenant.Id, out var student);
            staff.TryGetValue(tenant.Id, out var employee);
            guardians.TryGetValue(tenant.Id, out var guardian);
            accounts.TryGetValue(tenant.Id, out var account);
            admins.TryGetValue(tenant.Id, out var administrator);
            invites.TryGetValue(tenant.Id, out var invitation);
            activity.TryGetValue(tenant.Id, out var lastActivity);
            return new PlatformSchoolSummary(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAtUtc,
                student?.Total ?? 0, student?.Active ?? 0, employee?.Total ?? 0, employee?.Teaching ?? 0, employee?.NonTeaching ?? 0,
                guardian?.Total ?? 0, account?.Count ?? 0, administrator?.Count ?? 0, invitation?.Count ?? 0, lastActivity?.Last);
        }).ToList();
    }

    private async Task<string?> CreateAdministratorActivationAsync(Guid userId, Guid tenantId, string email, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(configuration["PASSWORD_RESET_BASE_URL"], UriKind.Absolute, out var baseUrl)
            || (baseUrl.Scheme != "https" && !(baseUrl.Scheme == "http" && baseUrl.IsLoopback)))
        {
            logger.LogWarning("Administrator activation was not issued because PASSWORD_RESET_BASE_URL is not configured.");
            return null;
        }
        var now = clock.GetUtcNow().UtcDateTime;
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        database.PasswordResetTokens.Add(new SchoolPlatform.Domain.Identity.PasswordResetToken(
            userId, tenantId, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))), now.AddMinutes(30)));
        await database.SaveChangesAsync(cancellationToken);
        var setupLink = new Uri(baseUrl, $"account/setup?token={raw}").AbsoluteUri;
        try
        {
            await emailSender.SendPasswordResetAsync(email, new Uri(setupLink), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Administrator activation email could not be delivered.");
        }
        return setupLink;
    }
}
