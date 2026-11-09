using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Activities;

/// <summary>
/// The timeline of one subject pages in time order without gaps or repeats, filters by type and text, shows names and
/// UTC instants, and refuses a request without exactly one subject.
/// </summary>
public sealed class TimelineTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string NoteType = "note_added";
    private const string TaskCompletedType = "task_completed";
    private const string ContactCreatedType = "contact_created";
    private const string SearchedNote = "Quarterly budget review";

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
    public async Task ContactTimeline_PagesEveryEntryOnce_InNewestFirstOrder()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        await NoteApi.CreateAsync(admin, SearchedNote, contactId, null, null, token);
        await NoteApi.CreateAsync(admin, "Second call notes", contactId, null, null, token);
        var task = await TaskApi.CreateAsync(admin, "Send proposal", null, null, contactId, token);
        (await TaskApi.CompleteAsync(admin, TaskApi.IdOf(task), TaskApi.VersionOf(task), token)).EnsureSuccessStatusCode();

        var seen = new List<Guid>();
        var cursor = (string?)null;
        do
        {
            var query = $"contactId={contactId}&limit=2" + (cursor is null ? string.Empty : $"&after={Uri.EscapeDataString(cursor)}");
            var page = await NoteApi.PageAsync(admin, CrmRoutes.Activities, query, token);
            seen.AddRange(NoteApi.ItemsOf(page).Select(item => item.GetProperty("id").GetGuid()));
            cursor = NoteApi.NextCursorOf(page);
        }
        while (cursor is not null);

        seen.Should().HaveCount(4).And.OnlyHaveUniqueItems();
        var all = await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&limit=100", token);
        var occurred = NoteApi.ItemsOf(all).Select(item => item.GetProperty("occurredAt").GetDateTime()).ToList();
        occurred.Should().BeInDescendingOrder();
        NoteApi.ItemsOf(all).Select(item => item.GetProperty("id").GetGuid()).Should().Equal(seen);
        NoteApi.ItemsOf(all).Select(item => item.GetProperty("type").GetString()).Should().Contain(ContactCreatedType);
    }

    [Fact]
    public async Task Timeline_FiltersByType_SearchesNoteAndTaskText_AndShowsNamesAndUtcInstants()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        var note = await NoteApi.CreateAsync(admin, SearchedNote, contactId, null, null, token);
        var task = await TaskApi.CreateAsync(admin, "Send proposal", null, null, contactId, token);
        (await TaskApi.CompleteAsync(admin, TaskApi.IdOf(task), TaskApi.VersionOf(task), token)).EnsureSuccessStatusCode();

        var notes = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&type={NoteType}", token)).ToList();
        notes.Should().ContainSingle();
        notes[0].GetProperty("noteId").GetGuid().Should().Be(NoteApi.IdOf(note));
        notes[0].GetProperty("noteBody").GetString().Should().Be(SearchedNote);
        notes[0].GetProperty("actorDisplayName").ValueKind.Should().NotBe(JsonValueKind.Undefined);
        notes[0].GetProperty("contactName").GetString().Should().NotBeNullOrWhiteSpace();
        notes[0].GetProperty("occurredAt").GetString().Should().EndWith("Z");

        var bySearch = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&q=budget", token)).ToList();
        bySearch.Should().ContainSingle();
        bySearch[0].GetProperty("type").GetString().Should().Be(NoteType);

        var byTaskTitle = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"contactId={contactId}&q=proposal", token)).ToList();
        byTaskTitle.Should().ContainSingle();
        byTaskTitle[0].GetProperty("type").GetString().Should().Be(TaskCompletedType);
    }

    [Fact]
    public async Task Timeline_NeedsExactlyOneSubject()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));
        var companyId = await CreateCompanyAsync(admin, token);

        var none = await admin.GetAsync($"{CrmRoutes.Activities}?limit=10", token);
        none.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(none, "contactId", token)).Should().Be(CrmErrorCodes.ActivitySubjectRequired);

        var two = await admin.GetAsync($"{CrmRoutes.Activities}?contactId={contactId}&companyId={companyId}", token);
        two.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(two, "contactId", token)).Should().Be(CrmErrorCodes.ActivitySubjectAmbiguous);

        var unknown = await admin.GetAsync($"{CrmRoutes.Activities}?contactId={Guid.NewGuid()}", token);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(unknown, token)).Should().Be(CrmErrorCodes.ActivitySubjectNotFound);
    }

    [Fact]
    public async Task CompanyTimeline_ShowsItsNotes()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var companyId = await CreateCompanyAsync(admin, token);
        await NoteApi.CreateAsync(admin, "Board meeting", null, null, companyId, token);

        var entries = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"companyId={companyId}", token)).ToList();

        entries.Should().ContainSingle();
        entries[0].GetProperty("type").GetString().Should().Be(NoteType);
        entries[0].GetProperty("companyName").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CompanyTimeline_ShowsContactCreatedEntriesOfItsContacts()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var companyId = await CreateCompanyAsync(admin, token);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create() with { CompanyId = companyId }, token));

        var entries = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"companyId={companyId}", token)).ToList();

        entries.Should().ContainSingle();
        entries[0].GetProperty("type").GetString().Should().Be(ContactCreatedType);
        entries[0].GetProperty("contactId").GetGuid().Should().Be(contactId);
    }

    private static async Task<Guid> CreateCompanyAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(CrmRoutes.Companies, new { name = "Northwind Trading" }, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return CrmApi.IdOf(await AuthTestClient.ReadJsonAsync(response, cancellationToken));
    }
}
