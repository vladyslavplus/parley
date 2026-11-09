using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Tasks;

/// <summary>
/// A sales rep sees and assigns only their own tasks, a viewer cannot create tasks, and a task cannot link a
/// contact the caller may not read.
/// </summary>
public sealed class TaskAccessTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
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
    public async Task SalesRep_SeesOnlyOwnTasks_AndCannotAssignToAnotherMember()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        var adminTask = await TaskApi.CreateAsync(admin, "Admin task", null, null, null, token);
        var repTask = await TaskApi.CreateAsync(repClient, "Rep task", null, null, null, token);
        TaskApi.AssigneeOf(repTask).Should().Be(rep.UserId);

        (await TaskApi.ListIdsAsync(repClient, CrmRoutes.Tasks, "limit=100", token)).Should().Equal(TaskApi.IdOf(repTask));
        (await TaskApi.ListIdsAsync(repClient, CrmRoutes.TasksMine, "limit=100", token)).Should().Equal(TaskApi.IdOf(repTask));
        (await TaskApi.ListIdsAsync(admin, CrmRoutes.TasksMine, "limit=100", token)).Should().Equal(TaskApi.IdOf(adminTask));

        var hidden = await repClient.GetAsync(PipelineApi.Route(CrmRoutes.TaskById, TaskApi.IdOf(adminTask)), token);
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var assignToAdmin = await repClient.PostAsJsonAsync(
            CrmRoutes.Tasks,
            new { title = "Not mine", assigneeUserId = owner.UserId },
            token);
        assignToAdmin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(assignToAdmin, "assigneeUserId", token)).Should().Be(CrmErrorCodes.TaskAssigneeNotAllowed);
    }

    [Fact]
    public async Task Viewer_CannotCreateTasks()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var viewer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.ViewerRoleId, token);
        using var viewerClient = CrmApi.Client(_factory, viewer.AccessToken);

        var refused = await viewerClient.PostAsJsonAsync(CrmRoutes.Tasks, new { title = "Not allowed" }, token);

        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TaskLinkedToAContactTheCallerCannotRead_IsRefused()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        var refused = await repClient.PostAsJsonAsync(CrmRoutes.Tasks, new { title = "Call", contactId }, token);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(refused, "contactId", token)).Should().Be(CrmErrorCodes.TaskSubjectNotFound);
    }
}
