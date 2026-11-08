using Mediator;
using Parley.Crm.Features.Companies.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.Restore;

/// <summary>Restores a soft-deleted company.</summary>
/// <param name="Id">Company identifier.</param>
[RequiresPermission(Permissions.CompaniesDelete)]
public sealed record RestoreCompanyCommand(Guid Id) : IRequest<Result<CompanyView>>;
