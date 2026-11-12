using Fluid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Campaigns.Contracts.Events;
using Parley.Crm.Contracts.Events;
using Parley.Identity.Contracts.Events;
using Parley.Insights.Contracts.Events;
using Parley.Notifications.Contracts.Email;
using Parley.Notifications.Features.Telegram.LinkCode;
using Parley.Notifications.Features.Telegram.Updates;
using Parley.Notifications.Inbox;
using Parley.Notifications.Inbox.Consumers;
using Parley.Notifications.Options;
using Parley.Notifications.Persistence;
using Parley.Notifications.Sending;
using Parley.Notifications.Telegram.Bot;
using Parley.Notifications.Telegram.Delivery;
using Parley.Notifications.Telegram.Link;
using Parley.Notifications.Telegram.Polling;
using Parley.Notifications.Telegram.Shared;
using Parley.Notifications.Telegram.Today;
using Parley.SharedKernel.Configuration;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Persistence.Interceptors;

namespace Parley.Notifications;

/// <summary>
/// Notifications module boundary and registration.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers the transactional email sender backed by MailKit and Fluid.
    /// </summary>
    /// <remarks>
    /// Binds the <c>Email</c> section and validates it on start. Unknown
    /// cultures fall back to <see cref="EmailOptions.DefaultCulture"/>, which
    /// must be <c>uk</c> or <c>en</c>.
    /// </remarks>
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(options => ConfigurationPlaceholders.IsSet(options.Host), "Email host is missing or still a placeholder. Set Email__Host.")
            .Validate(options => options.Port is >= EmailOptions.MinimumPort and <= EmailOptions.MaximumPort, "Email port is out of range. Set Email__Port.")
            .Validate(options => ConfigurationPlaceholders.IsSet(options.FromAddress) && EmailAddressRules.IsValid(options.FromAddress), "Email from address must be a valid email address. Set Email__FromAddress.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.FromName), "Email from name must not be empty.")
            .Validate(options => Enum.IsDefined(options.SecureSocketOption), "Email secure socket option is invalid.")
            .Validate(options => options.Timeout >= EmailOptions.MinimumTimeout && options.Timeout <= EmailOptions.MaximumTimeout, "Email timeout is out of range.")
            .Validate(options => EmailOptions.IsSupportedDefaultCulture(options.DefaultCulture), "Email default culture must be uk or en.")
            .Validate(options => string.IsNullOrWhiteSpace(options.Username) == string.IsNullOrWhiteSpace(options.Password), "Email username and password must be provided together.")
            .ValidateOnStart();

        services.AddSingleton(new FluidParser());
        services.AddSingleton<ITransactionalEmailSender, TransactionalEmailSender>();
        AddInAppNotifications(services, configuration);
        AddTelegram(services, configuration);
        return services;
    }

    /// <summary>
    /// Registers the in-app notifications: the list and the preferences, the database of the module, and the consumers of the
    /// events that notify people. Each consumer runs in its inbox transaction, so a replayed event adds nothing.
    /// </summary>
    private static void AddInAppNotifications(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .Validate(
                options => options.DefaultPageSize >= NotificationsOptionLimits.MinimumPageSize
                    && options.DefaultPageSize <= options.MaximumPageSize
                    && options.MaximumPageSize <= NotificationsOptionLimits.MaximumAllowedPageSize,
                "Notifications needs 1 <= DefaultPageSize <= MaximumPageSize <= 200.")
            .ValidateOnStart();
        services.AddOptions<NotificationsStorageOptions>()
            .Bind(configuration.GetSection(NotificationsStorageOptions.SectionName));

        services.AddDbContext<NotificationsDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditStampingInterceptor>()));

        services.AddScoped<InAppNotificationWriter>();
        services.AddHostedService<NotificationsMigrationsHostedService>();
        services.AddOutbox<NotificationsDbContext>(configuration);
        services.AddIntegrationEventConsumer<TaskAssigned, NotificationsDbContext, TaskAssignedConsumer>();
        services.AddIntegrationEventConsumer<TaskDueSoon, NotificationsDbContext, TaskDueSoonConsumer>();
        services.AddIntegrationEventConsumer<DealAssigned, NotificationsDbContext, DealAssignedConsumer>();
        services.AddIntegrationEventConsumer<CampaignCompleted, NotificationsDbContext, CampaignCompletedConsumer>();
        services.AddIntegrationEventConsumer<ScoringRunCompleted, NotificationsDbContext, ScoringRunCompletedConsumer>();
        services.AddIntegrationEventConsumer<MemberActivated, NotificationsDbContext, MemberActivatedConsumer>();
        services.AddIntegrationEventConsumer<MemberDeactivated, NotificationsDbContext, MemberDeactivatedConsumer>();
    }

    /// <summary>
    /// Registers the Telegram channel. Without a token the channel is off: nothing starts, the link endpoints answer
    /// <c>telegram.not_configured</c>, and the app runs normally. With a token, the options are validated on start, and the
    /// public web address is required because the messages link to it.
    /// </summary>
    private static void AddTelegram(IServiceCollection services, IConfiguration configuration)
    {
        var telegram = configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>() ?? new TelegramOptions();

        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .Validate(TelegramOptionRules.IsValid, TelegramOptionRules.InvalidMessage)
            .ValidateOnStart();

        services.AddOptions<NotificationsOptions>()
            .Validate(
                options => !TelegramOptionRules.IsConfigured(telegram) || IsAbsoluteWebAddress(options.PublicWebBaseUrl),
                "Notifications PublicWebBaseUrl must be an absolute http or https address while Telegram runs. Set Notifications__PublicWebBaseUrl.")
            .ValidateOnStart();

        // The Telegram client logs no request lines: the bot token is part of every request URL.
        services.AddHttpClient(TelegramApiClient.HttpClientName).RemoveAllLoggers();
        services.AddSingleton<ITelegramBotClient, TelegramApiClient>();
        services.AddSingleton<TelegramSendPacer>();
        services.AddSingleton<TelegramLinkCodeLimiter>();

        services.AddScoped<TelegramDeliveryPlanner>();
        services.AddScoped<TelegramChatLocales>();
        services.AddScoped<TelegramLinkRedeemer>();
        services.AddScoped<TelegramLinkEnder>();
        services.AddScoped<TelegramChatUnlinker>();
        services.AddScoped<TelegramTodayComposer>();
        services.AddScoped<TelegramDeliveryClaimer>();
        services.AddScoped<TelegramDeliveryAttempt>();
        services.AddScoped<TelegramUpdateRetention>();
        services.AddScoped<TelegramUpdateDispatcher>();

        services.AddSingleton<TelegramDeliverySweep>();

        services.AddHostedService<TelegramPollingWorker>();
        services.AddHostedService<TelegramDeliveryWorker>();
    }

    private static bool IsAbsoluteWebAddress(string? value) =>
        ConfigurationPlaceholders.IsSet(value)
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
