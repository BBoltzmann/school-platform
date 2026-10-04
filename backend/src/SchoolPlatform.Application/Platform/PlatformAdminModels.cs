namespace SchoolPlatform.Application.Platform;

public static class PlatformRoles
{
    public const string SuperAdmin = "PlatformSuperAdmin";
}

public sealed record PlatformLoginRequest(string Email, string Password);

public sealed record PlatformLoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles);

public sealed record CreatePlatformSchoolRequest(
    string Name,
    string Slug,
    string CampusName,
    string AdministratorFirstName,
    string AdministratorLastName,
    string AdministratorEmail);

public sealed record PlatformSchoolSummary(
    Guid TenantId,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAtUtc,
    int Students,
    int ActiveStudents,
    int Staff,
    int TeachingStaff,
    int NonTeachingStaff,
    int Guardians,
    int ActivatedAccounts,
    int ActiveAdministrators,
    int PendingTeacherInvitations,
    DateTime? LastActivityAtUtc);

public sealed record PlatformDashboardResult(
    int TotalSchools,
    int ActiveSchools,
    int SuspendedSchools,
    int Students,
    int ActiveStudents,
    int Staff,
    int TeachingStaff,
    int NonTeachingStaff,
    int Guardians,
    int ActivatedAccounts,
    int PendingTeacherInvitations);

public sealed record PlatformSchoolDetails(
    PlatformSchoolSummary Summary,
    IReadOnlyCollection<PlatformCampusSummary> Campuses);

public sealed record PlatformCampusSummary(Guid Id, string Name, bool IsActive);

public sealed record PlatformSchoolProvisionedResult(
    PlatformSchoolDetails School,
    bool AdministratorActivationPending);

public interface IPlatformAdminService
{
    Task<PlatformDashboardResult> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlatformSchoolSummary>> GetSchoolsAsync(CancellationToken cancellationToken = default);
    Task<PlatformSchoolDetails> GetSchoolAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PlatformSchoolProvisionedResult> CreateSchoolAsync(CreatePlatformSchoolRequest request, CancellationToken cancellationToken = default);
    Task SetSchoolActiveAsync(Guid tenantId, bool active, CancellationToken cancellationToken = default);
    Task PromoteSuperAdminAsync(string email, CancellationToken cancellationToken = default);
    Task<TenantBrandingResult> GetBrandingAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantBrandingResult> UpdateBrandingAsync(Guid tenantId, UpdateTenantBrandingRequest request, CancellationToken cancellationToken = default);
    Task<WebsiteImportDraft> ScanWebsiteAsync(string url, CancellationToken cancellationToken = default);
}

public sealed record UpdateTenantBrandingRequest(string? WebsiteUrl, string? ContactEmail, string? ContactPhone, string? Address, string? Motto, string? Mission, string? Vision, string? ShortAbout, string? LogoDataUrl, string? IconDataUrl, string? PrimaryColor, string? SecondaryColor, string? AccentColor);
public sealed record TenantBrandingResult(string TenantId, string Name, string Slug, string? WebsiteUrl, string? ContactEmail, string? ContactPhone, string? Address, string? Motto, string? Mission, string? Vision, string? ShortAbout, string? LogoDataUrl, string? IconDataUrl, string PrimaryColor, string SecondaryColor, string AccentColor);
public sealed record WebsiteImportDraft(
    string FinalUrl,
    string? Title,
    IReadOnlyCollection<string> NameCandidates,
    string? Description,
    IReadOnlyCollection<string> LogoCandidates,
    IReadOnlyCollection<string> IconCandidates,
    IReadOnlyCollection<string> Emails,
    IReadOnlyCollection<string> Phones,
    IReadOnlyCollection<string> SocialLinks,
    IReadOnlyCollection<string> ColorCandidates,
    string? MottoCandidate,
    string? Mission = null,
    string? Vision = null,
    IReadOnlyCollection<string>? CoreValues = null,
    IReadOnlyCollection<string>? PagesVisited = null,
    IReadOnlyCollection<string>? Warnings = null,
    IReadOnlyCollection<string>? FailedPages = null,
    IReadOnlyDictionary<string, string>? Sources = null,
    IReadOnlyCollection<WebsiteColorCandidate>? ColorCandidatesDetailed = null);

public sealed record WebsiteColorCandidate(string Value, string? RoleSuggestion, string Source, string SourceUrl);

public interface IPlatformAuthenticationService
{
    Task<PlatformLoginResult?> LoginAsync(PlatformLoginRequest request, CancellationToken cancellationToken = default);
}
