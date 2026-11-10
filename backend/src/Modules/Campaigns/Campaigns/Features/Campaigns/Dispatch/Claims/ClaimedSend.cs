namespace Parley.Campaigns.Features.Campaigns.Dispatch.Claims;

/// <summary>
/// A claim this worker holds, with what the send needs: the address and locale frozen with the recipient, and the
/// attempt number. It is the only input of the send step.
/// </summary>
/// <param name="SendId">Claim row.</param>
/// <param name="ContactId">Contact the message goes to.</param>
/// <param name="RecipientId">Frozen recipient row.</param>
/// <param name="MessageId">Deterministic message id.</param>
/// <param name="AttemptCount">Attempt number of this claim, starting at 1.</param>
/// <param name="Address">Address frozen with the recipient.</param>
/// <param name="Locale">Locale frozen with the recipient.</param>
public sealed record ClaimedSend(
    Guid SendId,
    Guid ContactId,
    Guid RecipientId,
    string MessageId,
    int AttemptCount,
    string Address,
    string Locale);
