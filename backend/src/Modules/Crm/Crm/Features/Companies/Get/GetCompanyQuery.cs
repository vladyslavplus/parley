using Mediator;
using Parley.Crm.Features.Companies.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.Get;

/// <summary>Reads one company the caller may see. Out-of-scope and deleted companies return not found.</summary>
/// <param name="Id">Company identifier.</param>
[RequiresPermission(Permissions.CompaniesRead)]
public sealed record GetCompanyQuery(Guid Id) : IRequest<Result<CompanyView>>;
