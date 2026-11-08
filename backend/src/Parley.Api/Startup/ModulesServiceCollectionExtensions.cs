using Parley.Identity;
using Parley.Notifications;

namespace Parley.Api.Startup;

public static class ModulesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Notifications and Identity modules.
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotifications(configuration);
        services.AddIdentity(configuration);
        return services;
    }
}
