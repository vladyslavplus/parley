using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.Campaigns.Features.Segments.Create;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates;
using Parley.Crm.Contracts.Metrics;
using Parley.Insights.Contracts.Scores;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Persistence.Interceptors;

namespace Parley.Campaigns;

/// <summary>
/// Campaigns module boundary and registration: email templates, segments and the campaigns schema. Identity
/// (permissions), the mediator pipeline, tenancy and the data source are registered by the host. The audience reader
/// comes from the CRM module through its contracts.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers the campaigns module. Options bind from the <c>CampaignsStorage</c> section. The migrations hosted
    /// service is registered before the outbox dispatcher, which reads the campaigns schema as soon as it starts.
    /// </summary>
    public static IServiceCollection AddCampaigns(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CampaignsStorageOptions>()
            .Bind(configuration.GetSection(CampaignsStorageOptions.SectionName));
        services.AddSingleton<TemplateSandbox>();

        services.AddDbContext<CampaignsDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditStampingInterceptor>()));

        services.AddHostedService<CampaignsMigrationsHostedService>();
        services.AddOutbox<CampaignsDbContext>(configuration);

        services.AddValidatorsFromAssemblyContaining<CreateSegmentCommandValidator>();

        AddCampaignOptions(services, configuration);

        // The score reader is a stub until the scoring pipeline exists; the pipeline's registration replaces it.
        services.TryAddSingleton<IScoreReader, NoScoreReader>();
        services.AddScoped<CampaignAudienceFreezer>();
        services.AddScoped<PrepareCampaignJob>();
        services.AddScoped<CampaignPrepareScheduler>();

        return services;
    }

    private static void AddCampaignOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CampaignsHoldoutOptions>()
            .Bind(configuration.GetSection(CampaignsHoldoutOptions.SectionName))
            .Validate(
                options => options.MinShareBps >= 1
                    && options.MinShareBps <= options.DefaultShareBps
                    && options.DefaultShareBps <= options.MaxShareBps
                    && options.MaxShareBps < BasisPoints.Scale,
                "Campaigns:Holdout bounds must satisfy 1 <= Min <= Default <= Max < 10000.")
            .ValidateOnStart();
        services.AddOptions<CampaignsTargetingOptions>()
            .Bind(configuration.GetSection(CampaignsTargetingOptions.SectionName))
            .Validate(
                options => options.MinShareBps >= 1 && options.MinShareBps <= options.MaxShareBps && options.MaxShareBps <= BasisPoints.Scale,
                "Campaigns:Targeting bounds must satisfy 1 <= Min <= Max <= 10000.")
            .ValidateOnStart();
        services.AddOptions<CampaignsAnalysisOptions>()
            .Bind(configuration.GetSection(CampaignsAnalysisOptions.SectionName))
            .Validate(
                options => options.SrmAlarmP > 0 && options.SrmAlarmP < 1
                    && options.ZAlpha > 0 && options.ZBeta > 0
                    && options.PreviewBaselineRateBps is >= 1 and < BasisPoints.Scale,
                "Campaigns:Analysis constants are out of range.")
            .ValidateOnStart();
        services.AddOptions<CampaignsPrepareOptions>()
            .Bind(configuration.GetSection(CampaignsPrepareOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.QueueName)
                    && options.BatchSize is >= 1 and <= CrmCustomerMetricsContract.MaximumBatchSize,
                "Campaigns:Prepare needs a queue name and a batch size between 1 and the CRM batch maximum.")
            .ValidateOnStart();
    }
}
