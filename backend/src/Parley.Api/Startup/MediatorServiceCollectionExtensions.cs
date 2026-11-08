using Parley.Identity.Authorization.Behavior;
using Parley.SharedKernel.Mediator.Behaviors;

namespace Parley.Api.Startup;

public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and its pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddMediatorPipeline(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Assemblies = [typeof(Parley.Identity.Module).Assembly];
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
