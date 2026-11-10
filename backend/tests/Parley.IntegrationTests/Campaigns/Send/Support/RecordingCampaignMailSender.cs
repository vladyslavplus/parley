using System.Collections.Concurrent;
using Parley.Campaigns.Sending.Mail;

namespace Parley.IntegrationTests.Campaigns.Send.Support;

/// <summary>
/// In-memory mail sender for the send tests. It records every call, counts calls per message id, and answers with
/// the outcome that <see cref="Behavior"/> returns for that attempt. No connection is made.
/// </summary>
public sealed class RecordingCampaignMailSender : ICampaignMailSender
{
    private readonly ConcurrentQueue<CampaignMailMessage> _calls = new();

    private readonly ConcurrentDictionary<string, int> _attempts = new(StringComparer.Ordinal);

    /// <summary>Outcome for a call: the message and its attempt number, starting at 1. Defaults to accepted.</summary>
    public Func<CampaignMailMessage, int, CampaignMailOutcome> Behavior { get; set; } =
        static (_, _) => CampaignMailOutcome.Accepted;

    public IReadOnlyList<CampaignMailMessage> Calls => _calls.ToArray();

    public int CallsFor(string messageId) => _attempts.TryGetValue(messageId, out var count) ? count : 0;

    public Task<CampaignMailOutcome> SendAsync(CampaignMailMessage message, CancellationToken cancellationToken)
    {
        var attempt = _attempts.AddOrUpdate(message.MessageId, 1, static (_, previous) => previous + 1);
        _calls.Enqueue(message);
        return Task.FromResult(Behavior(message, attempt));
    }
}
