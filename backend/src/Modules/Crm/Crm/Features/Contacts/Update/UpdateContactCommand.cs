using System.Text.Json;
using Mediator;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Update;

/// <summary>
/// Replaces the editable fields of a contact. <paramref name="Version"/> is the row version the
/// client read; a stale value returns 409. The owner changes through the bulk owner command.
/// A null custom field map keeps the stored values.
/// </summary>
[RequiresPermission(Permissions.ContactsWrite)]
public sealed record UpdateContactCommand(
    Guid Id,
    uint Version,
    string FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Country,
    string? City,
    Guid? CompanyId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields) : IRequest<Result<ContactView>>;
