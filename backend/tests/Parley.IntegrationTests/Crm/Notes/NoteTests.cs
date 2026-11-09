using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Crm.Notes;

/// <summary>
/// A note belongs to exactly one subject. Its text can be edited with the row version, and deleting it removes it
/// from the note list and from the timeline.
/// </summary>
public sealed class NoteTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string NoteType = "note_added";

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
    public async Task Note_IsEditedWithItsVersion_AndDisappearsWhenDeleted()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        var note = await NoteApi.CreateAsync(admin, "Met the buyer", contactId, null, null, token);
        var noteId = NoteApi.IdOf(note);

        var updated = await admin.PutAsJsonAsync(PipelineApi.Route(CrmRoutes.NoteById, noteId), new { version = NoteApi.VersionOf(note), body = "Buyer asked for terms" }, token);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedView = await AuthTestClient.ReadJsonAsync(updated, token);
        updatedView.GetProperty("body").GetString().Should().Be("Buyer asked for terms");

        var stale = await admin.PutAsJsonAsync(PipelineApi.Route(CrmRoutes.NoteById, noteId), new { version = NoteApi.VersionOf(note), body = "Stale" }, token);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(stale, token)).Should().Be(ErrorCodes.ConcurrencyConflict);

        var timeline = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&type={NoteType}", token)).ToList();
        timeline.Should().ContainSingle();
        timeline[0].GetProperty("noteBody").GetString().Should().Be("Buyer asked for terms");

        (await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.NoteById, noteId), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await NoteApi.PageAsync(admin, CrmRoutes.Notes, $"contactId={contactId}", token);
        NoteApi.ItemsOf(list).Should().BeEmpty();
        var afterDelete = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&type={NoteType}", token));
        afterDelete.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_NeedsExactlyOneSubject()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));

        var none = await admin.PostAsJsonAsync(CrmRoutes.Notes, new { body = "Orphan" }, token);
        none.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(none, "contactId", token)).Should().Be(CrmErrorCodes.NoteSubjectRequired);

        var two = await admin.PostAsJsonAsync(CrmRoutes.Notes, new { body = "Two", contactId, dealId = Guid.NewGuid() }, token);
        two.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(two, "contactId", token)).Should().Be(CrmErrorCodes.NoteSubjectRequired);
    }

    [Fact]
    public async Task Create_OnASubjectTheCallerCannotRead_IsRefused()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        var refused = await repClient.PostAsJsonAsync(CrmRoutes.Notes, new { body = "Not mine", contactId }, token);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(refused, "contactId", token)).Should().Be(CrmErrorCodes.NoteSubjectNotFound);
    }

    [Fact]
    public async Task OnlyTheAuthorOrAnOrganizationWideMember_EditsOrDeletesANote()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        var marketer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.MarketerRoleId, token);
        using var marketerClient = CrmApi.Client(_factory, marketer.AccessToken);
        var repContactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create() with { OwnerUserId = rep.UserId }, token));

        var ownNote = await NoteApi.CreateAsync(repClient, "Rep's own note", repContactId, null, null, token);
        var edited = await repClient.PutAsJsonAsync(PipelineApi.Route(CrmRoutes.NoteById, NoteApi.IdOf(ownNote)), new { version = NoteApi.VersionOf(ownNote), body = "Edited by author" }, token);
        edited.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminNote = await NoteApi.CreateAsync(admin, "Admin's note", repContactId, null, null, token);
        var otherRepEdit = await repClient.PutAsJsonAsync(PipelineApi.Route(CrmRoutes.NoteById, NoteApi.IdOf(adminNote)), new { version = NoteApi.VersionOf(adminNote), body = "Not mine" }, token);
        otherRepEdit.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(otherRepEdit, token)).Should().Be(CrmErrorCodes.NoteNotFound);
        var otherRepDelete = await repClient.DeleteAsync(PipelineApi.Route(CrmRoutes.NoteById, NoteApi.IdOf(adminNote)), token);
        otherRepDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var moderatedEdit = await marketerClient.PutAsJsonAsync(PipelineApi.Route(CrmRoutes.NoteById, NoteApi.IdOf(adminNote)), new { version = NoteApi.VersionOf(adminNote), body = "Moderated" }, token);
        moderatedEdit.StatusCode.Should().Be(HttpStatusCode.OK);
        var moderatedDelete = await marketerClient.DeleteAsync(PipelineApi.Route(CrmRoutes.NoteById, NoteApi.IdOf(adminNote)), token);
        moderatedDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TimelineEntry_CarriesTheNoteVersion_ForEditing()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        var note = await NoteApi.CreateAsync(admin, "Versioned", contactId, null, null, token);
        var updated = await admin.PutAsJsonAsync(PipelineApi.Route(CrmRoutes.NoteById, NoteApi.IdOf(note)), new { version = NoteApi.VersionOf(note), body = "Versioned again" }, token);
        var updatedNote = await AuthTestClient.ReadJsonAsync(updated, token);

        var entries = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&type={NoteType}", token)).ToList();

        entries.Should().ContainSingle();
        entries[0].GetProperty("noteVersion").GetUInt32().Should().Be(NoteApi.VersionOf(updatedNote));
        var created = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}", token))
            .Where(entry => entry.GetProperty("type").GetString() == "contact_created")
            .ToList();
        created.Should().ContainSingle();
        created[0].GetProperty("noteVersion").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }
}
