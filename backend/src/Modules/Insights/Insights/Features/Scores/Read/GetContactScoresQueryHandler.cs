using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Recipients;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Scores.Read;

public sealed class GetContactScoresQueryHandler(
    InsightsDbContext dbContext,
    IScoreReader scores,
    IDataScope dataScope,
    ICrmRecipientProfileReader profiles,
    IPermissionChecker permissions,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<GetContactScoresQuery, Result<ContactScoresView>>
{
    private const string SegmentKey = "segment";

    public async ValueTask<Result<ContactScoresView>> Handle(GetContactScoresQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        var owners = scope.Level == DataScopeLevel.Organization ? null : scope.OwnerUserIds;
        var visible = await profiles.ReadVisibleBatchAsync([query.ContactId], owners?.ToArray(), cancellationToken);
        if (visible.Count == 0)
        {
            // A contact outside the caller's owner scope is indistinguishable from a missing one.
            return Result<ContactScoresView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        // With ML switched off the scores are not current, so the contact shows no ML item and no uplift segment.
        var mlOn = await dbContext.MlSettings.AsNoTracking().AnyAsync(setting => setting.MlEnabled, cancellationToken);
        var rfmDay = await scores.ReadLatestRfmDayAsync(cancellationToken);
        var segments = await scores.ReadRfmSegmentsAsync([query.ContactId], rfmDay, cancellationToken);
        var rows = await dbContext.LatestScores.AsNoTracking()
            .Where(row => row.ContactId == query.ContactId)
            .OrderBy(row => row.ScoreKind)
            .Select(row => new { row.ScoreKind, row.ModelName, row.ModelVersion, row.Value, row.Payload, row.ScoredAt })
            .ToListAsync(cancellationToken);

        var versions = rows.Select(row => row.ModelVersion).Distinct().ToArray();
        var provenance = versions.Length == 0
            ? new Dictionary<(string, string), string>()
            : (await dbContext.ModelVersions.AsNoTracking()
                .Where(model => versions.Contains(model.Version))
                .Select(model => new { model.ModelName, model.Version, model.Provenance })
                .ToListAsync(cancellationToken))
                .ToDictionary(model => (model.ModelName, model.Version), model => model.Provenance);

        var mlViewer = currentUser.UserId is { } userId
            && await permissions.HasPermissionAsync(userId, tenantContext.OrganizationId, Permissions.InsightsMlView, cancellationToken);

        var items = rows
            .Where(row => row.ScoreKind != ScoreKinds.Rfm && mlOn)
            .Select(row => new ContactScoreItemView(
                row.ScoreKind,
                row.ModelName,
                row.ModelVersion,
                provenance.TryGetValue((row.ModelName, row.ModelVersion), out var origin) ? origin : null,
                row.ScoredAt,
                mlViewer ? row.Value : null,
                mlViewer ? JsonDocument.Parse(row.Payload).RootElement.Clone() : null))
            .ToList();

        var uplift = rows.FirstOrDefault(row => row.ScoreKind == ScoreKinds.Uplift);
        var upliftSegment = !mlOn || uplift is null ? null : ReadSegment(uplift.Payload);
        return Result<ContactScoresView>.Success(new ContactScoresView(
            query.ContactId,
            segments.TryGetValue(query.ContactId, out var segment) ? segment : null,
            upliftSegment,
            items));
    }

    private static string? ReadSegment(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.TryGetProperty(SegmentKey, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
