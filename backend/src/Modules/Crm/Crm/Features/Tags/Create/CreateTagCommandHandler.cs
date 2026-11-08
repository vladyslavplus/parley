using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Tags;
using Parley.Crm.Features.Tags.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Text;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Tags.Create;

public sealed class CreateTagCommandHandler(CrmDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<CreateTagCommand, Result<TagView>>
{
    public async ValueTask<Result<TagView>> Handle(CreateTagCommand command, CancellationToken cancellationToken)
    {
        var name = TextValues.Required(command.Name);
        var normalized = name.ToLowerInvariant();
        if (await dbContext.Tags.AnyAsync(tag => tag.NameNormalized == normalized, cancellationToken))
        {
            return Result<TagView>.Failure(Error.Conflict(CrmErrorCodes.TagNameTaken));
        }

        var tag = new Tag
        {
            OrganizationId = tenantContext.OrganizationId,
            Name = name,
            ColorIndex = command.ColorIndex,
        };
        dbContext.Tags.Add(tag);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.TagNameIndex))
        {
            return Result<TagView>.Failure(Error.Conflict(CrmErrorCodes.TagNameTaken));
        }

        return Result<TagView>.Success(TagLookup.ToView(tag));
    }
}
