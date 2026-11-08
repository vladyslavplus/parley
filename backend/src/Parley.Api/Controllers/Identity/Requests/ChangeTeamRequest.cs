namespace Parley.Api.Controllers.Identity.Requests;

/// <summary>Body of <c>PUT /members/{id}/team</c>. A null team removes the member from its team.</summary>
public sealed record ChangeTeamRequest(Guid? TeamId);
