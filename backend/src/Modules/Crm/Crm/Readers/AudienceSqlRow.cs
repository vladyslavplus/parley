namespace Parley.Crm.Readers;

/// <summary>Unmapped row of the audience statement.</summary>
public sealed class AudienceSqlRow
{
    public Guid ContactId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string? CountryIso2 { get; set; }
}
