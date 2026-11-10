using Parley.Campaigns;
using Parley.Crm;
using Parley.Identity;
using Parley.Insights;
using Parley.Notifications;

namespace Parley.Api.Startup;

public static class ModulesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Notifications, Identity, CRM, campaigns and insights modules.
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotifications(configuration);
        services.AddIdentity(configuration);
        services.AddCrm(configuration);
        services.AddCampaigns(configuration);
        services.AddInsights(configuration);
        return services;
    }
}
