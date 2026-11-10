namespace Parley.Crm.Readers;

/// <summary>Unmapped row of the audience statement.</summary>
public sealed class AudienceSqlRow
{
    public Guid ContactId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string? CountryIso2 { get; set; }

    public DateTime? LastOrderAt { get; set; }

    public string? Email { get; set; }

    public string? EmailNormalized { get; set; }

    public Guid[] TagIds { get; set; } = [];
}
