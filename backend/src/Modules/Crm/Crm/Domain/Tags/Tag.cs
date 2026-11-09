using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Tags;

/// <summary>A label of the organization that contacts can carry. Names are unique ignoring case.</summary>
public sealed class Tag : BusinessEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Index into the pastel palette of the interface.</summary>
    public short ColorIndex { get; set; }

    /// <summary>Lowercase copy of <see cref="Name"/>, a stored generated column that backs the unique index.</summary>
    public string NameNormalized { get; private set; } = string.Empty;
}
