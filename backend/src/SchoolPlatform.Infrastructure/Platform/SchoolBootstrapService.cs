using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Platform;

public sealed class SchoolBootstrapService : ISchoolBootstrapService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly IPasswordHasher<User> _passwordHasher;

    public SchoolBootstrapService(SchoolPlatformDbContext database, IPasswordHasher<User> passwordHasher)
    {
        _database = database;
        _passwordHasher = passwordHasher;
    }

    public async Task<BootstrapSchoolResult> BootstrapAsync(
        BootstrapSchoolRequest request,
        CancellationToken cancellationToken = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var email = request.AdminEmail.Trim().ToLowerInvariant();

        if (await _database.Tenants.AnyAsync(
                x => x.Slug == slug,
                cancellationToken))
        {
            throw new SchoolBootstrapConflictException(
                $"A school with slug '{slug}' already exists.");
        }

        var existingUser = await _database.Users
            .SingleOrDefaultAsync(
                x => x.Email == email,
                cancellationToken);

        if (request.AdminPassword is not null &&
            (existingUser is not null || !PasswordPolicy.IsValid(request.AdminPassword)))
            throw new SchoolBootstrapConflictException("Unable to create school with these details.");

        var tenant = new Tenant(
            request.SchoolName,
            slug);

        _database.Tenants.Add(tenant);

        var campus = new Campus(
            tenant.Id,
            request.CampusName);

        var user = existingUser ?? new User(
            email,
            request.AdminFirstName,
            request.AdminLastName);

        if (existingUser is null)
        {
            _database.Users.Add(user);
        }

        if (request.AdminPassword is not null)
            user.SetPasswordHash(_passwordHasher.HashPassword(user, request.AdminPassword));

        var membership = new TenantMembership(
            tenant.Id,
            user.Id);

        var administratorRole = new Role(
            tenant.Id,
            "Administrator",
            "Full school administrator");

        _database.Campuses.Add(campus);
        _database.TenantMemberships.Add(membership);
        _database.Roles.Add(administratorRole);

        var permissionCodes = new[]
        {
            "students.read",
            "students.create",
            "students.update",

            "staff.read",
            "staff.create",
            "staff.update",

            "admissions.read",
            "admissions.review",
            "admissions.approve",

            "academics.configure",

            "finance.read",
            "finance.configure",
            "finance.record_payment",

            "inventory.read",
            "inventory.receive",
            "inventory.issue",
            "inventory.adjust",

            "communications.broadcast",

            "users.manage",
            "roles.manage"
        };

        var existingPermissions = await _database.Permissions
            .Where(x => permissionCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        var permissionsByCode = existingPermissions
            .ToDictionary(x => x.Code);

        foreach (var code in permissionCodes)
        {
            if (permissionsByCode.ContainsKey(code))
                continue;

            var permission = new Permission(code);

            _database.Permissions.Add(permission);

            permissionsByCode[code] = permission;
        }

        var membershipRole = new MembershipRole(
            tenant.Id,
            membership.Id,
            administratorRole.Id);

        _database.MembershipRoles.Add(membershipRole);

        foreach (var permission in permissionsByCode.Values)
        {
            var rolePermission = new RolePermission(
                tenant.Id,
                administratorRole.Id,
                permission.Id);

            _database.RolePermissions.Add(rolePermission);
        }

        await _database.SaveChangesAsync(cancellationToken);

        return new BootstrapSchoolResult(
            tenant.Id,
            campus.Id,
            user.Id,
            membership.Id,
            administratorRole.Id);
    }
}
