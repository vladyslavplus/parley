using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Orders;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Localization;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Generates the demo workspace into one organization in a single transaction: the default pipeline, binary COPY of the
/// generated contacts and orders into staging tables, then set-based inserts that skip rows which already exist, the
/// last-order update, deals, tasks, notes, tags and the timeline. No events are written: the seed bypasses the outbox.
/// </summary>
public sealed class CrmDemoSeeder(NpgsqlDataSource dataSource) : ICrmDemoSeeder
{
    private const string OrganizationParameter = "org";

    private const string NowParameter = "now";

    private const string SourceParameter = "source";

    private const string OrderSourceParameter = "order_source";

    private const string EmptyJsonParameter = "empty_json";

    private const string CurrencyParameter = "currency";

    private const string RecentSinceParameter = "recent_since";

    private const string FirstPositionParameter = "first_position";

    private const string TagShareParameter = "tag_share";

    private static readonly string[] WipeStatements =
    [
        $"DELETE FROM {Table(CrmSchemaNames.ActivitiesTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.NotesTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.TasksTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.DealStageHistoryTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.DealsTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.OrdersTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.ContactTagsTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.ContactsTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.CompaniesTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.TagsTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.PipelineStagesTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.PipelinesTable)} WHERE organization_id = @org",
        $"DELETE FROM {Table(CrmSchemaNames.ContactExportsTable)} WHERE organization_id = @org",
    ];

