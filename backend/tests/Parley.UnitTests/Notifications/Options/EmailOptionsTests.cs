using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Parley.Notifications;
using Parley.Notifications.Options;
using Xunit;

namespace Parley.UnitTests.Notifications.Options;

public sealed class EmailOptionsTests
{
    [Fact]
    public void AddNotifications_InvalidPort_FailsValidation()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Email:Host"] = "127.0.0.1",
            ["Email:Port"] = "0",
            ["Email:FromAddress"] = "no-reply@parley.test",
            ["Email:FromName"] = "Parley",
            ["Email:SecureSocketOption"] = "None",
            ["Email:Timeout"] = "00:00:30",
            ["Email:DefaultCulture"] = "en",
        };
        var provider = BuildProvider(configuration);
        var resolve = () => provider.GetRequiredService<IOptions<EmailOptions>>().Value;
        resolve.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void AddNotifications_ValidValues_PassValidation()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Email:Host"] = "127.0.0.1",
            ["Email:Port"] = "1025",
            ["Email:FromAddress"] = "no-reply@parley.test",
            ["Email:FromName"] = "Parley",
            ["Email:SecureSocketOption"] = "None",
            ["Email:Timeout"] = "00:00:30",
            ["Email:DefaultCulture"] = "en",
        };
        var provider = BuildProvider(configuration);
        var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
        options.Host.Should().Be("127.0.0.1");
        options.Port.Should().Be(1025);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddNotifications(configuration);
        return services.BuildServiceProvider();
    }
}
