namespace Parley.Api.Startup;

public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the authorization services on top of the identity authentication scheme.
    /// </summary>
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        return services;
    }
}
