using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Parley.Identity.Contracts.Events;
using Parley.Insights.Contracts.DemoSeed;
using Parley.Insights.Contracts.Events;
using Parley.Insights.Contracts.MlStatus;
using Parley.Insights.Contracts.Priority;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Features.Daily;
using Parley.Insights.Features.DemoSeed;
using Parley.Insights.Features.MlSettings.Consumers;
using Parley.Insights.Features.MlSettings.Read;
using Parley.Insights.Features.Priority.Build;
using Parley.Insights.Features.Priority.List;
using Parley.Insights.Features.Priority.Read;
using Parley.Insights.Features.Priority.Rebuild;
using Parley.Insights.Features.Rfm.Compute;
using Parley.Insights.Features.Rfm.Segments;
using Parley.Insights.Features.Scores;
using Parley.Insights.Features.Scoring.Client;
using Parley.Insights.Features.Scoring.Models;
using Parley.Insights.Features.Scoring.Run;
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
        services.AddScoped<IDemoInsightsSeeder, DemoInsightsSeeder>();
        services.AddSingleton<InsightsDailyLock>();
        services.AddScoped<InsightsOrganizationDailyJob>();
        services.AddScoped<InsightsDailySweepJob>();
        services.AddScoped<InsightsDailyScheduler>();
        services.AddScoped<ScoringRunRunner>();
        services.AddScoped<RunScoringJob>();
        services.AddScoped<ScoringRunScheduler>();
        services.AddScoped<InsightsPriorityRebuildJob>();
        services.AddScoped<PriorityRebuildScheduler>();
        services.AddScoped<ScoringModelsSync>();
        services.AddScoped<ScoringModelsSyncJob>();
        services.AddScoped<ScoringModelsSyncScheduler>();
        services.AddScoped<ScoringRunAbandonment>();
        services.AddScoped<ScoringRunSweepJob>();
        AddScoringClient(services, configuration);
        services.AddIdempotencyStore<InsightsDbContext>();

        services.AddDbContext<InsightsDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditStampingInterceptor>()));

        services.AddHostedService<InsightsMigrationsHostedService>();
        services.AddOutbox<InsightsDbContext>(configuration);
        services.AddIntegrationEventConsumer<OrganizationCreated, InsightsDbContext, OrganizationCreatedConsumer>();
        services.AddIntegrationEventConsumer<ScoringRunCompleted, InsightsDbContext, ScoringRunCompletedRebuildConsumer>();
        services.AddIntegrationEventConsumer<MlSettingsChanged, InsightsDbContext, MlSettingsChangedRebuildConsumer>();

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
        services.AddOptions<InsightsScoringOptions>()
            .Bind(configuration.GetSection(InsightsScoringOptions.SectionName))
            .Validate(InsightsOptionRules.ScoringIsValid, "Insights:Scoring needs an absolute http or https base address, valid timeouts, a batch of at most 1000, a rejected share between 0 and 1, a horizon of at most 3650 days and a queue name.")
            .ValidateOnStart();
    }

    /// <summary>
    /// The typed client of the scoring service. The standard resilience handler bounds each attempt and the whole call, and
    /// opens a circuit after repeated failures. The circuit's sampling window is never shorter than two attempts, as the
    /// handler requires. The timeouts are read from the configuration here, because the handler is configured at registration.
    /// </summary>
    private static void AddScoringClient(IServiceCollection services, IConfiguration configuration)
    {
        var scoring = configuration.GetSection(InsightsScoringOptions.SectionName).Get<InsightsScoringOptions>() ?? new InsightsScoringOptions();
        var attempt = TimeSpan.FromSeconds(scoring.AttemptTimeoutSeconds);
        var samplingWindow = TimeSpan.FromSeconds(Math.Max(scoring.AttemptTimeoutSeconds * 2, DefaultCircuitSamplingSeconds));
        services.AddHttpClient<ScoringServiceClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<InsightsScoringOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

                // The resilience handler owns the timeouts; the client itself does not time out first.
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.AttemptTimeout.Timeout = attempt;
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(scoring.TotalTimeoutSeconds);
                resilience.CircuitBreaker.SamplingDuration = samplingWindow;
            });
    }

    /// <summary>The sampling window of the circuit breaker when the attempt timeout is short: the handler's own default.</summary>
    private const int DefaultCircuitSamplingSeconds = 30;
}
