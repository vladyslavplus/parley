using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.Delete;

/// <summary>Soft deletes one company. Records an audit entry in the same transaction.</summary>
/// <param name="Id">Company identifier.</param>
[RequiresPermission(Permissions.CompaniesDelete)]
public sealed record DeleteCompanyCommand(Guid Id) : IRequest<Result>;
