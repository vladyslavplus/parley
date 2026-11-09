namespace Parley.Crm.Features.Tags.Shared;

/// <summary>Raw tag columns read for keyset paging. <see cref="NameNormalized"/> is the database sort key.</summary>
public sealed record TagRow(Guid Id, string Name, short ColorIndex, string NameNormalized);
