using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SchoolPlatform.Application.Authentication;

public static class AuthInput
{
    public static bool EmailIsValid(string? value) => value is { Length: > 0 and <= 320 }
        && MailAddress.TryCreate(value.Trim(), out var address)
        && address.Address == value.Trim();
    public static bool SlugIsValid(string? value) => value is { Length: >= 3 and <= 100 }
        && Regex.IsMatch(value, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
}
