using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Orders;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Crm.Metrics.Support;

/// <summary>Writes the metrics fixture into one organization through the CRM model.</summary>
public static class CustomerMetricsFixtureSeed
{
    private const string FixtureFirstName = "Fixture";

    private const string FixtureDealTitle = "Fixture deal";

    private const string FixtureCurrency = CrmDemoContract.ExportCurrency;

    private const string FixturePipelineName = "Fixture pipeline";

    private const string FixtureStageName = "Open";

    private const string FixtureEmailDomain = "example.test";

    private const int FixtureBoardPosition = 1024;

    public static async Task SeedAsync(
        IServiceProvider services,
        Guid organizationId,
        CustomerMetricsFixtureData data,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        tenant.Set(organizationId);
        try
        {
            var pipeline = new Pipeline { OrganizationId = organizationId, Name = FixturePipelineName, IsDefault = true };
            var stage = new PipelineStage
            {
                OrganizationId = organizationId,
                PipelineId = pipeline.Id,
                Name = FixtureStageName,
                Position = 0,
                Kind = PipelineStageKinds.Open,
            };
            db.Pipelines.Add(pipeline);
            db.PipelineStages.Add(stage);

            foreach (var contactId in data.ContactIds)
            {
                db.Contacts.Add(new Contact
                {
                    Id = contactId,
                    OrganizationId = organizationId,
                    FirstName = FixtureFirstName,
                    Email = $"{contactId:N}@{FixtureEmailDomain}",
                    EmailNormalized = $"{contactId:N}@{FixtureEmailDomain}",
                    Source = ContactSources.Manual,
                });
            }

            foreach (var order in data.Orders)
            {
                db.Orders.Add(new Order
                {
                    OrganizationId = organizationId,
                    ContactId = order.ContactId,
                    ExternalRef = order.OrderId,
                    OrderedAt = order.OrderedAt,
                    TotalAmount = order.TotalAmount,
                    Currency = FixtureCurrency,
                    LineCount = 1,
                    IsCancellation = order.IsCancellation,
                    Source = OrderSources.Manual,
                });
            }

            foreach (var contactId in data.OpenDealContactIds)
            {
                db.Deals.Add(new Deal
                {
                    OrganizationId = organizationId,
                    PipelineId = pipeline.Id,
                    StageId = stage.Id,
                    Title = FixtureDealTitle,
                    Amount = 1m,
                    Currency = FixtureCurrency,
                    ContactId = contactId,
                    OwnerUserId = Guid.CreateVersion7(),
                    Status = DealStatuses.Open,
                    BoardPosition = FixtureBoardPosition,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }
}
