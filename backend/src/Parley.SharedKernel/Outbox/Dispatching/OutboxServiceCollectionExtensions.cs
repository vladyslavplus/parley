using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.SharedKernel.Outbox.Dispatching;

/// <summary>
/// Wires the transactional outbox for one module database. Each module calls
/// <c>AddOutbox{ModuleDbContext}</c> once (with tenancy from
/// <c>AddTenancy</c> so the dispatcher can set the tenant per row) and
/// registers its consumers through <see cref="AddIntegrationEventConsumer"/>
/// so every registration carries the consumer's own DbContext type.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOutbox<TDbContext>(IConfiguration configuration)
            where TDbContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<OutboxOptions>()
                .Bind(configuration.GetSection(OutboxOptions.SectionName))
                .Validate(options => options.PollInterval > TimeSpan.Zero, "Outbox poll interval must be positive.")
                .Validate(options => options.BatchSize is >= OutboxOptions.MinimumBatchSize and <= OutboxOptions.MaximumBatchSize,
                    "Outbox batch size is out of range.")
                .Validate(options => options.MaxAttempts is >= OutboxOptions.MinimumMaxAttempts and <= OutboxOptions.MaximumMaxAttempts,
                    "Outbox max attempts is out of range.")
                .Validate(options => options.BaseBackoff >= TimeSpan.Zero, "Outbox base backoff must not be negative.")
                .ValidateOnStart();

            services.AddScoped<IModuleOutbox<TDbContext>>(provider => new DbContextOutbox<TDbContext>(
                provider.GetRequiredService<TDbContext>(),
                provider.GetRequiredService<ITenantContext>(),
                provider.GetRequiredService<TimeProvider>()));
            services.AddHostedService<OutboxDispatcher<TDbContext>>();
            return services;
        }

        /// <summary>
        /// Registers an integration event consumer together with the DbContext the
        /// consumer owns. The dispatcher resolves that DbContext per consumer and
        /// commits the inbox row and the consumer's work in one transaction on it.
        /// </summary>
        public IServiceCollection AddIntegrationEventConsumer<TEvent, TDbContext, TConsumer>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TEvent : IntegrationEvent
            where TDbContext : DbContext
            where TConsumer : class, IIntegrationEventConsumer<TEvent>
        {
            ArgumentNullException.ThrowIfNull(services);

            services.Add(new ServiceDescriptor(
                typeof(IIntegrationEventConsumer<TEvent>),
                typeof(TConsumer),
                lifetime));
            services.AddSingleton(new ConsumerDbContextRegistration
            {
                ConsumerType = typeof(TConsumer),
                DbContextType = typeof(TDbContext),
            });
            return services;
        }
    }
}
