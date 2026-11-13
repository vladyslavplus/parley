namespace Parley.Campaigns.Features.Campaigns.Dispatch.Claims;

/// <summary>
/// One claim step: how many claims the worker now holds and which of them are sendable. A suppressed recipient is claimed
/// and recorded as skipped in the same transaction, so it counts in <paramref name="Claimed"/> but not in
/// <paramref name="Sendable"/>; the runner loops on the claimed count, so a whole batch of suppressed recipients cannot
/// stop it.
/// </summary>
/// <param name="Sendable">The claimed recipients whose mail may be submitted.</param>
/// <param name="Claimed">Every claim this step took, including the suppressed ones recorded as skipped.</param>
public sealed record ClaimedSendBatch(IReadOnlyList<ClaimedSend> Sendable, int Claimed);
