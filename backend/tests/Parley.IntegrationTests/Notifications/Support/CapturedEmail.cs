using Xunit;

[assembly: AssemblyFixture(typeof(Parley.IntegrationTests.Infrastructure.Fixtures.MailpitFixture))]

namespace Parley.IntegrationTests.Notifications.Support;


/// <summary>A message as captured by Mailpit.</summary>
public sealed record CapturedEmail(
    string Subject,
    string Text,
    string Html,
    IReadOnlyList<string> ToAddresses);
