namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of tag attachment to a contact.</summary>
public sealed record AttachTagsRequest(IReadOnlyList<Guid>? TagIds);
