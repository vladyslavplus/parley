using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.Create;

public sealed class CreateContactCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IPermissionChecker permissionChecker,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<CreateContactCommand, Result<ContactView>>
{
    public async ValueTask<Result<ContactView>> Handle(CreateContactCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsWrite, cancellationToken);

        var ownerId = command.OwnerUserId ?? actorId;
        if (!await OwnerRules.IsAllowedAsync(ownerId, scope, organizationId, permissionChecker, cancellationToken))
        {
            return Result<ContactView>.Failure(CrmErrors.Field(nameof(CreateContactCommand.OwnerUserId), CrmErrorCodes.ContactOwnerNotAllowed));
        }

        var email = TextValues.NormalizeEmail(command.Email);
        if (email is not null && await dbContext.Contacts.AnyAsync(contact => contact.EmailNormalized == email && contact.DeletedAt == null, cancellationToken))
        {
            return Result<ContactView>.Failure(Error.Conflict(CrmErrorCodes.ContactEmailTaken));
        }

        // A company the caller cannot see is reported as missing, the same as one that does not exist.
        var companyScope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        if (command.CompanyId is { } companyId
            && !await CompanyScopeFilter.Apply(dbContext.Companies, companyScope).AnyAsync(company => company.Id == companyId, cancellationToken))
        {
            return Result<ContactView>.Failure(CrmErrors.Field(nameof(CreateContactCommand.CompanyId), CrmErrorCodes.ContactCompanyNotFound));
        }

        var definitions = await ActiveCustomFieldDefinitions.ReadAsync(dbContext, CustomFieldEntityTypes.Contact, cancellationToken);
        var customFields = CustomFieldValues.Merge(definitions, command.CustomFields ?? CustomFieldValues.NoValues, CrmLimits.EmptyJsonObject);
        if (!customFields.IsSuccess)
        {
            return Result<ContactView>.Failure(customFields.Error!);
        }

        var now = timeProvider.GetUtcNow();
        var contact = new Contact
        {
            OrganizationId = organizationId,
            FirstName = TextValues.Required(command.FirstName),
            LastName = TextValues.Optional(command.LastName),
            Email = TextValues.Optional(command.Email),
            EmailNormalized = email,
            Phone = TextValues.Optional(command.Phone),
            JobTitle = TextValues.Optional(command.JobTitle),
            Country = TextValues.NormalizeCountry(command.Country),
            City = TextValues.Optional(command.City),
            CompanyId = command.CompanyId,
            OwnerUserId = ownerId,
            CustomFields = customFields.Value!,
        };
        dbContext.Contacts.Add(contact);
        dbContext.Activities.Add(new Activity
        {
            OrganizationId = organizationId,
            Type = ActivityTypes.ContactCreated,
            ContactId = contact.Id,
            ActorUserId = actorId,
            OccurredAt = now.UtcDateTime,
        });
        outbox.Add(new ContactCreated { ContactId = contact.Id, OwnerUserId = ownerId, OccurredAt = now });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.ContactEmailIndex))
        {
            // A parallel create of the same email won the unique index.
            return Result<ContactView>.Failure(Error.Conflict(CrmErrorCodes.ContactEmailTaken));
        }

        var version = dbContext.Entry(contact).Property<uint>(PersistenceConstants.XminPropertyName).CurrentValue;
        return Result<ContactView>.Success(ContactViews.FromEntity(contact, version, []));
    }
}
