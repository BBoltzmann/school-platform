using SchoolPlatform.Application.Authentication;

namespace SchoolPlatform.UnitTests;

public sealed class AuthenticationInputTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("short1", false)]
    [InlineData("onlylettershere", false)]
    [InlineData("123456789012", false)]
    [InlineData("Long passphrase 123", true)]
    public void PasswordPolicyEnforcesRequirements(string? password, bool valid) =>
        Assert.Equal(valid, PasswordPolicy.IsValid(password));

    [Theory]
    [InlineData("antioch-college", true)]
    [InlineData("new-school", true)]
    [InlineData("../antioch-college", false)]
    [InlineData("-school", false)]
    [InlineData("school--slug", false)]
    public void SchoolSlugsAreSafe(string slug, bool valid) => Assert.Equal(valid, AuthInput.SlugIsValid(slug));
}
