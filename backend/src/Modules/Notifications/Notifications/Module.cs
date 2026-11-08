using Fluid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parley.Notifications.Contracts.Email;
using Parley.Notifications.Options;
using Parley.Notifications.Sending;
using Parley.SharedKernel.Configuration;

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
        return services;
    }
}
