using System.Text.Json;

namespace Parley.Crm.Features.Deals.Shared;

/// <summary>
/// A deal as the API returns it. <see cref="Version"/> is the row version: clients send it back on update and
/// move, and a stale value returns 409.
/// </summary>
/// <param name="Id">Deal identifier.</param>
/// <param name="PipelineId">Pipeline the deal belongs to.</param>
/// <param name="StageId">Stage the deal sits on.</param>
/// <param name="StageName">Name of that stage.</param>
/// <param name="Status">Deal status: <c>open</c>, <c>won</c> or <c>lost</c>; follows the stage kind.</param>
/// <param name="Title">Deal title.</param>
/// <param name="Amount">Amount in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="ContactId">Linked contact, when set.</param>
/// <param name="ContactName">Full name of the linked contact, or null when unset or outside the caller's scope.</param>
/// <param name="CompanyId">Linked company, when set.</param>
/// <param name="CompanyName">Name of the linked company, or null when unset or outside the caller's scope.</param>
/// <param name="OwnerUserId">Owner of the deal.</param>
/// <param name="OwnerDisplayName">Owner display name, or null when unnamed or outside the organization.</param>
/// <param name="OwnerEmail">Owner email, or null under the same conditions.</param>
/// <param name="ExpectedCloseOn">Expected close date, when set.</param>
/// <param name="ClosedAt">Instant the deal was won or lost, or null while open.</param>
/// <param name="LostReason">Reason given when the deal was lost, when set.</param>
/// <param name="BoardPosition">Order of the deal inside its stage.</param>
/// <param name="CustomFields">Custom field values keyed by definition key.</param>
/// <param name="CreatedAt">Creation instant in UTC.</param>
/// <param name="UpdatedAt">Last change instant in UTC.</param>
/// <param name="Version">Row version to send back on update or move.</param>
public sealed record DealView(
    Guid Id,
    Guid PipelineId,
    Guid StageId,
    string StageName,
    string Status,
    string Title,
    decimal Amount,
    string Currency,
    Guid? ContactId,
    string? ContactName,
    Guid? CompanyId,
    string? CompanyName,
    Guid OwnerUserId,
    string? OwnerDisplayName,
    string? OwnerEmail,
    DateOnly? ExpectedCloseOn,
    DateTime? ClosedAt,
    string? LostReason,
    long BoardPosition,
    IReadOnlyDictionary<string, JsonElement> CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version);
