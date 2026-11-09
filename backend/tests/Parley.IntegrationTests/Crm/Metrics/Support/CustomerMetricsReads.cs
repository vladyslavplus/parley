using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.Metrics;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Crm.Metrics.Support;

/// <summary>Reads the customer metrics in batches inside one organization's tenant scope.</summary>
public static class CustomerMetricsReads
{
    public static async Task<IReadOnlyList<CrmCustomerMetricsRow>> ReadAllAsync(
        IServiceProvider services,
        Guid organizationId,
        int batchSize,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var reader = scope.ServiceProvider.GetRequiredService<ICrmCustomerMetricsReader>();
        tenant.Set(organizationId);
        try
        {
            var all = new List<CrmCustomerMetricsRow>();
            Guid? after = null;
            while (true)
            {
                var batch = await reader.ReadBatchAsync(after, batchSize, asOf, cancellationToken);
                if (batch.Count == 0)
                {
                    return all;
                }

                all.AddRange(batch);
                after = batch[^1].ContactId;
            }
        }
        finally
        {
            tenant.Clear();
        }
    }
}
