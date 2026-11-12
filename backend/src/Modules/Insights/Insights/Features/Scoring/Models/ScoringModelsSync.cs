using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Features.Scoring.Client;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Scoring.Models;

/// <summary>
/// Upserts every model the scoring service reports as available into <c>model_versions</c>. Unavailable models are not
/// stored, so a version the service cannot score with is never pinned. Used by the scheduled sync, by the sync that a run
/// start queues when no model is known, and by the admin refresh. Throws <see cref="ScoringServiceException"/> when the
/// service cannot answer; the caller decides whether that is a log line or a failed request.
/// </summary>
public sealed class ScoringModelsSync(ScoringServiceClient client, InsightsDbContext dbContext, TimeProvider timeProvider)
{
    /// <summary>Syncs the registry and returns the number of available models the service reported.</summary>
    public async Task<int> SyncAsync(CancellationToken cancellationToken)
    {
        var response = await client.ReadModelsAsync(cancellationToken);
        var syncedAt = timeProvider.GetUtcNow().UtcDateTime;
        var available = response.Models.Where(entry => entry.Status == ScoringModelStatuses.Available).ToList();

        foreach (var entry in available)
        {
            var existing = await dbContext.ModelVersions
                .FirstOrDefaultAsync(row => row.ModelName == entry.ModelName && row.Version == entry.Version, cancellationToken);
            var target = existing ?? new ModelVersion { ModelName = entry.ModelName, Version = entry.Version };
            target.Kind = entry.Kind;
            target.FeatureSchemaVersion = entry.FeatureSchemaVersion;
            target.Provenance = entry.Provenance;
            target.TrainedAt = ParseInstant(entry.TrainedAt, syncedAt);
            target.Metrics = JsonSerializer.Serialize(entry.Metrics);
            target.SyncedAt = syncedAt;
            if (existing is null)
            {
                dbContext.ModelVersions.Add(target);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return available.Count;
    }

    /// <summary>The training instant in UTC; an unreadable value falls back to the sync instant rather than failing the sync.</summary>
    private static DateTime ParseInstant(string text, DateTime fallback) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.UtcDateTime
            : fallback;
}
