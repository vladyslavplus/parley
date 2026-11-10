using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Recipients;

/// <summary>
/// Reads one keyset page of the recipients of a campaign in freeze order (recipient id). The filters narrow the rows;
/// <paramref name="Q"/> matches the contact name. Nothing here reads a statistic: the verdict and the rates come from the
/// results read.
/// </summary>
/// <param name="CampaignId">Campaign whose recipients are listed.</param>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the contact name must contain, ignoring case.</param>
/// <param name="Arm">Arm to list: <c>treatment</c> or <c>holdout</c>.</param>
/// <param name="Targeted">Only targeted (true) or only not targeted (false) recipients.</param>
/// <param name="SendStatus">Send status to list, as stored in the send records.</param>
/// <param name="Converted">Only converted (true) or only not converted (false) recipients with an outcome.</param>
/// <param name="Sort">Sort field: <c>id</c> (the only one, and the default).</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record ListCampaignRecipientsQuery(
    Guid CampaignId,
    string? After,
    int? Limit,
    string? Q,
    string? Arm,
    bool? Targeted,
    string? SendStatus,
    bool? Converted,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<CampaignRecipientView>>>;
