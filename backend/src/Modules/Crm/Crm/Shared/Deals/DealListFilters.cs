using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Deals;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Shared.Deals;

/// <summary>Applies <see cref="DealListFilter"/> to a deal query.</summary>
public static class DealListFilters
{
    public static IQueryable<Deal> Apply(IQueryable<Deal> deals, DealListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            deals = deals.Where(deal => EF.Functions.ILike(deal.TitleNormalized, pattern));
        }

        if (filter.StageId is { } stageId)
        {
            deals = deals.Where(deal => deal.StageId == stageId);
        }

        if (filter.OwnerUserId is { } ownerUserId)
        {
            deals = deals.Where(deal => deal.OwnerUserId == ownerUserId);
        }

        if (filter.CompanyId is { } companyId)
        {
            deals = deals.Where(deal => deal.CompanyId == companyId);
        }

        if (filter.ContactId is { } contactId)
        {
            deals = deals.Where(deal => deal.ContactId == contactId);
        }

        if (filter.Status is { } status)
        {
            deals = deals.Where(deal => deal.Status == status);
        }

        return deals;
    }
}
