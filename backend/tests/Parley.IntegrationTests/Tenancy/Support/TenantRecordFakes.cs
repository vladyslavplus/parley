using Bogus;

namespace Parley.IntegrationTests.Tenancy.Support;

/// <summary>
/// Bogus builder for test rows.
/// </summary>
public static class TenantRecordFakes
{
    public static Faker<TenantRecord> For(Guid organizationId) =>
        new Faker<TenantRecord>()
            .CustomInstantiator(faker => new TenantRecord(organizationId, faker.Random.Word().ToUpperInvariant() + " " + faker.Random.Word().ToUpperInvariant()));
}
