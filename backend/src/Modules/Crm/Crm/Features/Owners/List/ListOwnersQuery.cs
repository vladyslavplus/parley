using Mediator;
using Parley.Crm.Features.Owners.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Owners.List;

/// <summary>
/// Looks up the people the caller may assign as record owners: active members whose name or email contains the
/// text. Limited to the owners the caller's contact scope covers.
/// </summary>
/// <param name="Q">Text the name or the email must contain.</param>
/// <param name="Limit">Most entries returned; defaults to the lookup default and is capped at the lookup maximum.</param>
[RequiresPermission(Permissions.ContactsRead)]
public sealed record ListOwnersQuery(string? Q, int? Limit) : IRequest<Result<IReadOnlyList<OwnerView>>>;
