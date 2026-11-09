using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Tags;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.Tags;

public sealed class AttachContactTagsCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory) : IRequestHandler<AttachContactTagsCommand, Result<ContactView>>
{
    private const int MaximumAttachAttempts = 2;

    public async ValueTask<Result<ContactView>> Handle(AttachContactTagsCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsWrite, cancellationToken);
        var row = await ContactLookup.FindRowAsync(dbContext, dataScope, command.ContactId, scope, cancellationToken);
        if (row is null)
        {
            return Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        var tagIds = command.TagIds.Distinct().ToList();
        var existingTagCount = await dbContext.Tags.CountAsync(tag => tagIds.Contains(tag.Id), cancellationToken);
        if (existingTagCount != tagIds.Count)
        {
            return Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.TagNotFound));
        }

        var updated = await AttachMissingLinksAsync(command.ContactId, tagIds, scope, cancellationToken);
        return updated is null
            ? Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound))
            : Result<ContactView>.Success(await ContactViews.ToViewAsync(updated, ownerDirectory, cancellationToken));
    }

    /// <summary>
    /// Adds the links that are not there yet and returns the contact as it stands afterwards. Two
    /// concurrent attaches of one tag can both read "not attached"; the composite key rejects the
    /// second insert. The retry re-reads the links, finds the tag already attached and inserts
    /// nothing, so the duplicate attach succeeds without a second row.
    /// </summary>
    private async Task<ContactRow?> AttachMissingLinksAsync(
        Guid contactId,
        IReadOnlyList<Guid> tagIds,
        DataScope scope,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var current = await ContactLookup.FindRowAsync(dbContext, dataScope, contactId, scope, cancellationToken);
            if (current is null)
            {
                return null;
            }

            var attached = current.TagIds.ToHashSet();
            foreach (var tagId in tagIds.Where(tagId => !attached.Contains(tagId)))
            {
                dbContext.ContactTags.Add(new ContactTag(tenantContext.OrganizationId, contactId, tagId));
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return await ContactLookup.FindRowAsync(dbContext, dataScope, contactId, scope, cancellationToken);
            }
            catch (DbUpdateException exception)
                when (attempt < MaximumAttachAttempts
                    && UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.ContactTagPrimaryKey))
            {
                dbContext.ChangeTracker.Clear();
            }
        }
    }
}
