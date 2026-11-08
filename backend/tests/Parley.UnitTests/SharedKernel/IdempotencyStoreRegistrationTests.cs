using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Idempotency.Abstractions;
using Parley.SharedKernel.Idempotency.Persistence;
using Xunit;

namespace Parley.UnitTests.SharedKernel;

/// <summary>Each module registers its own idempotency store, so a second module never replaces the first.</summary>
public sealed class IdempotencyStoreRegistrationTests
{
    [Fact]
    public void SecondModuleRegistration_LeavesEachModuleItsOwnStore()
    {
        var services = new ServiceCollection();

        services.AddIdempotencyStore<CrmDbContext>();
        services.AddIdempotencyStore<OtherModuleContext>();

        services.Single(descriptor => descriptor.ServiceType == typeof(IModuleIdempotencyStore<CrmDbContext>))
            .ImplementationType.Should().Be(typeof(EfIdempotencyStore<CrmDbContext>));
        services.Single(descriptor => descriptor.ServiceType == typeof(IModuleIdempotencyStore<OtherModuleContext>))
            .ImplementationType.Should().Be(typeof(EfIdempotencyStore<OtherModuleContext>));
    }

    private sealed class OtherModuleContext(DbContextOptions<OtherModuleContext> options) : DbContext(options);
}
