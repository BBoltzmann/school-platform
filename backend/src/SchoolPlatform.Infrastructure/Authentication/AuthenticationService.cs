using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthenticationService(
        SchoolPlatformDbContext database,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _database = database;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<bool> SetInitialPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _database.Users
            .SingleOrDefaultAsync(
                x => x.Email == normalizedEmail,
                cancellationToken);

        if (user is null || !user.IsActive)
            return false;

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            return false;

        var passwordHash =
            _passwordHasher.HashPassword(user, password);

        user.SetPasswordHash(passwordHash);

        await _database.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<LoginResult?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        var normalizedSlug =
            request.TenantSlug.Trim().ToLowerInvariant();

        var user = await _database.Users
            .SingleOrDefaultAsync(
                x => x.Email == normalizedEmail,
                cancellationToken);

        if (user is null ||
            !user.IsActive ||
            string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        var passwordResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
            return null;

        var tenant = await _database.Tenants
            .SingleOrDefaultAsync(
                x => x.Slug == normalizedSlug && x.IsActive,
                cancellationToken);

        if (tenant is null)
            return null;

        var membership = await _database.TenantMemberships
            .SingleOrDefaultAsync(
                x =>
                    x.TenantId == tenant.Id &&
                    x.UserId == user.Id &&
                    x.IsActive,
                cancellationToken);

        if (membership is null)
            return null;

        var roles = await (
            from membershipRole in _database.MembershipRoles
            join role in _database.Roles
                on membershipRole.RoleId equals role.Id
            where
                membershipRole.TenantId == tenant.Id &&
                membershipRole.MembershipId == membership.Id &&
                role.IsActive
            select role.Name
        )
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync(cancellationToken);

        var permissions = await (
            from membershipRole in _database.MembershipRoles
            join rolePermission in _database.RolePermissions
                on membershipRole.RoleId equals rolePermission.RoleId
            join permission in _database.Permissions
                on rolePermission.PermissionId equals permission.Id
            where
                membershipRole.TenantId == tenant.Id &&
                membershipRole.MembershipId == membership.Id &&
                rolePermission.TenantId == tenant.Id
            select permission.Code
        )
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync(cancellationToken);

        var expiresAtUtc =
            DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new(
                "tenant_id",
                tenant.Id.ToString()),

            new(
                "tenant_slug",
                tenant.Slug),

            new(
                "membership_id",
                membership.Id.ToString())
        };

        claims.Add(new Claim("security_stamp", user.SecurityStamp ?? string.Empty));

        claims.AddRange(
            roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        claims.AddRange(
            permissions.Select(permission =>
                new Claim("permission", permission)));

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenValue =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new LoginResult(
            tokenValue,
            expiresAtUtc,
            user.Id,
            tenant.Id,
            membership.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            roles,
            permissions);
    }
}
