using AwesomeAssertions;
using Parley.Identity.Options.Auth;
using Xunit;

namespace Parley.UnitTests.Identity.Options;

public sealed class AuthOptionsValidatorTests
{
    private const string ProductionKey = "a-real-secret-key-with-at-least-32-bytes-0123456789";

    private const string PlaceholderKey = "<auth_signing_key>";

    [Fact]
    public void EmptySigningKey_IsRejected()
    {
        var validator = new AuthOptionsValidator();
        var options = new AuthOptions { SigningKey = string.Empty };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*signing key is missing*");
    }

    [Fact]
    public void PlaceholderSigningKey_IsRejected()
    {
        var validator = new AuthOptionsValidator();
        var options = new AuthOptions { SigningKey = PlaceholderKey };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*signing key is missing*");
    }

    [Fact]
    public void SecretKey_IsAccepted()
    {
        var validator = new AuthOptionsValidator();
        var options = new AuthOptions { SigningKey = ProductionKey, PublicBaseUrl = "https://app.parley.test" };

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }
}
