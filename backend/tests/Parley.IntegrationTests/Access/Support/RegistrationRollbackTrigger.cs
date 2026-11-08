using Npgsql;

namespace Parley.IntegrationTests.Access.Support;

/// <summary>
/// Test-only database trigger that fails the outbox write of a registration whose
/// organization name starts with <see cref="OrganizationNamePrefix"/>. The write runs
/// last in the registration transaction, so the failure proves the earlier writes roll
/// back with it. The trigger lives in the test database only.
/// </summary>
public static class RegistrationRollbackTrigger
{
    public const string OrganizationNamePrefix = "fail-";

    private const string Sql = """
        CREATE OR REPLACE FUNCTION identity.test_fail_marked_registration() RETURNS trigger
        LANGUAGE plpgsql AS $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM identity.organizations o
                WHERE o.id = NEW.organization_id AND o.name LIKE 'fail-%')
            THEN
                RAISE EXCEPTION 'test-only registration failure';
            END IF;
            RETURN NEW;
        END;
        $$;
        DROP TRIGGER IF EXISTS test_fail_marked_registration ON identity.outbox_messages;
        CREATE TRIGGER test_fail_marked_registration
            BEFORE INSERT ON identity.outbox_messages
            FOR EACH ROW EXECUTE FUNCTION identity.test_fail_marked_registration();
        """;

    public static async Task InstallAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(Sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
