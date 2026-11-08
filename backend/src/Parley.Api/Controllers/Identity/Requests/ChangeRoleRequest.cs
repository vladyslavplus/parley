namespace Parley.Api.Controllers.Identity.Requests;

/// <summary>Body of <c>PUT /members/{id}/role</c>.</summary>
public sealed record ChangeRoleRequest(string? RoleCode);
