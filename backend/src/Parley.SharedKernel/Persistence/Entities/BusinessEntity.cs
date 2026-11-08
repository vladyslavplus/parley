using Parley.SharedKernel.Persistence.Interceptors;

namespace Parley.SharedKernel.Persistence.Entities;

/// <summary>
/// Base for every tenant row. Identifiers are UUIDv7; timestamps are stamped by
/// <see cref="AuditStampingInterceptor"/> through <see cref="TimeProvider"/>.
/// Platform rows that have no tenant do not inherit from this type.
/// </summary>
public abstract class BusinessEntity()
{
    private static readonly DateTime UtcMinValue = new(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected BusinessEntity(Guid organizationId) : this()
    {
        OrganizationId = organizationId;
    }

    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid OrganizationId { get; set; }

    public DateTime CreatedAt { get; set; } = UtcMinValue;

    public DateTime UpdatedAt { get; set; } = UtcMinValue;
}
