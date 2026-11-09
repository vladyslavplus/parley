namespace Parley.Crm.Shared.Deals;

/// <summary>
/// The filters of the deal list. Every value is optional; the list applies only the ones given.
/// </summary>
/// <param name="Search">Case-insensitive text the title must contain.</param>
/// <param name="StageId">Only deals on this stage.</param>
/// <param name="OwnerUserId">Only deals owned by this user.</param>
/// <param name="CompanyId">Only deals of this company.</param>
/// <param name="ContactId">Only deals of this contact.</param>
/// <param name="Status">Only deals with this status: <c>open</c>, <c>won</c> or <c>lost</c>.</param>
public sealed record DealListFilter(
    string? Search,
    Guid? StageId,
    Guid? OwnerUserId,
    Guid? CompanyId,
    Guid? ContactId,
    string? Status);
