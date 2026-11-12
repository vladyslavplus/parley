
namespace Parley.Api.Seeding.Demo;

/// <summary>Registers the Development demo seed options and its background service.</summary>
public static class DemoSeedServiceCollectionExtensions
{
    public static IServiceCollection AddDemoSeed(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DemoSeedOptions>()
            .Bind(configuration.GetSection(DemoSeedOptions.SectionName))
            .Validate(
                options => options.RefreshAfterDays is >= DemoSeedOptions.MinimumRefreshAfterDays and <= DemoSeedOptions.MaximumRefreshAfterDays,
                "Demo:Seed RefreshAfterDays is out of range.")
            .Validate(
                options => options.Profile.Contacts is >= DemoSeedOptions.MinimumContacts and <= DemoSeedOptions.MaximumContacts,
                "Demo:Seed Profile Contacts is out of range.")
            .Validate(
                options => options.Profile.OrderMonths is >= DemoSeedOptions.MinimumOrderMonths and <= DemoSeedOptions.MaximumOrderMonths,
                "Demo:Seed Profile OrderMonths is out of range.")
            .ValidateOnStart();

        services.AddSingleton<DemoSeedRunner>();
        services.AddHostedService<DemoSeedHostedService>();
        return services;
    }
}
