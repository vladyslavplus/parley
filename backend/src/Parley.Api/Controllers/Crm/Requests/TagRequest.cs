namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of tag create and update.</summary>
public sealed record TagRequest(string? Name, short ColorIndex);
