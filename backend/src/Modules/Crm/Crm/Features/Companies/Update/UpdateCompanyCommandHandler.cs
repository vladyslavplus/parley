using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.Companies.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.Update;

public sealed class UpdateCompanyCommandHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory) : IRequestHandler<UpdateCompanyCommand, Result<CompanyView>>
{
    public async ValueTask<Result<CompanyView>> Handle(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesWrite, cancellationToken);
        var company = await CompanyLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (company is null)
        {
            return Result<CompanyView>.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound));
        }

        var definitions = await ActiveCustomFieldDefinitions.ReadAsync(dbContext, CustomFieldEntityTypes.Company, cancellationToken);
        var customFields = CustomFieldValues.Merge(definitions, command.CustomFields, company.CustomFields);
        if (!customFields.IsSuccess)
        {
            return Result<CompanyView>.Failure(customFields.Error!);
        }

        company.Name = TextValues.Required(command.Name);
        company.Domain = TextValues.Optional(command.Domain)?.ToLowerInvariant();
        company.Country = TextValues.NormalizeCountry(command.Country);
        company.Industry = TextValues.Optional(command.Industry);
        company.CustomFields = customFields.Value!;

        // The stale-write check: the UPDATE matches the client's row version, so a changed row affects nothing.
        var entry = dbContext.Entry(company);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<CompanyView>.Failure(ConcurrencyErrors.ToConflictError());
        }

        var row = await CompanyLookup.FindRowAsync(dbContext, company.Id, scope, cancellationToken);
        return row is null
            ? Result<CompanyView>.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound))
            : Result<CompanyView>.Success(await CompanyViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
