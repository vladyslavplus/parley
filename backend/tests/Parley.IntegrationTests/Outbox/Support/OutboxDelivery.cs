using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Parley.SharedKernel.Outbox.Dispatching;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Test control of outbox delivery in API hosts. A production host runs one background
/// dispatch loop per module, on its own clock. Test hosts share one database and one fake
/// clock, so a loop left running in any host claims rows another test is asserting on, and
/// <c>SKIP LOCKED</c> hides a row that another dispatcher is still delivering. Test hosts
/// therefore remove the loops, and tests run passes explicitly until no row is due.
/// </summary>
public static class OutboxDelivery
{
    /// <summary>Removes every module's background dispatch loop from a test host.</summary>
    public static void RemoveBackgroundLoops(IServiceCollection services)
    {
        var loops = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType is { IsGenericType: true } implementation
                && implementation.GetGenericTypeDefinition() == typeof(OutboxDispatcher<>))
            .ToArray();
        foreach (var loop in loops)
        {
            services.Remove(loop);
        }
    }

    /// <summary>Builds the dispatcher of one module against a host, for explicit passes.</summary>
    public static OutboxDispatcher<TDbContext> CreateDispatcher<TDbContext>(IServiceProvider services)
        where TDbContext : DbContext =>
        ActivatorUtilities.CreateInstance<OutboxDispatcher<TDbContext>>(services);

    /// <summary>Runs passes until no row is due, so every committed event has been delivered.</summary>
    public static async Task DeliverDueAsync<TDbContext>(IServiceProvider services, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        var dispatcher = CreateDispatcher<TDbContext>(services);
        while (await dispatcher.ProcessDueMessagesAsync(cancellationToken) > 0)
        {
        }
    }
}
