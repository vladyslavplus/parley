namespace Parley.Api.RateLimiting;

/// <summary>Rate-limit policy names of the Telegram endpoints that accept anonymous requests.</summary>
public static class TelegramRateLimitPolicies
{
    /// <summary>Webhook updates from Telegram, per client address.</summary>
    public const string Webhook = "telegram-webhook";
}
