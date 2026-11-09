using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Outbox.Publishing;

/// <summary>
/// <see cref="IOutbox"/> implementation appending to the calling scope's
/// <typeparamref name="TDbContext"/>. The organization and the occurred-at
/// instant are stamped from the current scope: the tenant is read from
/// <c>ITenantContext</c> (fail-closed, an unset tenant throws
/// <see cref="TenantContextNotSetException"/>) and the timestamp from
/// <c>TimeProvider</c>.
/// </summary>
public sealed class DbContextOutbox<TDbContext>(TDbContext context, ITenantContext tenantContext, TimeProvider timeProvider) : IModuleOutbox<TDbContext>
    where TDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public void Add<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var organizationId = tenantContext.OrganizationId;
        var occurredAt = DateTime.SpecifyKind(timeProvider.GetUtcNow().UtcDateTime, DateTimeKind.Utc);
        var payload = JsonSerializer.Serialize(integrationEvent, JsonOptions);

        context.Set<OutboxMessage>().Add(new OutboxMessage(
            organizationId,
            typeof(TEvent).FullName ?? typeof(TEvent).Name,
            payload,
            occurredAt,
            occurredAt));
    }
}
