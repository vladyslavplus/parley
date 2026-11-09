using System.Text.Json;

namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of deal update. <c>version</c> is the row version the client read; a null owner keeps the current one.</summary>
public sealed record UpdateDealRequest(
    uint Version,
    string? Title,
    decimal Amount,
    string? Currency,
    Guid? ContactId,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateOnly? ExpectedCloseOn,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
