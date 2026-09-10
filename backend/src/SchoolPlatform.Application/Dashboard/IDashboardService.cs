namespace SchoolPlatform.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardResult> GetAsync(CancellationToken cancellationToken = default);
}
