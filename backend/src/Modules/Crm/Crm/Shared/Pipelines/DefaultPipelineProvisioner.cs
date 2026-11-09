using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Shared.Pipelines;

/// <summary>
/// Creates the default pipeline of an organization at most once. The organization-created consumer adds it
/// inside the inbox transaction; a request that finds no default pipeline (an organization created before
/// the consumer existed) adds and saves it in its own culture. The unique default index is the final guard.
/// </summary>
public sealed class DefaultPipelineProvisioner(CrmDbContext dbContext, ITenantContext tenantContext)
{
    /// <summary>
    /// Adds the default pipeline to the tracked graph unless the organization already has one. The caller saves.
    /// Returns whether a pipeline was added.
    /// </summary>
    public async Task<bool> AddIfMissingAsync(Guid organizationId, string culture, CancellationToken cancellationToken)
    {
        if (await dbContext.Pipelines.AnyAsync(pipeline => pipeline.IsDefault, cancellationToken))
        {
            return false;
        }

        var pipeline = new Pipeline
        {
            OrganizationId = organizationId,
            Name = SharedTexts.Get(DefaultPipelineStages.PipelineNameKey, culture),
            SystemKey = DefaultPipelineStages.PipelineNameKey,
            IsDefault = true,
        };
        for (var index = 0; index < DefaultPipelineStages.All.Count; index++)
        {
            var template = DefaultPipelineStages.All[index];
            pipeline.Stages.Add(new PipelineStage
            {
                OrganizationId = organizationId,
                PipelineId = pipeline.Id,
                Name = SharedTexts.Get(template.NameKey, culture),
                SystemKey = template.NameKey,
                Position = index,
                Kind = template.Kind,
                ProbabilityBps = template.ProbabilityBps,
                ColorIndex = template.ColorIndex,
            });
        }

        dbContext.Pipelines.Add(pipeline);
        return true;
    }

    /// <summary>Adds and saves the default pipeline in the request culture when the organization has none.</summary>
    public async Task EnsureSavedAsync(CancellationToken cancellationToken)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (!await AddIfMissingAsync(tenantContext.OrganizationId, culture, cancellationToken))
        {
            return;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.PipelineDefaultIndex))
        {
            // A parallel request created the default pipeline first; the tracked copy is discarded.
            dbContext.ChangeTracker.Clear();
        }
    }
}
