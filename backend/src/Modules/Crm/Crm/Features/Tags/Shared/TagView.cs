namespace Parley.Crm.Features.Tags.Shared;

/// <summary>A tag of the organization.</summary>
/// <param name="Id">Tag identifier.</param>
/// <param name="Name">Tag name, unique ignoring case.</param>
/// <param name="ColorIndex">Index into the pastel palette, from 0 to 4.</param>
public sealed record TagView(Guid Id, string Name, short ColorIndex);
