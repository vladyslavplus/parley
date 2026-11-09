using System.Text.Json;
using Mediator;
using Parley.Crm.Features.Deals.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.Create;

/// <summary>
/// Creates a deal on a stage of the default pipeline (the first open stage when none is given). The owner defaults to
/// the caller and must lie inside the caller's scope. Emits <c>DealAssigned</c>, and <c>DealClosed</c> when the stage closes the deal.
/// </summary>
/// <param name="Title">Deal title.</param>
/// <param name="Amount">Amount in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="ContactId">Linked contact, when given.</param>
/// <param name="CompanyId">Linked company, when given.</param>
/// <param name="OwnerUserId">Owner; the caller when null.</param>
/// <param name="ExpectedCloseOn">Expected close date, when known.</param>
/// <param name="StageId">Stage of the deal; the first open stage when null.</param>
/// <param name="CustomFields">Custom field values; null means none.</param>
[RequiresPermission(Permissions.DealsWrite)]
public sealed record CreateDealCommand(
    string Title,
    decimal Amount,
    string Currency,
    Guid? ContactId,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateOnly? ExpectedCloseOn,
    Guid? StageId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields) : IRequest<Result<DealView>>;
