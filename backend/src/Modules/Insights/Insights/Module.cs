using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Identity.Contracts.Events;
using Parley.Insights.Contracts.MlStatus;
using Parley.Insights.Contracts.Priority;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Features.Daily;
using Parley.Insights.Features.MlSettings.Consumers;
using Parley.Insights.Features.MlSettings.Read;
using Parley.Insights.Features.Priority.Build;
using Parley.Insights.Features.Priority.List;
using Parley.Insights.Features.Priority.Read;
using Parley.Insights.Features.Rfm.Compute;
using Parley.Insights.Features.Rfm.Segments;
using Parley.Insights.Features.Scores;
using Parley.Insights.Options;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Persistence.Interceptors;

namespace Parley.Insights;

/// <summary>
/// Insights module boundary and registration: RFM snapshots, the priority list, the ML switch and the dashboard aggregate.
/// Identity (scope and permissions), the mediator pipeline, tenancy and the data source are registered by the host.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers the insights module. Options bind from the <c>Insights</c> sections and are validated at start. The
    /// migrations hosted service is registered before the outbox dispatcher, which reads the insights schema as soon as it
    /// starts.
    /// </summary>
    public static IServiceCollection AddInsights(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddInsightsOptions(services, configuration);
        services.AddMemoryCache();

        services.AddScoped<IScoreReader, InsightsScoreReader>();
        services.AddScoped<IRfmSegmentReader, InsightsRfmSegmentReader>();
        services.AddScoped<IPriorityReader, InsightsPriorityReader>();
        services.AddScoped<IMlStatusReader, InsightsMlStatusReader>();
        services.AddScoped<RfmSnapshotRunner>();
        services.AddScoped<PriorityBuildRunner>();
        services.AddSingleton<InsightsDailyLock>();
        services.AddScoped<InsightsOrganizationDailyJob>();
        services.AddScoped<InsightsDailySweepJob>();
        services.AddScoped<InsightsDailyScheduler>();
        services.AddIdempotencyStore<InsightsDbContext>();

        services.AddDbContext<InsightsDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditStampingInterceptor>()));

        services.AddHostedService<InsightsMigrationsHostedService>();
        services.AddOutbox<InsightsDbContext>(configuration);
        services.AddIntegrationEventConsumer<OrganizationCreated, InsightsDbContext, OrganizationCreatedConsumer>();

        services.AddValidatorsFromAssemblyContaining<ListPriorityQueryValidator>();

        return services;
    }

    private static void AddInsightsOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InsightsPriorityOptions>()
            .Bind(configuration.GetSection(InsightsPriorityOptions.SectionName))
            .Validate(InsightsOptionRules.PriorityIsValid, "Insights:Priority needs a weight for every scored segment, in order, and valid bonuses, windows and list sizes.")
            .ValidateOnStart();
        services.AddOptions<InsightsDashboardOptions>()
            .Bind(configuration.GetSection(InsightsDashboardOptions.SectionName))
            .Validate(InsightsOptionRules.DashboardIsValid, "Insights:Dashboard needs a cache lifetime between 30 and 60 seconds.")
            .ValidateOnStart();
        services.AddOptions<InsightsJobOptions>()
            .Bind(configuration.GetSection(InsightsJobOptions.SectionName))
            .Validate(InsightsOptionRules.JobsAreValid, "Insights:Jobs needs a queue name.")
            .ValidateOnStart();
        services.AddOptions<InsightsStorageOptions>()
            .Bind(configuration.GetSection(InsightsStorageOptions.SectionName));
    }
}
