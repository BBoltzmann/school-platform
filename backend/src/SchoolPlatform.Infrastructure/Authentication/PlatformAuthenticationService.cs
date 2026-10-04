using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Authentication;

public sealed class PlatformAuthenticationService(
    SchoolPlatformDbContext database,
    IPasswordHasher<User> passwordHasher,
    IOptions<JwtOptions> jwtOptions) : IPlatformAuthenticationService
{
    public async Task<PlatformLoginResult?> LoginAsync(PlatformLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await database.Users.SingleOrDefaultAsync(x => x.Email == email && x.IsActive && x.PasswordHash != null, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password) == PasswordVerificationResult.Failed)
            return null;
        var isSuperAdmin = await database.PlatformRoleAssignments.AnyAsync(x => x.UserId == user.Id && x.Role == PlatformRoles.SuperAdmin && x.IsActive, cancellationToken);
        if (!isSuperAdmin) return null;
        var expires = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("platform_role", PlatformRoles.SuperAdmin),
            new("security_stamp", user.SecurityStamp ?? string.Empty)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key));
        var token = new JwtSecurityToken(jwtOptions.Value.Issuer, jwtOptions.Value.Audience, claims, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Id, user.Email, user.FirstName, user.LastName, [PlatformRoles.SuperAdmin]);
    }
}
