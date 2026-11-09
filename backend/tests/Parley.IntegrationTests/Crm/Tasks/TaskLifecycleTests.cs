using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Crm.Tasks;

/// <summary>
/// A task moves through open, completed and open again, each step guarded by its row version. Completing a task linked
/// to a contact adds one timeline entry. Deleting a task hides it.
/// </summary>
public sealed class TaskLifecycleTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string TaskCompletedType = "task_completed";
    private const string TaskTitle = "Call back";

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
    public async Task CompleteAndReopen_FollowTheVersionAndStateRules_AndWriteOneTimelineEntry()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        var dueAt = _factory.Clock.GetUtcNow().AddHours(2);

        var created = await TaskApi.CreateAsync(admin, TaskTitle, dueAt, null, contactId, token);
        var taskId = TaskApi.IdOf(created);
        var createdVersion = TaskApi.VersionOf(created);
        created.GetProperty("isCompleted").GetBoolean().Should().BeFalse();
        TaskApi.AssigneeOf(created).Should().Be(owner.UserId, "the caller is the default assignee");
        created.GetProperty("dueAt").GetDateTime().Should().BeCloseTo(dueAt.UtcDateTime, TimeSpan.FromMilliseconds(1));

        var completed = await TaskApi.CompleteAsync(admin, taskId, createdVersion, token);
        completed.StatusCode.Should().Be(HttpStatusCode.OK);
        var completedView = await ReadBodyAsync(completed, token);
        completedView.GetProperty("isCompleted").GetBoolean().Should().BeTrue();
        completedView.GetProperty("completedAt").ValueKind.Should().NotBe(JsonValueKind.Null);

        // A double click completes again: 200 with the same task, and no second timeline entry.
        var completedAgain = await TaskApi.CompleteAsync(admin, taskId, TaskApi.VersionOf(completedView), token);
        completedAgain.StatusCode.Should().Be(HttpStatusCode.OK);
        var completedAgainView = await ReadBodyAsync(completedAgain, token);
        completedAgainView.GetProperty("completedAt").GetDateTime().Should().Be(completedView.GetProperty("completedAt").GetDateTime());

        var staleReopen = await TaskApi.ReopenAsync(admin, taskId, createdVersion, token);
        staleReopen.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(staleReopen, token)).Should().Be(ErrorCodes.ConcurrencyConflict);

        var reopened = await TaskApi.ReopenAsync(admin, taskId, TaskApi.VersionOf(completedView), token);
        reopened.StatusCode.Should().Be(HttpStatusCode.OK);
        var reopenedView = await ReadBodyAsync(reopened, token);
        reopenedView.GetProperty("isCompleted").GetBoolean().Should().BeFalse();

        // A double click reopens again: 200 with the open task, nothing changes.
        var reopenAgain = await TaskApi.ReopenAsync(admin, taskId, TaskApi.VersionOf(reopenedView), token);
        reopenAgain.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadBodyAsync(reopenAgain, token)).GetProperty("isCompleted").GetBoolean().Should().BeFalse();

        var timeline = await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&limit=50", token);
        var completions = NoteApi.ItemsOf(timeline).Where(item => item.GetProperty("type").GetString() == TaskCompletedType).ToList();
        completions.Should().ContainSingle();
        completions[0].GetProperty("taskId").GetGuid().Should().Be(taskId);
        completions[0].GetProperty("taskTitle").GetString().Should().Be(TaskTitle);
    }

    [Fact]
    public async Task Delete_HidesTheTask_AndAnUnknownTaskIsNotFound()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var task = await TaskApi.CreateAsync(admin, TaskTitle, null, null, null, token);
        var taskId = TaskApi.IdOf(task);

        (await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.TaskById, taskId), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var read = await admin.GetAsync(PipelineApi.Route(CrmRoutes.TaskById, taskId), token);
        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(read, token)).Should().Be(CrmErrorCodes.TaskNotFound);
        (await TaskApi.ListIdsAsync(admin, CrmRoutes.Tasks, "limit=100", token)).Should().NotContain(taskId);
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await AuthTestClient.ReadJsonAsync(response, cancellationToken);
}
