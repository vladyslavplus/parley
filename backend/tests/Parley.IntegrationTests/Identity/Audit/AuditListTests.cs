using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Parley.IntegrationTests.Outbox.Support;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Identity.Audit;

/// <summary>The audit log filters by action and by an inclusive-exclusive UTC time range, and rejects bad values.</summary>
public sealed class AuditListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Route = "audit-events";
    private const string ActionParameter = "action";
    private const string FromParameter = "from";
    private const string ToParameter = "to";
    private const int FirstTeamHour = 0;
    private const int SecondTeamHour = 1;
    private static readonly TimeSpan HourStep = TimeSpan.FromHours(1);

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
    public async Task AuditList_FiltersByActionAndTimeRange()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var startedAt = _factory.Clock.GetUtcNow().UtcDateTime;

        await CreateTeamAsync(admin, "Audit first", token);
        _factory.Clock.Advance(HourStep * (SecondTeamHour - FirstTeamHour));
        var secondTeamAt = _factory.Clock.GetUtcNow().UtcDateTime;
        await CreateTeamAsync(admin, "Audit second", token);

        var teamCreated = $"{Route}?{ActionParameter}={AuditActions.TeamCreated}";
        (await ListPages.ReadAllAsync(admin, teamCreated, token)).Should().HaveCount(2);
        (await ListPages.ReadAllAsync(admin, $"{teamCreated}&{FromParameter}={Uri.EscapeDataString(secondTeamAt.ToString("O"))}", token))
            .Should().HaveCount(1, "the start of the range is inclusive");
        (await ListPages.ReadAllAsync(admin, $"{teamCreated}&{ToParameter}={Uri.EscapeDataString(secondTeamAt.ToString("O"))}", token))
            .Should().HaveCount(1, "the end of the range is exclusive");
        (await ListPages.ReadAllAsync(admin, $"{teamCreated}&{FromParameter}={Uri.EscapeDataString(startedAt.ToString("O"))}&{ToParameter}={Uri.EscapeDataString(startedAt.ToString("O"))}", token))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task AuditList_RejectsAnUnreadableInstant_WithValidationCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var response = await admin.GetAsync($"{Route}?{FromParameter}=yesterday", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(response, FromParameter, token)).Should().Be(ErrorCodes.ValidationFailed);
    }

    /// <summary>Creates a team and runs the outbox, so the audit consumer has written its row before the next step.</summary>
    private async Task CreateTeamAsync(HttpClient admin, string name, CancellationToken token)
    {
        (await admin.PostAsJsonAsync(IdentityRoutes.Teams, new { name }, token)).StatusCode.Should().Be(HttpStatusCode.OK);
        await OutboxDelivery.DeliverDueAsync<IdentityDbContext>(_factory.Services, token);
    }
}
