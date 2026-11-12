using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Campaigns.Contracts.DemoSeed;
using Parley.Campaigns.Contracts.Results;
using Parley.Campaigns.Features.Campaigns.Attribution;
using Parley.Campaigns.Features.Campaigns.DemoSeed;
using Parley.Campaigns.Features.Campaigns.Dispatch.Claims;
using Parley.Campaigns.Features.Campaigns.Dispatch.Job;
using Parley.Campaigns.Features.Campaigns.Dispatch.Outcomes;
using Parley.Campaigns.Features.Campaigns.Dispatch.Pacing;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.Campaigns.Features.Campaigns.Results;
using Parley.Campaigns.Features.Campaigns.Start;
using Parley.Campaigns.Features.Campaigns.TestSend;
using Parley.Campaigns.Features.Segments.Create;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Sending.Mail;
using Parley.Campaigns.Sending.Messages;
using Parley.Campaigns.Sending.Unsubscribe;
using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates;
using Parley.Crm.Contracts.Metrics;
using Parley.SharedKernel.Configuration;
using Parley.SharedKernel.Idempotency.Persistence;
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
        services.AddIdempotencyStore<CampaignsDbContext>();

        services.AddValidatorsFromAssemblyContaining<CreateSegmentCommandValidator>();

        AddCampaignOptions(services, configuration);

        // The score reader is the insights module's; the campaigns module only reads through its contract.
        services.AddScoped<CampaignAudienceFreezer>();
        services.AddScoped<PrepareCampaignJob>();
        services.AddScoped<CampaignPrepareScheduler>();
        services.AddScoped<CampaignPrepareRecoveryJob>();

        // Attribution and results. The reader is the module's public contract for the stored result.
        services.AddScoped<CampaignAttributionRunner>();
        services.AddScoped<CampaignAttributionJob>();
        services.AddScoped<CampaignAttributionSweepJob>();
        services.AddScoped<CampaignAttributionScheduler>();
        services.AddScoped<ICampaignResultsReader, CampaignResultsReader>();

        // Demo seed only: the historic campaigns, and the sender that the send runner uses for their sends instead of SMTP.
        services.AddScoped<ICampaignDemoSeeder, CampaignDemoSeeder>();
        services.AddSingleton<DemoCampaignMailSender>();

        // Send pipeline. The pacer is a singleton: its gate spaces the sends of every job in this process.
        services.AddSingleton<ICampaignMailSender, MailKitCampaignMailSender>();
        services.AddSingleton<CampaignSendPacer>();
        services.AddSingleton<CampaignTestSendLimiter>();
        services.AddSingleton<CampaignMessageRenderer>();
        services.AddSingleton<UnsubscribeTokenCodec>();
        services.AddScoped<CampaignSendClaimer>();
        services.AddScoped<CampaignSendRecorder>();
        services.AddScoped<CampaignSendCompletion>();
        services.AddScoped<CampaignSendRunner>();
        services.AddScoped<CampaignSendJob>();
        services.AddScoped<CampaignSendScheduler>();
        services.AddScoped<CampaignSendRecoveryJob>();

        return services;
    }

    private static bool IsAbsoluteWebAddress(string value) =>
        ConfigurationPlaceholders.IsSet(value)
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

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
            .Validate(
                options => options.RecoveryInterval >= CampaignsPrepareOptions.MinimumRecoveryInterval
                    && options.RecoveryInterval <= CampaignsPrepareOptions.MaximumRecoveryInterval
                    && options.RecoveryInterval.Ticks % TimeSpan.TicksPerMinute == 0,
                "Campaigns:Prepare RecoveryInterval must be a whole number of minutes between 1 and 60.")
            .Validate(
                options => options.RecoveryAge >= TimeSpan.Zero && options.RecoveryAge <= CampaignsPrepareOptions.MaximumRecoveryAge,
                "Campaigns:Prepare RecoveryAge is out of range.")
            .ValidateOnStart();
        services.AddOptions<CampaignsSendOptions>()
            .Bind(configuration.GetSection(CampaignsSendOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.QueueName)
                    && options.BatchSize is >= CampaignsSendOptions.MinimumBatchSize and <= CrmCustomerMetricsContract.MaximumBatchSize,
                "Campaigns:Send needs a queue name and a batch size between 1 and the CRM batch maximum.")
            .Validate(
                options => options.MaxAttempts is >= 1 and <= CampaignsSendOptions.MaximumMailAttempts,
                "Campaigns:Send MaxAttempts is out of range.")
            .Validate(
                options => options.RatePerSecond is >= CampaignsSendOptions.MinimumRatePerSecond and <= CampaignsSendOptions.MaximumRatePerSecond,
                "Campaigns:Send RatePerSecond is out of range.")
            .Validate(
                options => TimeSpan.FromSeconds((double)options.BatchSize / options.RatePerSecond) <= options.LeaseDuration / 2,
                "Campaigns:Send needs BatchSize / RatePerSecond below half of LeaseDuration, so a batch's pacing fits inside its lease.")
            .Validate(
                options => options.LeaseDuration >= CampaignsSendOptions.MinimumLeaseDuration && options.LeaseDuration <= CampaignsSendOptions.MaximumLeaseDuration,
                "Campaigns:Send LeaseDuration is out of range.")
            .Validate(
                options => ConfigurationPlaceholders.IsSet(options.MessageIdDomain)
                    && options.MessageIdDomain.Length <= CampaignsSendOptions.MaximumMessageIdDomainLength,
                "Campaigns:Send MessageIdDomain must be set. Set Campaigns__Send__MessageIdDomain.")
            .Validate(
                options => ConfigurationPlaceholders.IsSet(options.UnsubscribeTokenKey)
                    && options.UnsubscribeTokenKey.Length >= CampaignsSendOptions.MinimumUnsubscribeTokenKeyLength,
                "Campaigns:Send UnsubscribeTokenKey must be set and at least 32 characters. Set Campaigns__Send__UnsubscribeTokenKey.")
            .Validate(
                options => options.UnsubscribeTokenLifetime > TimeSpan.Zero
                    && options.UnsubscribeTokenLifetime <= CampaignsSendOptions.MaximumUnsubscribeTokenLifetime,
                "Campaigns:Send UnsubscribeTokenLifetime is out of range.")
            .Validate(
                options => IsAbsoluteWebAddress(options.PublicApiBaseUrl),
                "Campaigns:Send PublicApiBaseUrl must be an absolute http or https address. Set Campaigns__Send__PublicApiBaseUrl.")
            .Validate(
                options => IsAbsoluteWebAddress(options.PublicWebBaseUrl),
                "Campaigns:Send PublicWebBaseUrl must be an absolute http or https address. Set Campaigns__Send__PublicWebBaseUrl.")
            .Validate(
                options => options.RetryBackoff >= TimeSpan.Zero && options.RetryBackoff <= CampaignsSendOptions.MaximumRetryBackoff,
                "Campaigns:Send RetryBackoff is out of range.")
            .Validate(
                options => options.RecoveryInterval >= CampaignsSendOptions.MinimumRecoveryInterval
                    && options.RecoveryInterval <= CampaignsSendOptions.MaximumRecoveryInterval
                    && options.RecoveryInterval.Ticks % TimeSpan.TicksPerMinute == 0,
                "Campaigns:Send RecoveryInterval must be a whole number of minutes between 1 and 60.")
            .Validate(
                options => options.UnsubscribePermitLimit is >= 1 and <= CampaignsSendOptions.MaximumUnsubscribePermitLimit
                    && options.UnsubscribeWindow >= TimeSpan.FromSeconds(1)
                    && options.UnsubscribeWindow <= CampaignsSendOptions.MaximumUnsubscribeWindow,
                "Campaigns:Send unsubscribe rate limit is out of range.")
            .Validate(
                options => options.TestSendPermitLimit is >= 1 and <= CampaignsSendOptions.MaximumTestSendPermitLimit
                    && options.TestSendWindow >= TimeSpan.FromSeconds(1)
                    && options.TestSendWindow <= CampaignsSendOptions.MaximumTestSendWindow,
                "Campaigns:Send test-send rate limit is out of range.")
            .ValidateOnStart();
        services.AddOptions<CampaignsAttributionOptions>()
            .Bind(configuration.GetSection(CampaignsAttributionOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.QueueName),
                "Campaigns:Attribution needs a queue name.")
            .ValidateOnStart();
        services.AddOptions<CampaignsMailOptions>()
            .Bind(configuration.GetSection(CampaignsMailOptions.SectionName))
            .Validate(
                options => ConfigurationPlaceholders.IsSet(options.Host),
                "Email host is missing or still a placeholder. Set Email__Host.")
            .Validate(
                options => options.Port is >= CampaignsMailOptions.MinimumPort and <= CampaignsMailOptions.MaximumPort,
                "Email port is out of range. Set Email__Port.")
            .Validate(
                options => ConfigurationPlaceholders.IsSet(options.FromAddress),
                "Email from address is missing or still a placeholder. Set Email__FromAddress.")
            .Validate(
                options => string.IsNullOrWhiteSpace(options.Username) == string.IsNullOrWhiteSpace(options.Password),
                "Email username and password must be provided together.")
            .ValidateOnStart();
    }
}
