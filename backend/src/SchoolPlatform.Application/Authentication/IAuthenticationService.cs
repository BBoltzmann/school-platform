namespace SchoolPlatform.Application.Authentication;

public interface IAuthenticationService
{
    Task<LoginResult?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SetInitialPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
