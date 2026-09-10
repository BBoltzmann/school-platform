namespace SchoolPlatform.Application.Dashboard;

public sealed record DashboardResult(
    DateTimeOffset GeneratedAtUtc,
    DashboardTenantResult Tenant,
    DashboardMetricsResult Metrics,
    DashboardAcademicResult Academic,
    IReadOnlyCollection<DashboardActionResult> Actions,
    IReadOnlyCollection<DashboardActivityResult> RecentActivity,
    DashboardEventsResult UpcomingEvents);

public sealed record DashboardTenantResult(Guid Id, string Slug, string Name);
public sealed record DashboardMetricsResult(
    int TotalStudents, int TotalStaff, int PendingApplications,
    DashboardFeesResult FeesCollected);
public sealed record DashboardFeesResult(decimal Amount, string Currency, string Period);
public sealed record DashboardAcademicResult(
    DashboardAcademicPeriodResult? CurrentSession,
    DashboardAcademicPeriodResult? CurrentTerm,
    bool AdmissionsEnabled,
    string? AdmissionsBlockedReason);
public sealed record DashboardAcademicPeriodResult(
    Guid Id, string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record DashboardActionResult(string Code, string Priority, int? Count);
// Existing audit fields only; no inferred actor names or raw JSON snapshots.
public sealed record DashboardActivityResult(
    Guid Id, DateTime CreatedAtUtc, string Action, string EntityType, Guid? EntityId);
public sealed record DashboardEventsResult(
    string Availability, bool CanCreate, IReadOnlyCollection<DashboardEventResult> Items);
public sealed record DashboardEventResult(
    Guid Id, string Title, DateTimeOffset StartsAt, string? Location);
