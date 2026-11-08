using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Parley.Identity;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Dispatching;
using Xunit;

namespace Parley.UnitTests.Identity;

public sealed class IdentityHostedServiceOrderTests
{
    [Fact]
    public void MigrationsStartBeforeOutboxDispatcher()
    {
        var services = new ServiceCollection();
        services.AddIdentity(new ConfigurationBuilder().Build());

        var hostedServices = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Select(descriptor => descriptor.ImplementationType)
            .ToList();

        var migrations = hostedServices.IndexOf(typeof(IdentityMigrationsHostedService));
        var dispatcher = hostedServices.IndexOf(typeof(OutboxDispatcher<IdentityDbContext>));

        migrations.Should().BeGreaterThanOrEqualTo(0);
        dispatcher.Should().BeGreaterThan(migrations);
    }
}
