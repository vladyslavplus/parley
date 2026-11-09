using System.Text.Json;
using Mediator;
using Parley.Crm.Features.Deals.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.Update;

/// <summary>
/// Replaces the editable fields of a deal. <paramref name="Version"/> is the row version the client read; a stale value
/// returns 409. The stage changes through the move command. A null owner keeps the current one; a null custom field map
/// keeps the stored values.
/// </summary>
/// <param name="Id">Deal identifier.</param>
/// <param name="Version">Row version the client read.</param>
/// <param name="Title">Deal title.</param>
/// <param name="Amount">Amount in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="ContactId">Linked contact, or null to unlink.</param>
/// <param name="CompanyId">Linked company, or null to unlink.</param>
/// <param name="OwnerUserId">New owner, or null to keep the current one.</param>
/// <param name="ExpectedCloseOn">Expected close date, or null to clear it.</param>
/// <param name="CustomFields">Custom field values, or null to keep the stored ones.</param>
[RequiresPermission(Permissions.DealsWrite)]
public sealed record UpdateDealCommand(
    Guid Id,
    uint Version,
    string Title,
    decimal Amount,
    string Currency,
    Guid? ContactId,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateOnly? ExpectedCloseOn,
    IReadOnlyDictionary<string, JsonElement>? CustomFields) : IRequest<Result<DealView>>;
