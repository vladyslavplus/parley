using Parley.SharedKernel.Persistence.Entities;

namespace Parley.IntegrationTests.Tenancy.Support;

/// <summary>
/// Test-only tenant row proving isolation and optimistic concurrency.
/// Production modules add their own rows; nothing here ships in the API.
/// </summary>
public sealed class TenantRecord : BusinessEntity
{
    public TenantRecord()
    {
    }

    public TenantRecord(Guid organizationId, string name) : base(organizationId)
    {
        Name = name;
    }

    public string Name { get; set; } = string.Empty;
}