    public async Task<CrmDemoImportReport> SeedAsync(CrmDemoImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var contacts = DemoGenerator.Contacts(request);
        var orders = DemoGenerator.Orders(contacts, request);
        var now = request.ReferenceInstant;
        var recentSince = now.AddDays(-DemoWorkspaceRules.RecentActivityDays);
        var scope = Scope(request.OrganizationId, now);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var pipelineCreated = await EnsureDefaultPipelineAsync(connection, transaction, request, now, cancellationToken);

        await ExecuteAsync(connection, transaction, DemoSeedSql.CreateOrdersStaging, cancellationToken);
        await CopyOrdersAsync(connection, orders, cancellationToken);
        await ExecuteAsync(connection, transaction, DemoSeedSql.CreateContactsStaging, cancellationToken);
        await CopyContactsAsync(connection, contacts, cancellationToken);

        var companiesInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertCompanies, cancellationToken, scope);
        var contactsInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertContacts, cancellationToken, scope);
        var ordersWithoutContact = await CountOrdersWithoutContactAsync(connection, transaction, scope, cancellationToken);
        var ordersInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertOrders, cancellationToken,
            [.. scope, (CurrencyParameter, request.Currency)]);
        var lastOrderUpdated = await ExecuteAsync(connection, transaction, DemoSeedSql.UpdateLastOrderAt, cancellationToken, scope);

        var dealScope = Concat(scope, [(CurrencyParameter, request.Currency), (RecentSinceParameter, recentSince)]);
        var dealsInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertDeals, cancellationToken, dealScope);
        var firstPositionScope = Concat(scope, [(FirstPositionParameter, DemoWorkspaceRules.FirstStageIndex)]);
        var historyInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertDealHistoryCreated, cancellationToken, firstPositionScope)
            + await ExecuteAsync(connection, transaction, DemoSeedSql.InsertDealHistoryMoved, cancellationToken, firstPositionScope);
        var tasksInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertTasks, cancellationToken, scope);
        var notesInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertNotes, cancellationToken, scope);
        var activitiesInserted = await ExecuteAsync(connection, transaction, DemoSeedSql.InsertActivities, cancellationToken, scope);

        await ExecuteAsync(connection, transaction, DemoSeedSql.InsertTags, cancellationToken, scope);
        await ExecuteAsync(connection, transaction, DemoSeedSql.InsertContactTags, cancellationToken,
            Concat(scope, [(TagShareParameter, request.Profile.TagSharePercent)]));

        await transaction.CommitAsync(cancellationToken);

        var latest = orders.Count == 0 ? (DateTimeOffset?)null : new DateTimeOffset(orders.Max(order => order.OrderedAt), TimeSpan.Zero);
        return new CrmDemoImportReport(
            ContactsRead: contacts.Count,
            ContactsInserted: contactsInserted,
            ContactsSkipped: contacts.Count - contactsInserted,
            OrdersRead: orders.Count,
            OrdersInserted: ordersInserted,
            OrdersSkipped: orders.Count - ordersWithoutContact - ordersInserted,
            OrdersWithoutContact: ordersWithoutContact,
            ContactsLastOrderUpdated: lastOrderUpdated,
            LatestOrderAt: latest,
            PipelineCreated: pipelineCreated,
            CompaniesInserted: companiesInserted,
            DealsInserted: dealsInserted,
            DealStageHistoryInserted: historyInserted,
            TasksInserted: tasksInserted,
            NotesInserted: notesInserted,
            ActivitiesInserted: activitiesInserted);
    }

    public async Task WipeAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var statement in WipeStatements)
        {
            await using var command = new NpgsqlCommand(statement, connection, transaction);
            command.Parameters.AddWithValue(OrganizationParameter, organizationId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>The parameters every statement of the seed takes.</summary>
    private static IReadOnlyList<(string Name, object Value)> Scope(Guid organizationId, DateTimeOffset now) =>
    [
        (OrganizationParameter, organizationId),
        (NowParameter, now),
        (SourceParameter, ContactSources.DemoSeed),
        (OrderSourceParameter, OrderSources.DemoSeed),
        (EmptyJsonParameter, CrmLimits.EmptyJsonObject),
    ];

    private static IReadOnlyList<(string Name, object Value)> Concat(
        IReadOnlyList<(string Name, object Value)> first,
        IReadOnlyList<(string Name, object Value)> second) => [.. first, .. second];

    /// <summary>Creates the default pipeline when the organization has none. Stored names are in the organization locale; system keys let readers localize them.</summary>
    private static async Task<bool> EnsureDefaultPipelineAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CrmDemoImportRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await ScalarAsync(connection, transaction, DemoSeedSql.CountDefaultPipeline, cancellationToken,
            [(OrganizationParameter, request.OrganizationId)]);
        if (Convert.ToInt64(existing, CultureInfo.InvariantCulture) > 0)
        {
            return false;
        }

        var pipelineId = DemoIdentifiers.Derive(DemoSeedConstants.PipelineKind, request.OrganizationId.ToString("D"));
        await ExecuteAsync(connection, transaction, DemoSeedSql.InsertPipeline, cancellationToken,
        [
            (OrganizationParameter, request.OrganizationId),
            (NowParameter, now),
            ("id", pipelineId),
            ("name", SharedTexts.Get(DefaultPipelineStages.PipelineNameKey, request.Locale)),
            ("system_key", DefaultPipelineStages.PipelineNameKey),
        ]);

        for (var index = 0; index < DefaultPipelineStages.All.Count; index++)
        {
            var template = DefaultPipelineStages.All[index];
            await ExecuteAsync(connection, transaction, DemoSeedSql.InsertPipelineStage, cancellationToken,
            [
                (OrganizationParameter, request.OrganizationId),
                (NowParameter, now),
                ("id", DemoIdentifiers.Derive(DemoSeedConstants.PipelineStageKind, $"{pipelineId:D}:{index}")),
                ("pipeline_id", pipelineId),
                ("name", SharedTexts.Get(template.NameKey, request.Locale)),
                ("system_key", template.NameKey),
                ("position", index),
                ("kind", template.Kind),
                ("probability_bps", template.ProbabilityBps),
                ("color_index", template.ColorIndex),
            ]);
        }

        return true;
    }

    private static async Task CopyContactsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<DemoContactSeed> contacts,
        CancellationToken cancellationToken)
    {
        await using var importer = await connection.BeginBinaryImportAsync(DemoSeedSql.CopyContacts, cancellationToken);
        foreach (var contact in contacts)
        {
            DateTime? taskDueAt = contact.TaskDueAt;
            bool? taskCompleted = contact.TaskDueAt is null ? null : contact.TaskCompleted;
            DateTime? expectedCloseOn = contact.DealExpectedCloseOn;

            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(contact.Id, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(contact.CustomerRef, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(contact.FirstName, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(contact.LastName, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(contact.Email, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(contact.Phone, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(contact.JobTitle, NpgsqlDbType.Text, cancellationToken);
            await WriteNullableAsync(importer, contact.Country, NpgsqlDbType.Text, cancellationToken);
            await WriteNullableAsync(importer, contact.City, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(contact.OwnerUserId, NpgsqlDbType.Uuid, cancellationToken);
            await WriteNullableAsync(importer, contact.CompanyId, NpgsqlDbType.Uuid, cancellationToken);
            await WriteNullableAsync(importer, contact.CompanyName, NpgsqlDbType.Text, cancellationToken);
            await WriteNullableAsync(importer, contact.DealStageIndex, NpgsqlDbType.Integer, cancellationToken);
            await WriteNullableAsync(importer, contact.DealTitle, NpgsqlDbType.Text, cancellationToken);
            await WriteNullableAsync(importer, contact.DealLostReason, NpgsqlDbType.Text, cancellationToken);
            await WriteNullableAsync(importer, expectedCloseOn, NpgsqlDbType.Date, cancellationToken);
            await WriteNullableAsync(importer, contact.TaskTitle, NpgsqlDbType.Text, cancellationToken);
            await WriteNullableAsync(importer, taskDueAt, NpgsqlDbType.TimestampTz, cancellationToken);
            await WriteNullableAsync(importer, taskCompleted, NpgsqlDbType.Boolean, cancellationToken);
            await WriteNullableAsync(importer, contact.NoteBody, NpgsqlDbType.Text, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static async Task CopyOrdersAsync(
        NpgsqlConnection connection,
        IReadOnlyList<DemoOrderSeed> orders,
        CancellationToken cancellationToken)
    {
        await using var importer = await connection.BeginBinaryImportAsync(DemoSeedSql.CopyOrders, cancellationToken);
        foreach (var order in orders)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(order.Id, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(order.InvoiceNo, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(order.CustomerRef, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(order.OrderedAt, NpgsqlDbType.TimestampTz, cancellationToken);
            await importer.WriteAsync(order.TotalAmount, NpgsqlDbType.Numeric, cancellationToken);
            await importer.WriteAsync(order.LineCount, NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(order.IsCancellation, NpgsqlDbType.Boolean, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static async Task WriteNullableAsync<T>(NpgsqlBinaryImporter importer, T? value, NpgsqlDbType type, CancellationToken cancellationToken)
        where T : struct
    {
        if (value is null)
        {
            await importer.WriteNullAsync(cancellationToken);
        }
        else
        {
            await importer.WriteAsync(value.Value, type, cancellationToken);
        }
    }

    private static async Task WriteNullableAsync(NpgsqlBinaryImporter importer, string? value, NpgsqlDbType type, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            await importer.WriteNullAsync(cancellationToken);
        }
        else
        {
            await importer.WriteAsync(value, type, cancellationToken);
        }
    }

    private static async Task<int> CountOrdersWithoutContactAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<(string Name, object Value)> scope,
        CancellationToken cancellationToken)
    {
        var count = await ScalarAsync(connection, transaction, DemoSeedSql.CountOrdersWithoutContact, cancellationToken, scope);
        return Convert.ToInt32(count, CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Runs one statement with the given parameters and returns the number of rows it wrote.</summary>
    private static async Task<int> ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        IReadOnlyList<(string Name, object Value)> parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<object?> ScalarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        IReadOnlyList<(string Name, object Value)> parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddParameters(command, parameters);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static void AddParameters(NpgsqlCommand command, IReadOnlyList<(string Name, object Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
    }

    private static string Table(string name) => "\"" + CrmSchemaNames.Schema + "\".\"" + name + "\"";
}
