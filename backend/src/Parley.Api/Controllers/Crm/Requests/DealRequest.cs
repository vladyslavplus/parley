using System.Text.Json;

namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of deal create. The owner defaults to the caller and the stage to the first open stage when omitted.</summary>
public sealed record DealRequest(
    string? Title,
    decimal Amount,
    string? Currency,
    Guid? ContactId,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateOnly? ExpectedCloseOn,
    Guid? StageId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
