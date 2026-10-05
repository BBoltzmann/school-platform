using SchoolPlatform.Application.Platform;

namespace SchoolPlatform.UnitTests;

public sealed class SchoolSlugTests
{
    [Theory]
    [InlineData("Dietams School", "dietams-school")]
    [InlineData("Antioch Royal College", "antioch-royal-college")]
    [InlineData("St. Mary's International School", "st-marys-international-school")]
    [InlineData("Dietams_School", "dietams-school")]
    public void GeneratesCanonicalHyphenatedSlug(string name, string expected) => Assert.Equal(expected, SchoolSlug.FromName(name));
}
