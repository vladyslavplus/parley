using Mediator;
using Parley.Crm.Contracts.Search;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Search.Search;

/// <summary>
/// Global search across the records the caller may see. Requires <c>contacts.read</c>; companies
/// are searched only when the caller also holds <c>companies.read</c>, and each kind follows its
/// own scope. Deals will join under the same rule.
/// </summary>
/// <param name="Q">The search term; trimmed and length-checked by the validator.</param>
[RequiresPermission(Permissions.ContactsRead)]
public sealed record SearchQuery(string? Q) : IRequest<Result<SearchResponse>>;
