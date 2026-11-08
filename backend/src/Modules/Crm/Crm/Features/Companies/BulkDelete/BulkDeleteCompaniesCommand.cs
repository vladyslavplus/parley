using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.BulkDelete;

/// <summary>
/// Soft deletes several companies. All-or-nothing: if any listed company is missing or outside
/// the caller's scope, nothing changes and the answer is not found.
/// </summary>
/// <param name="CompanyIds">Companies to delete.</param>
[RequiresPermission(Permissions.CompaniesDelete)]
public sealed record BulkDeleteCompaniesCommand(IReadOnlyList<Guid> CompanyIds) : IRequest<Result>;
