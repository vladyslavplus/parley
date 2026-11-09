using System.Text.Json;
using Mediator;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Create;

/// <summary>
/// Creates a contact. The owner defaults to the caller and must lie inside the caller's scope.
/// A null custom field map means no custom values. Emits <c>ContactCreated</c> and a
/// <c>contact_created</c> timeline entry in the same transaction.
/// </summary>
[RequiresPermission(Permissions.ContactsWrite)]
public sealed record CreateContactCommand(
    string FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Country,
    string? City,
    Guid? CompanyId,
    Guid? OwnerUserId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields) : IRequest<Result<ContactView>>;
