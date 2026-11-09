namespace Parley.Crm.Exports;

/// <summary>Contact columns written to the export file. Read with one projected query.</summary>
public sealed record ContactExportRow(
    Guid Id,
    string FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Country,
    string? City,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateTime CreatedAt);
