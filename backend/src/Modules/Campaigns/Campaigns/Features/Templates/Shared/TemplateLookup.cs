using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Domain.Templates;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Features.Templates.Shared;

/// <summary>Template lookups shared by the template handlers. Archived templates are not found.</summary>
internal static class TemplateLookup
{
    public static Task<EmailTemplate?> FindLiveTrackedAsync(CampaignsDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Templates.FirstOrDefaultAsync(template => template.Id == id && template.ArchivedAt == null, cancellationToken);

    /// <summary>Reads the view from the database, so the version is the one the database holds after a save.</summary>
    public static Task<TemplateView?> FindLiveViewAsync(CampaignsDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Templates
            .AsNoTracking()
            .Where(template => template.Id == id && template.ArchivedAt == null)
            .Select(template => new TemplateView(
                template.Id,
                template.Name,
                template.SubjectLiquid,
                template.BodyLiquid,
                template.Locale,
                EF.Property<uint>(template, PersistenceConstants.XminPropertyName)))
            .FirstOrDefaultAsync(cancellationToken);
}
