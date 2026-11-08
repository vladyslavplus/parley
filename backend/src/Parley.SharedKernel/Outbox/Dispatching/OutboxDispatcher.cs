using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.SharedKernel.Logging;
using Parley.SharedKernel.Observability;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.SharedKernel.Outbox.Dispatching;

/// <summary>
/// Reliable cross-module delivery for one module database. Runs as a
/// <see cref="BackgroundService"/> per module, parameterised by the producer
/// module <typeparamref name="TDbContext"/>:
///
/// 1. A <see cref="PeriodicTimer"/> built from <c>TimeProvider</c> with the
///    configured poll interval triggers each pass (tests use a fake clock).
///    A failing pass is logged and the loop continues; one bad pass never
///    stops the host.
/// 2. Each pass claims due rows one at a time with
///    <c>SELECT ... FOR UPDATE SKIP LOCKED</c>: two dispatcher instances
///    never claim the same row. The claim transaction spans delivery, so the
///    row lock is held until the outcome commits.
/// 3. Consumers are invoked in-process for the resolved event type. The
///    per-message execution runs under the current organization (taken from
///    the row). Every consumer owns its DbContext: the dispatcher runs one
///    transaction on that DbContext (inbox insert plus consumer work plus
///    commit). Only after that commit the dispatcher marks the producer row
///    processed in the claim transaction. A crash between the two redelivers
///    the row, and the consumer inbox primary key turns the replay into a
///    no-op. When producer and consumer share one DbContext there is a single
///    transaction instead. Delivery is at-least-once; at-least-once delivery
///    plus an idempotent inbox is effectively once because consumers do
///    database work only, never external I/O.
/// 4. On failure the claim transaction stays open while the consumer
///    transaction rolls back its own writes, and the failure is recorded in
///    the claim transaction itself (the row lock is never released before
///    the outcome is recorded). The row is rescheduled with exponential
///    backoff; after the max-attempts cap it stops retrying as a dead
///    letter with the last error preserved.
///
/// Idle passes cost one indexed claim query, so the overhead while idle is a
/// single half-second-or-configured-interval database read.
/// </summary>
public sealed class OutboxDispatcher<TDbContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    IMutableTenantContext tenantContext,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher<TDbContext>> logger,
    IEnumerable<ConsumerDbContextRegistration> registrations) : BackgroundService
    where TDbContext : DbContext
{
    private const int MaxLastErrorLength = 512;

    /// <summary>
    /// Savepoint taken right after the claim. A failed statement aborts the
    /// PostgreSQL transaction; rolling back to this savepoint makes the claim
    /// transaction usable again so the failure can be recorded inside it.
    /// </summary>
    private const string ClaimSavepointName = "outbox_claim";

    private const double JitterFraction = 0.25;

    private const string ActivityName = "outbox.delivery";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly ConcurrentDictionary<string, Type> EventTypeCache = new();

    private readonly OutboxOptions _options = options.Value;

    private readonly IReadOnlyDictionary<Type, Type> _consumerDbContexts = BuildConsumerMap(registrations);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollInterval, timeProvider);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueMessagesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Outbox dispatcher pass failed.");
                }

                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Delivers up to <c>BatchSize</c> due messages. Returns how many rows
    /// were handled (processed or scheduled for retry). Tests call this
    /// directly for a deterministic pass; the background loop schedules it
    /// from <see cref="ExecuteAsync"/>.
    /// </summary>
    public async Task<int> ProcessDueMessagesAsync(CancellationToken cancellationToken = default)
    {
        var handled = 0;
        for (var index = 0; index < _options.BatchSize; index++)
        {
            var hadRow = false;
            try
            {
                hadRow = await ProcessSingleDueMessageAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox dispatcher message attempt failed.");
                // Stop this pass: an unexpected failure must not be retried in a
                // hot loop against the same row; the next pass starts fresh.
                break;
            }

            if (!hadRow)
            {
                break;
            }

            handled++;
        }

        return handled;
    }

    private async Task<bool> ProcessSingleDueMessageAsync(CancellationToken cancellationToken)
    {
        // The execution strategy lives for the whole attempt so a transient
        // failure retries the whole message with a fresh scope; consumers
        // run inside the retried transaction, so retrying is apply-once.
        using var strategyScope = scopeFactory.CreateScope();
        var strategyDb = strategyScope.ServiceProvider.GetRequiredService<TDbContext>();
        var strategy = strategyDb.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var nowUtc = UtcNow();
            var message = await ClaimDueMessageAsync(db, nowUtc, cancellationToken);
            if (message is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CreateSavepointAsync(ClaimSavepointName, cancellationToken);

            // The row's organization stays ambient for the whole attempt: consumer
            // queries, consumer saves and the outcome save all read the tenant on
            // SaveChanges through the audit interceptor, so it is cleared only after
            // the outcome has been decided.
            tenantContext.Set(message.OrganizationId);
            try
            {
                return await DeliverOrRecordOutcomeAsync(
                    scope.ServiceProvider, db, transaction, message, nowUtc, cancellationToken);
            }
            finally
            {
                tenantContext.Clear();
            }
        });
    }

    private async Task<bool> DeliverOrRecordOutcomeAsync(
        IServiceProvider provider,
        TDbContext db,
        IDbContextTransaction transaction,
        OutboxMessage message,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await DeliverAsync(provider, db, message, nowUtc, cancellationToken);
            message.ProcessedAt = nowUtc;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Delivered outbox message {MessageId} of type {MessageType} for organization {OrganizationId}.",
                message.Id, message.Type, message.OrganizationId);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception exception)
        {
            // Consumer transactions for other DbContexts already rolled back their
            // own writes; same-context work pending on this context is discarded by
            // clearing the tracker. The failure is then recorded in this same claim
            // transaction, so the row lock is held until the outcome commits and no
            // other dispatcher can race the attempt count.
            // A failed statement aborted the claim transaction: undo it back to the
            // claim savepoint so the outcome is recorded in the same transaction and
            // the row lock is kept until that commit.
            await transaction.RollbackToSavepointAsync(ClaimSavepointName, cancellationToken);
            db.ChangeTracker.Clear();
            message.ProcessedAt = null;
            db.Set<OutboxMessage>().Attach(message);
            var failureNow = UtcNow();
            message.Attempts++;
            var errorText = Truncate(
                $"{exception.GetType().Name}: {PersonalDataMasking.Mask(exception.Message)}",
                MaxLastErrorLength);
            if (message.Attempts >= _options.MaxAttempts)
            {
                // Dead letter: stops retrying; the last error is preserved.
                message.DeadLetteredAt = failureNow;
                message.LastError = errorText;
                logger.LogWarning(
                    "Outbox message {MessageId} of type {MessageType} for organization {OrganizationId} exhausted {Attempts} attempts and stopped retrying. Last error: {LastError}",
                    message.Id, message.Type, message.OrganizationId, message.Attempts, errorText);
            }
            else
            {
                message.NextAttemptAt = failureNow + ComputeBackoff(message.Attempts);
                message.LastError = errorText;
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
    }

    private async Task DeliverAsync(
        IServiceProvider provider,
        TDbContext db,
        OutboxMessage message,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var eventType = ResolveEventType(message.Type) ?? throw new InvalidOperationException(
            $"Unknown integration event type '{message.Type}'.");

        IntegrationEvent integrationEvent;
        try
        {
            integrationEvent = (IntegrationEvent)JsonSerializer.Deserialize(message.Payload, eventType, JsonOptions)!;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Cannot deserialize integration event '{message.Type}'.", exception);
        }

        var consumerInterfaceType = typeof(IIntegrationEventConsumer<>).MakeGenericType(eventType);
        var consumers = provider.GetServices(consumerInterfaceType).ToList();

        using var activity = OrganizationTelemetry.StartActivity(ActivityName, message.OrganizationId);
        foreach (var consumer in consumers)
        {
            if (consumer is null)
            {
                continue;
            }

            var consumerName = consumer.GetType().FullName ?? consumer.GetType().Name;
            if (!_consumerDbContexts.TryGetValue(consumer.GetType(), out var consumerDbType))
            {
                throw new InvalidOperationException(
                    $"Integration event consumer '{consumerName}' is not registered with a DbContext. " +
                    "Register it with AddIntegrationEventConsumer so its work is not silently dropped.");
            }

            if (consumerDbType == typeof(TDbContext))
            {
                var alreadyApplied = await db.Set<InboxMessage>().AnyAsync(
                    entry => entry.Consumer == consumerName && entry.EventId == integrationEvent.EventId,
                    cancellationToken);
                if (alreadyApplied)
                {
                    continue;
                }

                db.Set<InboxMessage>().Add(new InboxMessage(consumerName, integrationEvent.EventId, nowUtc));
                await InvokeConsumerAsync(consumer, integrationEvent, cancellationToken);
                continue;
            }

            var consumerDb = (DbContext)provider.GetRequiredService(consumerDbType);
            await using var consumerTransaction = await consumerDb.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var alreadyApplied = await consumerDb.Set<InboxMessage>().AnyAsync(
                    entry => entry.Consumer == consumerName && entry.EventId == integrationEvent.EventId,
                    cancellationToken);
                if (alreadyApplied)
                {
                    await consumerTransaction.CommitAsync(cancellationToken);
                    continue;
                }

                consumerDb.Set<InboxMessage>().Add(new InboxMessage(consumerName, integrationEvent.EventId, nowUtc));
                await InvokeConsumerAsync(consumer, integrationEvent, cancellationToken);
                await consumerDb.SaveChangesAsync(cancellationToken);
                await consumerTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await consumerTransaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private static async Task<OutboxMessage?> ClaimDueMessageAsync(
        TDbContext db,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var entityType = db.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException(
                "OutboxMessage is not mapped in this DbContext. Call AddOutboxConventions() in OnModelCreating.");
        var tableName = entityType.GetTableName() ?? OutboxTableNames.OutboxMessages;
        var schema = entityType.GetSchema() ?? db.Model.GetDefaultSchema()
            ?? throw new InvalidOperationException("The outbox schema could not be resolved from the model.");

        // Identifiers come from the trusted EF model metadata; values are
        // always parameters. SKIP LOCKED makes concurrent dispatchers claim
        // disjoint rows, and the transaction keeps the lock until delivery
        // commits.
        var sql = $"SELECT * FROM \"{schema}\".\"{tableName}\" " +
            "WHERE \"processed_at\" IS NULL AND \"dead_lettered_at\" IS NULL AND \"next_attempt_at\" <= @p0 " +
            "ORDER BY \"next_attempt_at\", \"occurred_at\" LIMIT 1 FOR UPDATE SKIP LOCKED";
        var rows = await db.Set<OutboxMessage>().FromSqlRaw(sql, nowUtc).ToListAsync(cancellationToken);
        return rows.Count > 0 ? rows[0] : null;
    }

    private static IReadOnlyDictionary<Type, Type> BuildConsumerMap(
        IEnumerable<ConsumerDbContextRegistration> registrations)
    {
        var map = new Dictionary<Type, Type>();
        foreach (var registration in registrations)
        {
            if (!typeof(DbContext).IsAssignableFrom(registration.DbContextType))
            {
                throw new InvalidOperationException(
                    $"Consumer DbContext '{registration.DbContextType.FullName}' for consumer '{registration.ConsumerType.FullName}' is not a DbContext.");
            }

            map[registration.ConsumerType] = registration.DbContextType;
        }

        return map;
    }

    private static Type? ResolveEventType(string typeName)
    {
        if (EventTypeCache.TryGetValue(typeName, out var cached))
        {
            return cached;
        }

        var resolved = ResolveEventTypeUncached(typeName);
        if (resolved is not null)
        {
            // Factory may run twice for one key; both threads observe an
            // equivalent type and only one entry is stored.
            EventTypeCache.TryAdd(typeName, resolved);
        }

        return resolved;
    }

    private static Type? ResolveEventTypeUncached(string typeName)
    {
        var type = Type.GetType(typeName, throwOnError: false);
        if (type is not null && typeof(IntegrationEvent).IsAssignableFrom(type))
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                type = assembly.GetType(typeName, throwOnError: false);
            }
            catch (TypeLoadException)
            {
                type = null;
            }

            if (type is not null && typeof(IntegrationEvent).IsAssignableFrom(type))
            {
                return type;
            }
        }

        return null;
    }

    private static async Task InvokeConsumerAsync(object consumer, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var consumerInterfaceType = consumer.GetType().GetInterfaces()
            .First(interfaceType => interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IIntegrationEventConsumer<>));
        var consumeMethod = consumerInterfaceType.GetMethod(nameof(IIntegrationEventConsumer<IntegrationEvent>.ConsumeAsync))!;
        Task task;
        try
        {
            task = (Task)consumeMethod.Invoke(consumer, [integrationEvent, cancellationToken])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            // Reflection wraps a synchronously-throwing implementation; the
            // consumer's own exception is what gets recorded and retried.
            throw exception.InnerException;
        }

        await task;
    }

    private TimeSpan ComputeBackoff(int attempts)
    {
        var multiplier = Math.Pow(2, attempts - 1);
        var jitter = 1 + Random.Shared.NextDouble() * JitterFraction;
        return _options.BaseBackoff * multiplier * jitter;
    }

    private DateTime UtcNow() => DateTime.SpecifyKind(timeProvider.GetUtcNow().UtcDateTime, DateTimeKind.Utc);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value.Substring(0, maxLength);
}
