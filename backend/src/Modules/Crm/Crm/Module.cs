using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Campaigns.Contracts.Events;
using Parley.Crm.Contracts.Audience;
using Parley.Crm.Contracts.Dashboard;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Contracts.Outcomes;
using Parley.Crm.Contracts.Priority;
using Parley.Crm.Contracts.Recipients;
using Parley.Crm.Exports;
using Parley.Crm.Features.Activities.Consumers;
using Parley.Crm.Features.Contacts.Create;
using Parley.Crm.Features.Pipelines.Consumers;
using Parley.Crm.Options;
using Parley.Crm.Persistence;
using Parley.Crm.Readers;
using Parley.Crm.Seeding.DemoSeed;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Pipelines;
using Parley.Crm.Tasks;
using Parley.Identity.Contracts.Events;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Persistence.Interceptors;

namespace Parley.Crm;

/// <summary>
/// CRM module boundary and registration: contacts, companies, tags, custom field
/// definitions and the crm schema. Identity (scope and permissions), the mediator pipeline,
/// tenancy and the data source are registered by the host.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers the CRM module. Options bind from the <c>CrmStorage</c> section. The migrations
    /// hosted service is registered before the outbox dispatcher, which reads the crm schema
    /// as soon as it starts.
    /// </summary>
    public static IServiceCollection AddCrm(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CrmStorageOptions>()
            .Bind(configuration.GetSection(CrmStorageOptions.SectionName));
        services.AddOptions<CrmExportOptions>()
            .Bind(configuration.GetSection(CrmExportOptions.SectionName));
        services.AddOptions<CrmTaskOptions>()
            .Bind(configuration.GetSection(CrmTaskOptions.SectionName));
        services.AddSingleton<ContactExportStorage>();
        services.AddScoped<ContactExportScheduler>();
        services.AddScoped<OwnerDirectory>();
        services.AddScoped<ICrmCustomerMetricsReader, CrmCustomerMetricsReader>();
        services.AddScoped<ICrmPriorityFactsReader, CrmPriorityFactsReader>();
        services.AddScoped<ICrmDashboardReader, CrmDashboardReader>();
        services.AddScoped<ICrmAudienceReader, CrmAudienceReader>();
        services.AddScoped<ICrmRecipientProfileReader, CrmRecipientProfileReader>();
        services.AddScoped<ICrmOutcomeReader, CrmOutcomeReader>();
        services.AddScoped<ICrmDemoDataImporter, CrmDemoDataImporter>();
        services.AddScoped<ICrmDemoSimulatedOrderWriter, CrmDemoSimulatedOrderWriter>();
        services.AddIdempotencyStore<CrmDbContext>();

        services.AddDbContext<CrmDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditStampingInterceptor>()));

        services.AddHostedService<CrmMigrationsHostedService>();
        services.AddScoped<DefaultPipelineProvisioner>();
        services.AddScoped<TaskDueSoonJob>();
        services.AddScoped<TaskDueSoonScanJob>();
        services.AddOutbox<CrmDbContext>(configuration);
        services.AddIntegrationEventConsumer<OrganizationCreated, CrmDbContext, OrganizationCreatedConsumer>();
        services.AddIntegrationEventConsumer<CampaignEmailSent, CrmDbContext, CampaignEmailSentConsumer>();
        services.AddIntegrationEventConsumer<RecipientUnsubscribed, CrmDbContext, RecipientUnsubscribedConsumer>();

        services.AddValidatorsFromAssemblyContaining<CreateContactCommandValidator>();

        return services;
    }
}
