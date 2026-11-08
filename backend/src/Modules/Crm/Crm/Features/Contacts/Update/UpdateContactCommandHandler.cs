using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Update;

public sealed class UpdateContactCommandHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory) : IRequestHandler<UpdateContactCommand, Result<ContactView>>
{
    public async ValueTask<Result<ContactView>> Handle(UpdateContactCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsWrite, cancellationToken);
        var contact = await ContactLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (contact is null)
        {
            return Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        var email = TextValues.NormalizeEmail(command.Email);
        if (email is not null && await dbContext.Contacts.AnyAsync(other => other.EmailNormalized == email && other.Id != contact.Id && other.DeletedAt == null, cancellationToken))
        {
            return Result<ContactView>.Failure(Error.Conflict(CrmErrorCodes.ContactEmailTaken));
        }

        // A company the caller cannot see is reported as missing, the same as one that does not exist.
        var companyScope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        if (command.CompanyId is { } companyId
            && !await CompanyScopeFilter.Apply(dbContext.Companies, companyScope).AnyAsync(company => company.Id == companyId, cancellationToken))
        {
            return Result<ContactView>.Failure(CrmErrors.Field(nameof(UpdateContactCommand.CompanyId), CrmErrorCodes.ContactCompanyNotFound));
        }

        var definitions = await ActiveCustomFieldDefinitions.ReadAsync(dbContext, CustomFieldEntityTypes.Contact, cancellationToken);
        var customFields = CustomFieldValues.Merge(definitions, command.CustomFields, contact.CustomFields);
        if (!customFields.IsSuccess)
        {
            return Result<ContactView>.Failure(customFields.Error!);
        }

        contact.FirstName = TextValues.Required(command.FirstName);
        contact.LastName = TextValues.Optional(command.LastName);
        contact.Email = TextValues.Optional(command.Email);
        contact.EmailNormalized = email;
        contact.Phone = TextValues.Optional(command.Phone);
        contact.JobTitle = TextValues.Optional(command.JobTitle);
        contact.Country = TextValues.NormalizeCountry(command.Country);
        contact.City = TextValues.Optional(command.City);
        contact.CompanyId = command.CompanyId;
        contact.CustomFields = customFields.Value!;

        // The stale-write check: the UPDATE matches the client's row version, so a changed row affects nothing.
        var entry = dbContext.Entry(contact);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ContactView>.Failure(ConcurrencyErrors.ToConflictError());
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.ContactEmailIndex))
        {
            return Result<ContactView>.Failure(Error.Conflict(CrmErrorCodes.ContactEmailTaken));
        }

        var row = await ContactLookup.FindRowAsync(dbContext, dataScope, contact.Id, scope, cancellationToken);
        return row is null
            ? Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound))
            : Result<ContactView>.Success(await ContactViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
