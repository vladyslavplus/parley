using System.Text.Json;
using Mediator;
using Parley.Crm.Features.Companies.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.Create;

/// <summary>Creates a company. The owner defaults to the caller and must lie inside the caller's scope.</summary>
[RequiresPermission(Permissions.CompaniesWrite)]
public sealed record CreateCompanyCommand(
    string Name,
    string? Domain,
    string? Country,
    string? Industry,
    Guid? OwnerUserId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields) : IRequest<Result<CompanyView>>;
