using Microsoft.Extensions.Options;
using Npgsql;
using Parley.Api.Options;

namespace Parley.Api.Startup;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared PostgreSQL data source.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return NpgsqlDataSource.Create(databaseOptions.ConnectionString);
        });
        return services;
    }
}
