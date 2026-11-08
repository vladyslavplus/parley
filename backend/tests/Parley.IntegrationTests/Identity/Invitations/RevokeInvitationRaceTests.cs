using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Npgsql;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Identity.Invitations;

/// <summary>
/// A revoke whose invitation row is rewritten by another writer while the revoke is in flight
/// (for example the delivery job rotating the token) still revokes. It is not reported as a conflict.
/// </summary>
public sealed class RevokeInvitationRaceTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private static readonly TimeSpan BlockedWriterLimit = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan BlockedWriterPollInterval = TimeSpan.FromMilliseconds(50);

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task RevokeWhoseRowIsRewrittenInFlight_StillRevokes()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);
        var created = await admin.PostAsJsonAsync(
            IdentityRoutes.Invitations,
            new { email = AuthTestClient.UniqueEmail(), roleCode = SystemRoles.Viewer },
            token);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var invitationId = (await AuthTestClient.ReadJsonAsync(created, token)).GetProperty("id").GetGuid();

        // Another writer takes the row lock and rewrites the row. The revoke loads the row before
        // this commit, then its write waits on the lock and finds the row changed underneath it.
        await using var writer = new NpgsqlConnection(postgres.ConnectionString);
        await writer.OpenAsync(token);
        await using var writerTransaction = await writer.BeginTransactionAsync(token);
        await using (var rewrite = new NpgsqlCommand(
            $"UPDATE \"{IdentityDbContext.SchemaName}\".\"invitations\" SET \"expires_at\" = \"expires_at\" WHERE \"id\" = @id",
            writer,
            writerTransaction))
        {
            rewrite.Parameters.AddWithValue("id", invitationId);
            (await rewrite.ExecuteNonQueryAsync(token)).Should().Be(1);
        }

        var revoke = admin.PostAsync(IdentityApi.Route(IdentityRoutes.InvitationRevoke, invitationId), null, token);
        await WaitForBlockedWriterAsync(token);
        await writerTransaction.CommitAsync(token);

        using var response = await revoke;
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "a row rewritten by another writer is not a conflict for a pending invitation");
        (await IsRevokedAsync(invitationId, token)).Should().BeTrue("the revoke is stored");
    }

    /// <summary>Waits until a write is blocked on a row lock, so the rewrite is known to overlap the revoke.</summary>
    private async Task WaitForBlockedWriterAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var probe = new NpgsqlConnection(postgres.ConnectionString);
        await probe.OpenAsync(cancellationToken);
        while (true)
        {
            await using var command = new NpgsqlCommand("SELECT count(*) FROM pg_locks WHERE NOT granted", probe);
            if ((long)(await command.ExecuteScalarAsync(cancellationToken))! > 0)
            {
                return;
            }

            if (stopwatch.Elapsed > BlockedWriterLimit)
            {
                throw new TimeoutException("The revoke never waited on the invitation row, so the overlap did not happen.");
            }

            await Task.Delay(BlockedWriterPollInterval, cancellationToken);
        }
    }

    private async Task<bool> IsRevokedAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT \"revoked_at\" IS NOT NULL FROM \"{IdentityDbContext.SchemaName}\".\"invitations\" WHERE \"id\" = @id",
            connection);
        command.Parameters.AddWithValue("id", invitationId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
