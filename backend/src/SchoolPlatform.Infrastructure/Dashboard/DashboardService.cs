using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Dashboard;
using SchoolPlatform.Application.Timetabling;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Dashboard;

public sealed class DashboardService(
    SchoolPlatformDbContext database,
    ITenantContext tenantContext,
    ITimetableReadinessService timetableReadiness,
    TimeProvider clock) : IDashboardService
{
    public async Task<DashboardResult> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var tenant = await database.Tenants.AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => new DashboardTenantResult(x.Id, x.Slug, x.Name))
            .SingleAsync(cancellationToken);

        // Match directory semantics: include inactive records, never count login users as staff.
        var students = await database.Students.CountAsync(x => x.TenantId == tenantId, cancellationToken);
        var staff = await database.StaffMembers.CountAsync(x => x.TenantId == tenantId, cancellationToken);
        // Waitlisted, Approved and Rejected applications are explicitly not pending.
        var pending = await database.AdmissionApplications.CountAsync(x =>
            x.TenantId == tenantId && x.IsActive &&
            (x.Status == "Submitted" || x.Status == "Under Review"), cancellationToken);
        // Same reversal semantics as FeesService, across all terms and sessions.
        var collected = await database.FeePayments
            .Where(x => x.TenantId == tenantId && !x.IsReversed)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var session = await database.AcademicSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsCurrent && x.IsActive)
            .OrderByDescending(x => x.StartDate).ThenBy(x => x.Id)
            .Select(x => new DashboardAcademicPeriodResult(x.Id, x.Name, x.StartDate, x.EndDate))
            .FirstOrDefaultAsync(cancellationToken);
        DashboardAcademicPeriodResult? term = null;
        var configured = false;
        if (session is not null)
        {
            var matchingTerms = await database.AcademicTerms.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.AcademicSessionId == session.Id &&
                    x.IsActive && x.StartDate <= today && x.EndDate >= today)
                .Select(x => new DashboardAcademicPeriodResult(x.Id, x.Name, x.StartDate, x.EndDate))
                .Take(2).ToListAsync(cancellationToken);
            term = matchingTerms.Count == 1 ? matchingTerms[0] : null;
            // A usable class needs an active level and campus belonging to this same tenant.
            configured = await database.ClassGroups.AnyAsync(x =>
                x.TenantId == tenantId && x.IsActive &&
                x.AcademicLevel.TenantId == tenantId && x.AcademicLevel.IsActive &&
                x.Campus.TenantId == tenantId && x.Campus.IsActive, cancellationToken);
        }

        // Allocations cannot exceed a charge, so distinct students with positive active
        // charge balances matches the fee service's students-owing semantics, all time.
        var owing = await database.StudentFeeCharges
            .Where(x => x.TenantId == tenantId && x.IsActive && x.Amount > x.AmountPaid)
            .Select(x => x.StudentId).Distinct().CountAsync(cancellationToken);

        var actions = new List<DashboardActionResult>();
        if (session is null)
            actions.Add(new("NO_CURRENT_SESSION", "high", null));
        else
        {
            if (term is null)
                actions.Add(new("NO_CURRENT_TERM", "high", null));
            if (!configured)
                actions.Add(new("ACADEMICS_NOT_CONFIGURED", "normal", null));
        }
        if (pending > 0)
            actions.Add(new("PENDING_APPLICATIONS", "normal", pending));
        if (owing > 0)
            actions.Add(new("OUTSTANDING_FEES", "normal", owing));
        if (session is not null && term is not null && configured)
        {
            var readiness = await timetableReadiness.GetReadinessAsync(cancellationToken);
            if (!readiness.CanGenerate)
                actions.Add(new("TIMETABLE_SETUP_REQUIRED", "normal", null));
        }

        var activity = await database.AuditLogs.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => new DashboardActivityResult(x.Id, x.CreatedAtUtc, x.Action, x.EntityType, x.EntityId))
            .Take(10).ToListAsync(cancellationToken);

        return new DashboardResult(now, tenant,
            new(students, staff, pending, new(collected, "NGN", "allTime")),
            new(session, term, session is not null,
                session is null ? "Create a current academic session before accepting applications." : null),
            actions, activity, new("notImplemented", false, []));
    }
}
