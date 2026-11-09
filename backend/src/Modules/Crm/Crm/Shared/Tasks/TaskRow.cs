namespace Parley.Crm.Shared.Tasks;

/// <summary>
/// A task with the names it shows, read in one statement. The contact, deal and company names are empty when the
/// caller may not see the linked record.
/// </summary>
public sealed record TaskRow(
    Guid Id,
    string Title,
    string TitleNormalized,
    string? Notes,
    DateTime? DueAt,
    DateTime? CompletedAt,
    Guid AssigneeUserId,
    Guid? ContactId,
    string? ContactFirstName,
    string? ContactLastName,
    Guid? DealId,
    string? DealTitle,
    Guid? CompanyId,
    string? CompanyName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version);
