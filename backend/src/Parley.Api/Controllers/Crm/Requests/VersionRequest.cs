namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a state change that only needs the row version the client read.</summary>
public sealed record VersionRequest(uint Version);
