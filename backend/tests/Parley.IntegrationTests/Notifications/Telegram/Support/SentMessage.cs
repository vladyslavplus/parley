namespace Parley.IntegrationTests.Notifications.Telegram.Support;

/// <summary>One message the fake sent: the chat and the text.</summary>
public sealed record SentMessage(long ChatId, string Text);
