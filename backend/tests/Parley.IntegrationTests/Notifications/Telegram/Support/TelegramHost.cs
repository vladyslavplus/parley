using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Api.Controllers.Telegram;
using Parley.Crm.Contracts.Events;
using Parley.Insights.Contracts.Priority;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Bot;
using Parley.Notifications.Telegram.Webhook;

namespace Parley.IntegrationTests.Notifications.Telegram.Support;

/// <summary>
/// The host of the Telegram tests: a configured channel in webhook mode with fake bot and priority clients, and the requests and
/// database checks the tests share. No test here reaches Telegram or the priority engine.
/// </summary>
public static class TelegramHost
{
    public const string BotUsername = "parley_test_bot";

    private const string WebhookSecret = "test-webhook-secret-0123456789abcdef";

    private const string TokenValue = "123456:test-token-not-a-real-bot";

    private const string PublicWebBaseUrl = "https://app.parley.test";

    private const string CodeProperty = "code";

    private const string LinkedProperty = "linked";

    private const string ReplayAssignmentsSql = """
        INSERT INTO crm.outbox_messages (id, organization_id, type, payload, occurred_at, processed_at, attempts, next_attempt_at)
        SELECT gen_random_uuid(), organization_id, type, payload, occurred_at, NULL, 0, now()
        FROM crm.outbox_messages
        WHERE type = @type
        """;

    public static IdentityApiFactory Create(PostgresFixture postgres, MailpitFixture mailpit) => new(
        postgres,
        mailpit,
        extraConfiguration: new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = TokenValue,
            ["Telegram:BotUsername"] = BotUsername,
            ["Telegram:Mode"] = "Webhook",
            ["Telegram:WebhookSecret"] = WebhookSecret,
            ["Telegram:DeliveryWorkerEnabled"] = "false",
            ["Telegram:MinimumChatIntervalMilliseconds"] = "0",
            ["Telegram:MaximumMessagesPerSecond"] = "0",
            ["Notifications:PublicWebBaseUrl"] = PublicWebBaseUrl,
        },
        extraServices: AddFakes);

    /// <summary>The app as a deployment with no token runs it: the channel is off and nothing is configured.</summary>
    public static IdentityApiFactory CreateWithoutToken(PostgresFixture postgres, MailpitFixture mailpit) =>
        new(postgres, mailpit, extraServices: AddFakes);

    public static FakeTelegramBotClient Bot(IdentityApiFactory factory) => factory.Services.GetRequiredService<FakeTelegramBotClient>();

    public static FakePriorityReader Priority(IdentityApiFactory factory) => factory.Services.GetRequiredService<FakePriorityReader>();

    /// <summary>Posts one update to the webhook as Telegram does, with the secret header when one is given.</summary>
    public static async Task<HttpResponseMessage> PostUpdateAsync(
        IdentityApiFactory factory,
        long updateId,
        long chatId,
        string text,
        string? secret = WebhookSecret,
        string? languageCode = "en",
        CancellationToken cancellationToken = default)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, TelegramRoutes.Webhook);
        request.Content = JsonContent.Create(new
        {
            update_id = updateId,
            message = new { text, chat = new { id = chatId, type = "private" }, from = new { language_code = languageCode } },
        });
        if (secret is not null)
        {
            request.Headers.Add(TelegramWebhookSecret.HeaderName, secret);
        }

        return await client.SendAsync(request, cancellationToken);
    }

    /// <summary>The link code of a signed-in member. The code is returned once; the test sends it to the bot.</summary>
    public static async Task<string> CreateLinkCodeAsync(HttpClient member, CancellationToken cancellationToken)
    {
        var response = await member.PostAsync(TelegramRoutes.LinkCode, null, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));
        return (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(CodeProperty).GetString()!;
    }

    public static async Task<bool> IsLinkedAsync(HttpClient member, CancellationToken cancellationToken)
    {
        var response = await member.GetAsync(TelegramRoutes.Link, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));
        return (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(LinkedProperty).GetBoolean();
    }

    /// <summary>Links the chat to the member with a new code and the given update id. Returns the reply the bot sent.</summary>
    public static async Task<string> LinkChatAsync(
        IdentityApiFactory factory,
        HttpClient member,
        long updateId,
        long chatId,
        CancellationToken cancellationToken)
    {
        var code = await CreateLinkCodeAsync(member, cancellationToken);
        var response = await PostUpdateAsync(factory, updateId, chatId, "/start " + code, cancellationToken: cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));
        return LastText(factory);
    }

    public static string LastText(IdentityApiFactory factory) => Bot(factory).Sent[^1].Text;

    public static async Task<int> CountActiveLinksAsync(IdentityApiFactory factory, long chatId, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        return await db.TelegramLinks
            .IgnoreQueryFilters()
            .CountAsync(link => link.ChatId == chatId && link.Active, cancellationToken);
    }

    public static async Task<List<TelegramDelivery>> ReadDeliveriesAsync(IdentityApiFactory factory, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        return await db.TelegramDeliveries.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <summary>Moves the member's code past its expiry, so the code is refused without waiting for the clock.</summary>
    public static async Task ExpireCodesAsync(IdentityApiFactory factory, Guid userId, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await db.TelegramLinkCodes
            .IgnoreQueryFilters()
            .Where(code => code.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(code => code.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)), cancellationToken);
    }

    /// <summary>Copies the task-assigned outbox rows as unprocessed rows: a replay of the same events.</summary>
    public static async Task ReplayTaskAssignedAsync(PostgresFixture postgres, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ReplayAssignmentsSql, connection);
        command.Parameters.AddWithValue("type", typeof(TaskAssigned).FullName!);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddFakes(IServiceCollection services)
    {
        services.AddSingleton<FakeTelegramBotClient>();
        services.AddSingleton<ITelegramBotClient>(provider => provider.GetRequiredService<FakeTelegramBotClient>());
        services.AddSingleton<FakePriorityReader>();
        services.AddSingleton<IPriorityReader>(provider => provider.GetRequiredService<FakePriorityReader>());
    }
}
