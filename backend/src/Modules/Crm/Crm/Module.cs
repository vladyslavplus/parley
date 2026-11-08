using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Crm.Exports;
using Parley.Crm.Features.Contacts.Create;
using Parley.Crm.Options;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Persistence.Interceptors;

namespace Parley.Crm;

/// <summary>
/// CRM module boundary and registration: contacts, companies, tags, custom field
/// definitions and the crm schema. Identity (scope and permissions), the mediator pipeline,
/// tenancy and the data source are registered by the host.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers the CRM module. Options bind from the <c>CrmStorage</c> section. The migrations
    /// hosted service is registered before the outbox dispatcher, which reads the crm schema
    /// as soon as it starts.
    /// </summary>
    public static IServiceCollection AddCrm(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CrmStorageOptions>()
            .Bind(configuration.GetSection(CrmStorageOptions.SectionName));
        services.AddOptions<CrmExportOptions>()
            .Bind(configuration.GetSection(CrmExportOptions.SectionName));
        services.AddSingleton<ContactExportStorage>();
        services.AddScoped<ContactExportScheduler>();
        services.AddIdempotencyStore<CrmDbContext>();

        services.AddDbContext<CrmDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditStampingInterceptor>()));

        services.AddHostedService<CrmMigrationsHostedService>();
        services.AddOutbox<CrmDbContext>(configuration);

        services.AddValidatorsFromAssemblyContaining<CreateContactCommandValidator>();

        return services;
    }
}
