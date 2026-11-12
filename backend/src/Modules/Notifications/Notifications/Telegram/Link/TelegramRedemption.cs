namespace Parley.Notifications.Telegram.Link;

/// <summary>
/// The outcome of a <c>/start</c> redemption: the reply text, and the organization the redemption ran in. The organization is null
/// when the code was not found. The caller sets the tenant from it before saving, because a tenant set inside an awaited call
/// does not flow back to its caller.
/// </summary>
public sealed record TelegramRedemption(string Text, Guid? OrganizationId);
