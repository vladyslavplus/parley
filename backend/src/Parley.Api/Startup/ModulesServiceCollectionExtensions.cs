using Parley.Crm;
using Parley.Identity;
using Parley.Notifications;

namespace Parley.Api.Startup;

public static class ModulesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Notifications, Identity and CRM modules.
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotifications(configuration);
        services.AddIdentity(configuration);
        services.AddCrm(configuration);
        return services;
    }
}
