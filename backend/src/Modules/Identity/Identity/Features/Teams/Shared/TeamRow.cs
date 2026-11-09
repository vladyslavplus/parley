namespace Parley.Identity.Features.Teams.Shared;

/// <summary>A team with the sort keys the team list orders by.</summary>
internal sealed record TeamRow(Guid Id, string Name, string NameNormalized, DateTime CreatedAt);
