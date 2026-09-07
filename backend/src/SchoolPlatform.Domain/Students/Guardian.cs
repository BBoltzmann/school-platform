using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Students;

public sealed class Guardian : TenantEntity
{
    private Guardian()
    {
    }

    public Guardian(
        Guid tenantId,
        string firstName,
        string? middleName,
        string lastName,
        string? email,
        string phone,
        string? alternatePhone,
        string? occupation,
        string? address)
    {
        TenantId = tenantId;

        FirstName = firstName.Trim();

        MiddleName = string.IsNullOrWhiteSpace(middleName)
            ? null
            : middleName.Trim();

        LastName = lastName.Trim();

        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        Phone = phone.Trim();

        AlternatePhone = string.IsNullOrWhiteSpace(alternatePhone)
            ? null
            : alternatePhone.Trim();

        Occupation = string.IsNullOrWhiteSpace(occupation)
            ? null
            : occupation.Trim();

        Address = string.IsNullOrWhiteSpace(address)
            ? null
            : address.Trim();

        IsActive = true;
    }

    public string FirstName { get; private set; } = null!;

    public string? MiddleName { get; private set; }

    public string LastName { get; private set; } = null!;

    public string? Email { get; private set; }

    public string Phone { get; private set; } = null!;

    public string? AlternatePhone { get; private set; }

    public string? Occupation { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<StudentGuardian> StudentLinks { get; private set; }
        = new List<StudentGuardian>();

    public void Update(
        string firstName,
        string? middleName,
        string lastName,
        string? email,
        string phone,
        string? alternatePhone,
        string? occupation,
        string? address)
    {
        FirstName = firstName.Trim();

        MiddleName = string.IsNullOrWhiteSpace(middleName)
            ? null
            : middleName.Trim();

        LastName = lastName.Trim();

        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        Phone = phone.Trim();

        AlternatePhone = string.IsNullOrWhiteSpace(alternatePhone)
            ? null
            : alternatePhone.Trim();

        Occupation = string.IsNullOrWhiteSpace(occupation)
            ? null
            : occupation.Trim();

        Address = string.IsNullOrWhiteSpace(address)
            ? null
            : address.Trim();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
