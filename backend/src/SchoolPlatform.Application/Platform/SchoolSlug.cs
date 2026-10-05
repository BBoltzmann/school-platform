using System.Globalization;
using System.Text;

namespace SchoolPlatform.Application.Platform;

public static class SchoolSlug
{
    public static string FromName(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (character is '\'' or '’') continue;
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }
        return builder.ToString().Trim('-');
    }

    public static string Normalize(string value) => FromName(value.Replace('_', '-'));
}
