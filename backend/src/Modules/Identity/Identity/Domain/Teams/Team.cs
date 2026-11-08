using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Identity.Domain.Teams;

/// <summary>
/// A group of members inside one organization. Team scope follows membership.
/// </summary>
public sealed class Team : BusinessEntity
{
    public Team()
    {
    }

    public Team(Guid organizationId, string name) : base(organizationId)
    {
        Name = name;
    }

    public string Name { get; set; } = string.Empty;

    /// <summary>Lowercase copy of <see cref="Name"/>, a stored generated column that backs the unique index.</summary>
    public string NameNormalized { get; private set; } = string.Empty;
}
