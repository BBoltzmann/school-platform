using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class User : Entity
{
    private User()
    {
    }

    public User(
        string email,
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "Email is required.",
                nameof(email));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(
                "First name is required.",
                nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(
                "Last name is required.",
                nameof(lastName));

        Email = email.Trim().ToLowerInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public string Email { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<TenantMembership> Memberships { get; private set; }
        = new List<TenantMembership>();

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash is required.",
                nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
