using Parley.Identity.Authorization.Behavior;
using Parley.SharedKernel.Mediator.Behaviors;

namespace Parley.Api.Startup;

public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and its pipeline behaviors. The assembly list stays a literal: the source generator reads it at
    /// compile time, so a list computed at run time finds no handlers. Each module with requests is listed here once.
    /// </summary>
    public static IServiceCollection AddMediatorPipeline(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Assemblies =
            [
                typeof(Identity.Module).Assembly,
                typeof(Crm.Module).Assembly,
                typeof(Campaigns.Module).Assembly,
                typeof(Insights.Module).Assembly,
                typeof(Notifications.Module).Assembly,
            ];
            options.PipelineBehaviors =
            [
                typeof(LoggingBehavior<,>),
                typeof(PermissionAuthorizationBehavior<,>),
                typeof(ValidationBehavior<,>),
                typeof(TransactionBehavior<,>),
            ];
        });
        return services;
    }
}
