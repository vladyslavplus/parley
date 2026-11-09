using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.BulkAssignOwner;

/// <summary>
/// Assigns one owner to several companies. All-or-nothing: if any listed company is missing or
/// outside the caller's scope, nothing changes and the answer is not found.
/// </summary>
/// <param name="CompanyIds">Companies to change.</param>
/// <param name="OwnerUserId">New owner, which must lie inside the caller's scope.</param>
[RequiresPermission(Permissions.CompaniesWrite)]
public sealed record BulkAssignCompanyOwnerCommand(IReadOnlyList<Guid> CompanyIds, Guid OwnerUserId) : IRequest<Result>;
