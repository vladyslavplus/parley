using Mediator;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.Companies.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Companies.Create;

public sealed class CreateCompanyCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IPermissionChecker permissionChecker) : IRequestHandler<CreateCompanyCommand, Result<CompanyView>>
{
    public async ValueTask<Result<CompanyView>> Handle(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesWrite, cancellationToken);

        var ownerId = command.OwnerUserId ?? currentUser.RequireUserId();
        if (!await OwnerRules.IsAllowedAsync(ownerId, scope, organizationId, permissionChecker, cancellationToken))
        {
            return Result<CompanyView>.Failure(CrmErrors.Field(nameof(CreateCompanyCommand.OwnerUserId), CrmErrorCodes.CompanyOwnerNotAllowed));
        }

        var definitions = await ActiveCustomFieldDefinitions.ReadAsync(dbContext, CustomFieldEntityTypes.Company, cancellationToken);
        var customFields = CustomFieldValues.Merge(definitions, command.CustomFields ?? CustomFieldValues.NoValues, CrmLimits.EmptyJsonObject);
        if (!customFields.IsSuccess)
        {
            return Result<CompanyView>.Failure(customFields.Error!);
        }

        var company = new Company
        {
            OrganizationId = organizationId,
            Name = TextValues.Required(command.Name),
            Domain = TextValues.Optional(command.Domain)?.ToLowerInvariant(),
            Country = TextValues.NormalizeCountry(command.Country),
            Industry = TextValues.Optional(command.Industry),
            OwnerUserId = ownerId,
            CustomFields = customFields.Value!,
        };
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        var version = dbContext.Entry(company).Property<uint>(PersistenceConstants.XminPropertyName).CurrentValue;
        return Result<CompanyView>.Success(CompanyViews.FromEntity(company, version));
    }
}
