using System.Text.Json;
using Mediator;
using Parley.Crm.Features.Companies.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.Update;

/// <summary>
/// Replaces the editable fields of a company. <paramref name="Version"/> is the row version the
/// client read; a stale value returns 409. The owner changes through the bulk owner command.
/// A null custom field map keeps the stored values.
/// </summary>
[RequiresPermission(Permissions.CompaniesWrite)]
public sealed record UpdateCompanyCommand(
    Guid Id,
    uint Version,
    string Name,
    string? Domain,
    string? Country,
    string? Industry,
    IReadOnlyDictionary<string, JsonElement>? CustomFields) : IRequest<Result<CompanyView>>;
