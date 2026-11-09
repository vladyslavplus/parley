namespace Parley.Crm.Shared.Deals;

/// <summary>
/// Raw deal columns read in one statement, with the display columns of its stage, contact and company.
/// Mapped to a view in memory, after the owner names are read from the user directory.
/// </summary>
public sealed record DealRow(
    Guid Id,
    Guid PipelineId,
    Guid StageId,
    string? StageName,
    string? StageSystemKey,
    string Status,
    string Title,
    string TitleNormalized,
    decimal Amount,
    string Currency,
    Guid? ContactId,
    string? ContactFirstName,
    string? ContactLastName,
    Guid? CompanyId,
    string? CompanyName,
    Guid OwnerUserId,
    string? LostReason,
    DateOnly? ExpectedCloseOn,
    DateTime? ClosedAt,
    long BoardPosition,
    string CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version);
