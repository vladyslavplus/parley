using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Organizations.Get;

public sealed class GetOrganizationQueryHandler(IdentityDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetOrganizationQuery, Result<OrganizationView>>
{
    public async ValueTask<Result<OrganizationView>> Handle(GetOrganizationQuery query, CancellationToken cancellationToken)
    {
        if (query.OrganizationId != tenantContext.OrganizationId)
        {
            return Result<OrganizationView>.Failure(Error.NotFound(IdentityErrorCodes.OrganizationNotFound));
        }

        var view = await dbContext.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == query.OrganizationId)
            .Select(organization => new OrganizationView(
                organization.Id,
                organization.Name,
                organization.Slug,
                organization.TimeZone,
                organization.DefaultLocale,
                organization.Currency))
            .FirstOrDefaultAsync(cancellationToken);
        return view is null
            ? Result<OrganizationView>.Failure(Error.NotFound(IdentityErrorCodes.OrganizationNotFound))
            : Result<OrganizationView>.Success(view);
    }
}
