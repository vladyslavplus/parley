using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Parley.SharedKernel.Persistence.Entities;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Persistence.Interceptors;

/// <summary>
/// Stamps <c>created_at</c> and <c>updated_at</c> through <see cref="TimeProvider"/>
/// on save, and validates the tenant for tenant writes. An unset tenant throws
/// <see cref="TenantContextNotSetException"/>; an entity written under a
/// different organization throws <see cref="TenantMismatchException"/>.
/// </summary>
public sealed class AuditStampingInterceptor(
    TimeProvider timeProvider,
    ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in context.ChangeTracker.Entries<BusinessEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.OrganizationId == Guid.Empty)
                    {
                        entry.Entity.OrganizationId = tenantContext.OrganizationId;
                    }
                    else if (entry.Entity.OrganizationId != tenantContext.OrganizationId)
                    {
                        throw new TenantMismatchException();
                    }

                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    if (entry.Entity.OrganizationId != tenantContext.OrganizationId)
                    {
                        throw new TenantMismatchException();
                    }

                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Deleted:
                    if (entry.Entity.OrganizationId != tenantContext.OrganizationId)
                    {
                        throw new TenantMismatchException();
                    }

                    break;
            }
        }
    }
}
