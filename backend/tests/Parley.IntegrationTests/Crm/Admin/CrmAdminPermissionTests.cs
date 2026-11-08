using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Admin;

/// <summary>Tag and custom field administration is closed to a sales rep: the write is refused before any record is touched.</summary>
public sealed class CrmAdminPermissionTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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
    public async Task SalesRep_CannotCreateTagsOrCustomFields()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        var tag = await repClient.PostAsJsonAsync(CrmRoutes.Tags, new { name = "priority", colorIndex = (short)1 }, token);
        tag.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var field = await repClient.PostAsJsonAsync(
            CrmRoutes.CustomFields,
            new
            {
                entityType = "contact",
                key = "segment",
                label = "Segment",
                fieldType = "text",
                options = (string[]?)null,
                isRequired = false,
                position = 0,
            },
            token);
        field.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
