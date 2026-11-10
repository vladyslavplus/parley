namespace Parley.Crm.Readers;

/// <summary>Unmapped row of the recipient profile statement.</summary>
public sealed class RecipientProfileSqlRow
{
    public Guid ContactId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? CompanyName { get; set; }

    public bool IsDeleted { get; set; }
}
